using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Reelix.Domain.Catalog;
using Reelix.Domain.Notifications;
using Reelix.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Reelix.UnitTests.Api;

public sealed class PostgresAuthorizationIntegrationTests
    : IClassFixture<PostgresAuthorizationFixture>, IDisposable
{
    private readonly PostgresAuthorizationFixture _fixture;
    private readonly HttpClient _client;

    public PostgresAuthorizationIntegrationTests(PostgresAuthorizationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task PrivateProfiles_AreIsolatedBetweenAuthenticatedAccounts()
    {
        var ownerSubject = Guid.NewGuid();
        var otherSubject = Guid.NewGuid();

        using var updateOwnerProfile = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile")
        {
            Content = JsonContent.Create(new
            {
                displayName = "Perfil privado A",
                biography = "Conteúdo de teste.",
                profileIsPublic = false,
                favoritesArePublic = false,
                watchHistoryIsPublic = false,
            }),
        };
        updateOwnerProfile.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _fixture.Factory.CreateToken(ownerSubject));

        using var updateResponse = await _client.SendAsync(updateOwnerProfile);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var ownerAccountRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/auth/me",
            ownerSubject);
        using var ownerAccountResponse = await _client.SendAsync(ownerAccountRequest);
        Assert.Equal(HttpStatusCode.OK, ownerAccountResponse.StatusCode);
        using var ownerAccount = JsonDocument.Parse(await ownerAccountResponse.Content.ReadAsStringAsync());
        var ownerAccountId = ownerAccount.RootElement.GetProperty("id").GetGuid();

        using var otherOwnProfileRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/profile",
            otherSubject);
        using var otherOwnProfileResponse = await _client.SendAsync(otherOwnProfileRequest);
        Assert.Equal(HttpStatusCode.OK, otherOwnProfileResponse.StatusCode);
        using var otherProfile = JsonDocument.Parse(await otherOwnProfileResponse.Content.ReadAsStringAsync());
        Assert.Null(otherProfile.RootElement.GetProperty("displayName").GetString());

        using var privateProfileRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/profile/{ownerAccountId}",
            otherSubject);
        using var privateProfileResponse = await _client.SendAsync(privateProfileRequest);
        Assert.Equal(HttpStatusCode.NotFound, privateProfileResponse.StatusCode);

        using var ownerOwnProfileRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/profile",
            ownerSubject);
        using var ownerOwnProfileResponse = await _client.SendAsync(ownerOwnProfileRequest);
        Assert.Equal(HttpStatusCode.OK, ownerOwnProfileResponse.StatusCode);
        using var ownerProfile = JsonDocument.Parse(await ownerOwnProfileResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            "Perfil privado A",
            ownerProfile.RootElement.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task CustomLists_CannotBeReadUpdatedOrDeletedByAnotherAccount()
    {
        var ownerSubject = Guid.NewGuid();
        var otherSubject = Guid.NewGuid();

        using var createRequest = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/lists",
            ownerSubject);
        createRequest.Content = JsonContent.Create(new
        {
            name = "Lista privada A",
            description = "Lista de integração.",
        });
        using var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createdList = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var listId = createdList.RootElement.GetProperty("id").GetGuid();

        using var readRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/lists/{listId}",
            otherSubject);
        using var readResponse = await _client.SendAsync(readRequest);
        Assert.Equal(HttpStatusCode.NotFound, readResponse.StatusCode);

        using var updateRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/v1/lists/{listId}",
            otherSubject);
        updateRequest.Content = JsonContent.Create(new
        {
            name = "Lista tomada por B",
            description = "Tentativa de alteração.",
        });
        using var updateResponse = await _client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);

        using var deleteRequest = CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/v1/lists/{listId}",
            otherSubject);
        using var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var ownerReadRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/lists/{listId}",
            ownerSubject);
        using var ownerReadResponse = await _client.SendAsync(ownerReadRequest);
        Assert.Equal(HttpStatusCode.OK, ownerReadResponse.StatusCode);
        using var ownerList = JsonDocument.Parse(await ownerReadResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            "Lista privada A",
            ownerList.RootElement.GetProperty("list").GetProperty("name").GetString());
    }

    [Fact]
    public async Task PrivateRatings_AreVisibleAndMutableOnlyByTheirOwner()
    {
        var ownerSubject = Guid.NewGuid();
        var otherSubject = Guid.NewGuid();
        var targetId = await _fixture.CreateMovieReferenceAsync();

        using var saveRatingRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/v1/ratings/movie/{targetId}",
            ownerSubject);
        saveRatingRequest.Content = JsonContent.Create(new
        {
            score = 4.5m,
            audience = "private",
        });
        using var saveRatingResponse = await _client.SendAsync(saveRatingRequest);
        Assert.Equal(HttpStatusCode.OK, saveRatingResponse.StatusCode);

        using var ownerRatingsRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/ratings/mine",
            ownerSubject);
        using var ownerRatingsResponse = await _client.SendAsync(ownerRatingsRequest);
        Assert.Equal(HttpStatusCode.OK, ownerRatingsResponse.StatusCode);
        using var ownerRatings = JsonDocument.Parse(await ownerRatingsResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, ownerRatings.RootElement.GetProperty("totalCount").GetInt32());
        var ownRating = Assert.Single(ownerRatings.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(targetId, ownRating.GetProperty("targetId").GetGuid());

        using var otherRatingsRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/ratings/mine",
            otherSubject);
        using var otherRatingsResponse = await _client.SendAsync(otherRatingsRequest);
        Assert.Equal(HttpStatusCode.OK, otherRatingsResponse.StatusCode);
        using var otherRatings = JsonDocument.Parse(await otherRatingsResponse.Content.ReadAsStringAsync());
        Assert.Equal(0, otherRatings.RootElement.GetProperty("totalCount").GetInt32());

        using var otherSummaryRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/ratings/movie/{targetId}",
            otherSubject);
        using var otherSummaryResponse = await _client.SendAsync(otherSummaryRequest);
        Assert.Equal(HttpStatusCode.OK, otherSummaryResponse.StatusCode);
        using var otherSummary = JsonDocument.Parse(await otherSummaryResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            JsonValueKind.Null,
            otherSummary.RootElement.GetProperty("averageScore").ValueKind);
        Assert.Equal(0, otherSummary.RootElement.GetProperty("publicCount").GetInt32());
        Assert.Equal(
            JsonValueKind.Null,
            otherSummary.RootElement.GetProperty("myRating").ValueKind);

        using var otherDeleteRequest = CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/v1/ratings/movie/{targetId}",
            otherSubject);
        using var otherDeleteResponse = await _client.SendAsync(otherDeleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, otherDeleteResponse.StatusCode);

        using var ownerRatingsAfterDeleteRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/ratings/mine",
            ownerSubject);
        using var ownerRatingsAfterDeleteResponse = await _client.SendAsync(ownerRatingsAfterDeleteRequest);
        using var ownerRatingsAfterDelete = JsonDocument.Parse(
            await ownerRatingsAfterDeleteResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, ownerRatingsAfterDelete.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task MovieTracking_IsScopedToItsOwner()
    {
        var ownerSubject = Guid.NewGuid();
        var otherSubject = Guid.NewGuid();
        var titleId = await _fixture.CreateMovieReferenceAsync();

        using var markRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/v1/tracking/movies/{titleId}",
            ownerSubject);
        markRequest.Content = JsonContent.Create(new
        {
            watchedAt = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero),
        });
        using var markResponse = await _client.SendAsync(markRequest);
        Assert.Equal(HttpStatusCode.OK, markResponse.StatusCode);

        using var ownerHistoryRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/tracking/history?page=1&pageSize=20&type=movie",
            ownerSubject);
        using var ownerHistoryResponse = await _client.SendAsync(ownerHistoryRequest);
        Assert.Equal(HttpStatusCode.OK, ownerHistoryResponse.StatusCode);
        using var ownerHistory = JsonDocument.Parse(await ownerHistoryResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, ownerHistory.RootElement.GetProperty("totalCount").GetInt32());

        using var otherHistoryRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/tracking/history?page=1&pageSize=20&type=movie",
            otherSubject);
        using var otherHistoryResponse = await _client.SendAsync(otherHistoryRequest);
        Assert.Equal(HttpStatusCode.OK, otherHistoryResponse.StatusCode);
        using var otherHistory = JsonDocument.Parse(await otherHistoryResponse.Content.ReadAsStringAsync());
        Assert.Equal(0, otherHistory.RootElement.GetProperty("totalCount").GetInt32());

        using var otherStatusRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tracking/movies/{titleId}",
            otherSubject);
        using var otherStatusResponse = await _client.SendAsync(otherStatusRequest);
        Assert.Equal(HttpStatusCode.OK, otherStatusResponse.StatusCode);
        Assert.False(await otherStatusResponse.Content.ReadFromJsonAsync<bool>());

        using var otherRemoveRequest = CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/v1/tracking/movies/{titleId}",
            otherSubject);
        using var otherRemoveResponse = await _client.SendAsync(otherRemoveRequest);
        Assert.Equal(HttpStatusCode.NoContent, otherRemoveResponse.StatusCode);

        using var ownerStatusRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tracking/movies/{titleId}",
            ownerSubject);
        using var ownerStatusResponse = await _client.SendAsync(ownerStatusRequest);
        Assert.Equal(HttpStatusCode.OK, ownerStatusResponse.StatusCode);
        Assert.True(await ownerStatusResponse.Content.ReadFromJsonAsync<bool>());
    }

    [Fact]
    public async Task ConcurrentMovieTrackingMarks_KeepOneRecordPerAccountAndTitle()
    {
        var subject = Guid.NewGuid();
        var titleId = await _fixture.CreateMovieReferenceAsync();
        var requests = Enumerable.Range(0, 8).Select(async index =>
        {
            using var request = CreateAuthorizedRequest(
                HttpMethod.Put,
                $"/api/v1/tracking/movies/{titleId}",
                subject);
            request.Content = JsonContent.Create(new
            {
                watchedAt = DateTimeOffset.UtcNow.AddMinutes(-index),
            });
            return await _client.SendAsync(request);
        });
        var responses = await Task.WhenAll(requests);
        try
        {
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        using var historyRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/tracking/history?page=1&pageSize=20&type=movie",
            subject);
        using var historyResponse = await _client.SendAsync(historyRequest);
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        using var history = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, history.RootElement.GetProperty("totalCount").GetInt32());
        var record = Assert.Single(history.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(titleId, record.GetProperty("targetId").GetGuid());
    }

    [Fact]
    public async Task NotificationPreferences_AreIsolatedBetweenAccounts()
    {
        var firstSubject = Guid.NewGuid();
        var secondSubject = Guid.NewGuid();

        using var firstUpdateRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            "/api/v1/notifications/preferences",
            firstSubject);
        firstUpdateRequest.Content = JsonContent.Create(new
        {
            socialEnabled = true,
            newEpisodeEnabled = false,
            premiereEnabled = false,
        });
        using var firstUpdateResponse = await _client.SendAsync(firstUpdateRequest);
        Assert.Equal(HttpStatusCode.OK, firstUpdateResponse.StatusCode);

        using var secondUpdateRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            "/api/v1/notifications/preferences",
            secondSubject);
        secondUpdateRequest.Content = JsonContent.Create(new
        {
            socialEnabled = false,
            newEpisodeEnabled = true,
            premiereEnabled = true,
        });
        using var secondUpdateResponse = await _client.SendAsync(secondUpdateRequest);
        Assert.Equal(HttpStatusCode.OK, secondUpdateResponse.StatusCode);

        using var firstReadRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/notifications/preferences",
            firstSubject);
        using var firstReadResponse = await _client.SendAsync(firstReadRequest);
        Assert.Equal(HttpStatusCode.OK, firstReadResponse.StatusCode);
        using var firstPreferences = JsonDocument.Parse(await firstReadResponse.Content.ReadAsStringAsync());
        Assert.True(firstPreferences.RootElement.GetProperty("socialEnabled").GetBoolean());
        Assert.False(firstPreferences.RootElement.GetProperty("newEpisodeEnabled").GetBoolean());
        Assert.False(firstPreferences.RootElement.GetProperty("premiereEnabled").GetBoolean());

        using var secondReadRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/notifications/preferences",
            secondSubject);
        using var secondReadResponse = await _client.SendAsync(secondReadRequest);
        Assert.Equal(HttpStatusCode.OK, secondReadResponse.StatusCode);
        using var secondPreferences = JsonDocument.Parse(await secondReadResponse.Content.ReadAsStringAsync());
        Assert.False(secondPreferences.RootElement.GetProperty("socialEnabled").GetBoolean());
        Assert.True(secondPreferences.RootElement.GetProperty("newEpisodeEnabled").GetBoolean());
        Assert.True(secondPreferences.RootElement.GetProperty("premiereEnabled").GetBoolean());
    }

    [Fact]
    public async Task NotificationInbox_IsScopedToItsOwner()
    {
        var ownerSubject = Guid.NewGuid();
        var otherSubject = Guid.NewGuid();

        using var ownerAccountRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/auth/me",
            ownerSubject);
        using var ownerAccountResponse = await _client.SendAsync(ownerAccountRequest);
        Assert.Equal(HttpStatusCode.OK, ownerAccountResponse.StatusCode);
        using var ownerAccount = JsonDocument.Parse(await ownerAccountResponse.Content.ReadAsStringAsync());
        var ownerAccountId = ownerAccount.RootElement.GetProperty("id").GetGuid();
        var notificationId = await _fixture.CreateNotificationAsync(ownerAccountId);

        using var otherInboxRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/notifications/inbox",
            otherSubject);
        using var otherInboxResponse = await _client.SendAsync(otherInboxRequest);
        Assert.Equal(HttpStatusCode.OK, otherInboxResponse.StatusCode);
        using var otherInbox = JsonDocument.Parse(await otherInboxResponse.Content.ReadAsStringAsync());
        Assert.Empty(otherInbox.RootElement.GetProperty("items").EnumerateArray());

        using var otherMarkReadRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/v1/notifications/inbox/{notificationId}/read",
            otherSubject);
        using var otherMarkReadResponse = await _client.SendAsync(otherMarkReadRequest);
        Assert.Equal(HttpStatusCode.NotFound, otherMarkReadResponse.StatusCode);

        using var ownerInboxRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/notifications/inbox",
            ownerSubject);
        using var ownerInboxResponse = await _client.SendAsync(ownerInboxRequest);
        Assert.Equal(HttpStatusCode.OK, ownerInboxResponse.StatusCode);
        using var ownerInbox = JsonDocument.Parse(await ownerInboxResponse.Content.ReadAsStringAsync());
        var item = Assert.Single(ownerInbox.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(notificationId, item.GetProperty("id").GetGuid());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("readAt").ValueKind);

        using var ownerMarkReadRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/v1/notifications/inbox/{notificationId}/read",
            ownerSubject);
        using var ownerMarkReadResponse = await _client.SendAsync(ownerMarkReadRequest);
        Assert.Equal(HttpStatusCode.NoContent, ownerMarkReadResponse.StatusCode);
    }

    [Fact]
    public async Task GamificationPreferences_AreIsolatedAndHidePrivateBadges()
    {
        var ownerSubject = Guid.NewGuid();
        var otherSubject = Guid.NewGuid();

        using var ownerUpdateRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            "/api/v1/gamification/preferences",
            ownerSubject);
        ownerUpdateRequest.Content = JsonContent.Create(new
        {
            badgesPublic = false,
            rankingEnabled = true,
        });
        using var ownerUpdateResponse = await _client.SendAsync(ownerUpdateRequest);
        Assert.Equal(HttpStatusCode.OK, ownerUpdateResponse.StatusCode);

        using var otherUpdateRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            "/api/v1/gamification/preferences",
            otherSubject);
        otherUpdateRequest.Content = JsonContent.Create(new
        {
            badgesPublic = true,
            rankingEnabled = false,
        });
        using var otherUpdateResponse = await _client.SendAsync(otherUpdateRequest);
        Assert.Equal(HttpStatusCode.OK, otherUpdateResponse.StatusCode);

        using var ownerAccountRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/auth/me",
            ownerSubject);
        using var ownerAccountResponse = await _client.SendAsync(ownerAccountRequest);
        using var ownerAccount = JsonDocument.Parse(await ownerAccountResponse.Content.ReadAsStringAsync());
        var ownerAccountId = ownerAccount.RootElement.GetProperty("id").GetGuid();

        using var ownerDashboardRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/gamification/dashboard",
            ownerSubject);
        using var ownerDashboardResponse = await _client.SendAsync(ownerDashboardRequest);
        Assert.Equal(HttpStatusCode.OK, ownerDashboardResponse.StatusCode);
        using var ownerDashboard = JsonDocument.Parse(await ownerDashboardResponse.Content.ReadAsStringAsync());
        var ownerPreferences = ownerDashboard.RootElement.GetProperty("preferences");
        Assert.False(ownerPreferences.GetProperty("badgesPublic").GetBoolean());
        Assert.True(ownerPreferences.GetProperty("rankingEnabled").GetBoolean());

        using var otherDashboardRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/gamification/dashboard",
            otherSubject);
        using var otherDashboardResponse = await _client.SendAsync(otherDashboardRequest);
        Assert.Equal(HttpStatusCode.OK, otherDashboardResponse.StatusCode);
        using var otherDashboard = JsonDocument.Parse(await otherDashboardResponse.Content.ReadAsStringAsync());
        var otherPreferences = otherDashboard.RootElement.GetProperty("preferences");
        Assert.True(otherPreferences.GetProperty("badgesPublic").GetBoolean());
        Assert.False(otherPreferences.GetProperty("rankingEnabled").GetBoolean());

        using var badgesRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/gamification/users/{ownerAccountId}/badges",
            otherSubject);
        using var badgesResponse = await _client.SendAsync(badgesRequest);
        Assert.Equal(HttpStatusCode.OK, badgesResponse.StatusCode);
        using var badges = JsonDocument.Parse(await badgesResponse.Content.ReadAsStringAsync());
        Assert.False(badges.RootElement.GetProperty("isPublic").GetBoolean());
        Assert.Empty(badges.RootElement.GetProperty("badges").EnumerateArray());
    }

    [Fact]
    public async Task FriendRequest_CanOnlyBeAcceptedByItsRecipient()
    {
        var requesterSubject = Guid.NewGuid();
        var recipientSubject = Guid.NewGuid();
        var thirdPartySubject = Guid.NewGuid();

        using var recipientAccountRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/auth/me",
            recipientSubject);
        using var recipientAccountResponse = await _client.SendAsync(recipientAccountRequest);
        Assert.Equal(HttpStatusCode.OK, recipientAccountResponse.StatusCode);
        using var recipientAccount = JsonDocument.Parse(
            await recipientAccountResponse.Content.ReadAsStringAsync());
        var recipientAccountId = recipientAccount.RootElement.GetProperty("id").GetGuid();

        using var recipientProfileRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            "/api/v1/profile",
            recipientSubject);
        recipientProfileRequest.Content = JsonContent.Create(new
        {
            displayName = "Destinatário de teste",
            biography = (string?)null,
            profileIsPublic = true,
            favoritesArePublic = false,
            watchHistoryIsPublic = false,
        });
        using var recipientProfileResponse = await _client.SendAsync(recipientProfileRequest);
        Assert.Equal(HttpStatusCode.OK, recipientProfileResponse.StatusCode);

        using var sendRequest = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/social/friend-requests",
            requesterSubject);
        sendRequest.Content = JsonContent.Create(new { accountId = recipientAccountId });
        using var sendResponse = await _client.SendAsync(sendRequest);
        Assert.Equal(HttpStatusCode.Created, sendResponse.StatusCode);
        using var sentRequest = JsonDocument.Parse(await sendResponse.Content.ReadAsStringAsync());
        var relationshipId = sentRequest.RootElement.GetProperty("relationshipId").GetGuid();

        using var thirdPartyAccept = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/v1/social/friend-requests/{relationshipId}/accept",
            thirdPartySubject);
        using var thirdPartyResponse = await _client.SendAsync(thirdPartyAccept);
        Assert.Equal(HttpStatusCode.NotFound, thirdPartyResponse.StatusCode);

        using var requesterAccept = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/v1/social/friend-requests/{relationshipId}/accept",
            requesterSubject);
        using var requesterResponse = await _client.SendAsync(requesterAccept);
        Assert.Equal(HttpStatusCode.Conflict, requesterResponse.StatusCode);

        using var recipientAccept = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/v1/social/friend-requests/{relationshipId}/accept",
            recipientSubject);
        using var recipientResponse = await _client.SendAsync(recipientAccept);
        Assert.Equal(HttpStatusCode.OK, recipientResponse.StatusCode);

        using var requesterFriendsRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/social/friends",
            requesterSubject);
        using var requesterFriendsResponse = await _client.SendAsync(requesterFriendsRequest);
        Assert.Equal(HttpStatusCode.OK, requesterFriendsResponse.StatusCode);
        using var requesterFriends = JsonDocument.Parse(
            await requesterFriendsResponse.Content.ReadAsStringAsync());
        var requesterFriend = Assert.Single(requesterFriends.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(recipientAccountId, requesterFriend.GetProperty("accountId").GetGuid());

        using var thirdPartyFriendsRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/social/friends",
            thirdPartySubject);
        using var thirdPartyFriendsResponse = await _client.SendAsync(thirdPartyFriendsRequest);
        Assert.Equal(HttpStatusCode.OK, thirdPartyFriendsResponse.StatusCode);
        using var thirdPartyFriends = JsonDocument.Parse(
            await thirdPartyFriendsResponse.Content.ReadAsStringAsync());
        Assert.Empty(thirdPartyFriends.RootElement.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task SocialFeed_RequiresAcceptedFriendshipAndCurrentConsent()
    {
        var ownerSubject = Guid.NewGuid();
        var friendSubject = Guid.NewGuid();
        var strangerSubject = Guid.NewGuid();
        var titleId = await _fixture.CreateMovieReferenceAsync();
        var ownerAccountId = await GetAccountIdAsync(ownerSubject);
        var friendAccountId = await GetAccountIdAsync(friendSubject);

        using var friendProfileRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            "/api/v1/profile",
            friendSubject);
        friendProfileRequest.Content = JsonContent.Create(new
        {
            displayName = "Amigo de teste",
            biography = (string?)null,
            profileIsPublic = true,
            favoritesArePublic = false,
            watchHistoryIsPublic = false,
            isDiscoverable = true,
            watchHistorySharedWithFriends = false,
            favoritesSharedWithFriends = false,
            ratingsSharedWithFriends = false,
        });
        using var friendProfileResponse = await _client.SendAsync(friendProfileRequest);
        Assert.Equal(HttpStatusCode.OK, friendProfileResponse.StatusCode);

        using var requestFriendship = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/social/friend-requests",
            ownerSubject);
        requestFriendship.Content = JsonContent.Create(new { accountId = friendAccountId });
        using var requestFriendshipResponse = await _client.SendAsync(requestFriendship);
        Assert.Equal(HttpStatusCode.Created, requestFriendshipResponse.StatusCode);
        using var relationship = JsonDocument.Parse(
            await requestFriendshipResponse.Content.ReadAsStringAsync());
        var relationshipId = relationship.RootElement.GetProperty("relationshipId").GetGuid();

        using var acceptRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/v1/social/friend-requests/{relationshipId}/accept",
            friendSubject);
        using var acceptResponse = await _client.SendAsync(acceptRequest);
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

        using var enableConsent = CreateAuthorizedRequest(
            HttpMethod.Put,
            "/api/v1/profile",
            ownerSubject);
        enableConsent.Content = JsonContent.Create(new
        {
            displayName = "Dono do histórico",
            biography = (string?)null,
            profileIsPublic = false,
            favoritesArePublic = false,
            watchHistoryIsPublic = false,
            isDiscoverable = false,
            watchHistorySharedWithFriends = true,
            favoritesSharedWithFriends = false,
            ratingsSharedWithFriends = false,
        });
        using var enableConsentResponse = await _client.SendAsync(enableConsent);
        Assert.Equal(HttpStatusCode.OK, enableConsentResponse.StatusCode);

        using var markMovie = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/v1/tracking/movies/{titleId}",
            ownerSubject);
        markMovie.Content = JsonContent.Create(new { watchedAt = DateTimeOffset.UtcNow });
        using var markMovieResponse = await _client.SendAsync(markMovie);
        Assert.Equal(HttpStatusCode.OK, markMovieResponse.StatusCode);

        using var friendFeedRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/social/feed",
            friendSubject);
        using var friendFeedResponse = await _client.SendAsync(friendFeedRequest);
        Assert.Equal(HttpStatusCode.OK, friendFeedResponse.StatusCode);
        using var friendFeed = JsonDocument.Parse(await friendFeedResponse.Content.ReadAsStringAsync());
        var sharedActivity = Assert.Single(friendFeed.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(ownerAccountId, sharedActivity.GetProperty("actorAccountId").GetGuid());
        Assert.Equal("movieWatched", sharedActivity.GetProperty("activityType").GetString());

        using var strangerFeedRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/social/friends/{ownerAccountId}/activities",
            strangerSubject);
        using var strangerFeedResponse = await _client.SendAsync(strangerFeedRequest);
        Assert.Equal(HttpStatusCode.OK, strangerFeedResponse.StatusCode);
        using var strangerFeed = JsonDocument.Parse(await strangerFeedResponse.Content.ReadAsStringAsync());
        Assert.Empty(strangerFeed.RootElement.GetProperty("items").EnumerateArray());

        using var disableConsent = CreateAuthorizedRequest(
            HttpMethod.Put,
            "/api/v1/profile",
            ownerSubject);
        disableConsent.Content = JsonContent.Create(new
        {
            displayName = "Dono do histórico",
            biography = (string?)null,
            profileIsPublic = false,
            favoritesArePublic = false,
            watchHistoryIsPublic = false,
            isDiscoverable = false,
            watchHistorySharedWithFriends = false,
            favoritesSharedWithFriends = false,
            ratingsSharedWithFriends = false,
        });
        using var disableConsentResponse = await _client.SendAsync(disableConsent);
        Assert.Equal(HttpStatusCode.OK, disableConsentResponse.StatusCode);

        using var feedAfterConsentChangeRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/social/feed",
            friendSubject);
        using var feedAfterConsentChangeResponse = await _client.SendAsync(feedAfterConsentChangeRequest);
        Assert.Equal(HttpStatusCode.OK, feedAfterConsentChangeResponse.StatusCode);
        using var feedAfterConsentChange = JsonDocument.Parse(
            await feedAfterConsentChangeResponse.Content.ReadAsStringAsync());
        Assert.Empty(feedAfterConsentChange.RootElement.GetProperty("items").EnumerateArray());
    }

    public void Dispose() => _client.Dispose();

    private HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string path,
        Guid subject)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _fixture.Factory.CreateToken(subject));
        return request;
    }

    private async Task<Guid> GetAccountIdAsync(Guid subject)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/v1/auth/me", subject);
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var account = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return account.RootElement.GetProperty("id").GetGuid();
    }
}

public sealed class PostgresAuthorizationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("reelix_integration")
        .WithUsername("reelix_test")
        .WithPassword("reelix_test")
        .Build();

    public PostgresAuthorizationApiFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        var options = new DbContextOptionsBuilder<ReelixDbContext>()
            .UseNpgsql(_database.GetConnectionString())
            .Options;
        await using (var dbContext = new ReelixDbContext(options))
        {
            await dbContext.Database.MigrateAsync();
        }

        Factory = new PostgresAuthorizationApiFactory(_database.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        Factory?.Dispose();
        await _database.DisposeAsync();
    }

    public async Task<Guid> CreateMovieReferenceAsync()
    {
        var options = new DbContextOptionsBuilder<ReelixDbContext>()
            .UseNpgsql(_database.GetConnectionString())
            .Options;
        await using var dbContext = new ReelixDbContext(options);
        var reference = ExternalCatalogReference.Create(
            "tmdb",
            Guid.NewGuid().ToString("N"),
            "movie");
        dbContext.ExternalCatalogReferences.Add(reference);
        await dbContext.SaveChangesAsync();
        return reference.Id;
    }

    public async Task<Guid> CreateNotificationAsync(Guid accountId)
    {
        var options = new DbContextOptionsBuilder<ReelixDbContext>()
            .UseNpgsql(_database.GetConnectionString())
            .Options;
        await using var dbContext = new ReelixDbContext(options);
        var notification = UserNotification.Create(
            accountId,
            NotificationCategory.Social,
            Guid.NewGuid().ToString("N"),
            "Solicitação social",
            "Uma solicitação foi recebida.",
            "/social");
        dbContext.UserNotifications.Add(notification);
        await dbContext.SaveChangesAsync();
        return notification.Id;
    }
}

public sealed class PostgresAuthorizationApiFactory : WebApplicationFactory<Program>
{
    private readonly RSA _signingKey = RSA.Create(2048);
    private readonly string _connectionString;
    private readonly string? _previousSupabaseUrl =
        Environment.GetEnvironmentVariable("Supabase__Url");
    private readonly string? _previousConnectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__Reelix");

    public PostgresAuthorizationApiFactory(string connectionString)
    {
        _connectionString = connectionString;
        Environment.SetEnvironmentVariable(
            "Supabase__Url",
            "https://reelix-test.supabase.co");
        Environment.SetEnvironmentVariable("ConnectionStrings__Reelix", connectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Supabase:Url"] = "https://reelix-test.supabase.co",
                ["Supabase:JwtSecret"] = string.Empty,
                ["ConnectionStrings:Reelix"] = _connectionString,
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ReelixDbContext>>();
            services.RemoveAll<ReelixDbContext>();
            services.AddDbContext<ReelixDbContext>(options => options.UseNpgsql(_connectionString));
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.Authority = null;
                    options.ConfigurationManager = null;
                    options.TokenValidationParameters.ValidIssuer =
                        "https://reelix-test.supabase.co/auth/v1";
                    options.TokenValidationParameters.ValidAudience = "authenticated";
                    options.TokenValidationParameters.IssuerSigningKey =
                        new RsaSecurityKey(_signingKey);
                });
        });
    }

    public string CreateToken(Guid subject)
    {
        var credentials = new SigningCredentials(
            new RsaSecurityKey(_signingKey),
            SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(
            "https://reelix-test.supabase.co/auth/v1",
            "authenticated",
            [new Claim("sub", subject.ToString())],
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(5),
            credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _signingKey.Dispose();
            Environment.SetEnvironmentVariable("Supabase__Url", _previousSupabaseUrl);
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__Reelix",
                _previousConnectionString);
        }
    }
}

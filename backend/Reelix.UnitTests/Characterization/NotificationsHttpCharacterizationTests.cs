using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Reelix.Domain.Notifications;

namespace Reelix.UnitTests.Characterization;

/// <summary>
/// Batch 0 of spec 0014 (R4): the seven notification routes over HTTP and PostgreSQL, with the
/// golden JSON and the status of every error the routes produce today.
/// </summary>
[Collection(PostgresCharacterizationCollection.Name)]
public sealed class NotificationsHttpCharacterizationTests(PostgresCharacterizationFixture fixture)
    : CharacterizationTestBase(fixture)
{
    private static object Registration(
        Guid? installationId = null,
        string? token = null,
        object? platform = null,
        Guid? projectId = null) => new
    {
        installationId = installationId ?? Guid.NewGuid(),
        expoPushToken = token ?? NotificationsSeeder.NewToken(),
        platform = platform ?? "android",
        projectId = projectId ?? PostgresCharacterizationFixture.ExpoProjectId,
    };

    [Fact]
    public async Task Preferences_OfANewAccount_AreAllOff()
    {
        var account = await Fixture.CreateAccountAsync();

        var response = await GetAsync(account.Subject, "/api/v1/notifications/preferences");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.Equal("application/json", response.ContentType);
        AssertSameJson(
            """{"socialEnabled":false,"newEpisodeEnabled":false,"premiereEnabled":false}""",
            response.Body);
    }

    [Fact]
    public async Task Preferences_AreSavedAndReadBack()
    {
        var account = await Fixture.CreateAccountAsync();

        var update = await PutAsync(
            account.Subject,
            "/api/v1/notifications/preferences",
            new { socialEnabled = true, newEpisodeEnabled = false, premiereEnabled = true });
        var read = await GetAsync(account.Subject, "/api/v1/notifications/preferences");

        Assert.Equal(HttpStatusCode.OK, update.Status);
        const string expected = """{"socialEnabled":true,"newEpisodeEnabled":false,"premiereEnabled":true}""";
        AssertSameJson(expected, update.Body);
        AssertSameJson(expected, read.Body);
    }

    [Fact]
    public async Task RegisteringADevice_Returns201WithLocationAndTheDeviceJson()
    {
        var account = await Fixture.CreateAccountAsync();

        var response = await CallAsync(
            HttpMethod.Post,
            "/api/v1/notifications/devices",
            account.Subject,
            Registration(platform: "ANDROID"));

        Assert.Equal(HttpStatusCode.Created, response.Status);
        var body = response.Json();
        var id = body.GetProperty("id").GetGuid();
        Assert.EndsWith($"/api/v1/notifications/devices/{id}", response.Headers["Location"], StringComparison.Ordinal);
        var now = Iso(Clock.GetUtcNow());
        AssertSameJson(
            $$"""{"id":"{{id}}","platform":"android","createdAt":"{{now}}","lastRegisteredAt":"{{now}}"}""",
            response.Body);
        Assert.True((await Fixture.ReadDeviceAsync(id)).IsActive);
    }

    [Fact]
    public async Task RegisteringADevice_AcceptsThePlatformAsAnInteger()
    {
        var account = await Fixture.CreateAccountAsync();

        var response = await CallAsync(
            HttpMethod.Post,
            "/api/v1/notifications/devices",
            account.Subject,
            Registration(platform: 0));

        Assert.Equal(HttpStatusCode.Created, response.Status);
        Assert.Equal("ios", response.Json().GetProperty("platform").GetString());
    }

    [Fact]
    public async Task RegisteringTheSameInstallationAgain_UpdatesItAndCancelsItsOpenDeliveries()
    {
        var account = await Fixture.CreateAccountAsync();
        await Fixture.SetPreferencesAsync(account.AccountId, social: true);
        var installation = Guid.NewGuid();
        var first = await CallAsync(
            HttpMethod.Post,
            "/api/v1/notifications/devices",
            account.Subject,
            Registration(installation));
        var deviceId = first.Json().GetProperty("id").GetGuid();
        var delivery = await Fixture.AddPendingDeliveryAsync(account.AccountId, deviceId);
        Clock.Advance(TimeSpan.FromMinutes(10));

        var second = await CallAsync(
            HttpMethod.Post,
            "/api/v1/notifications/devices",
            account.Subject,
            Registration(installation));

        Assert.Equal(HttpStatusCode.Created, second.Status);
        var body = second.Json();
        Assert.Equal(deviceId, body.GetProperty("id").GetGuid());
        Assert.Equal(Iso(Clock.GetUtcNow()), body.GetProperty("lastRegisteredAt").GetString());
        Assert.NotEqual(Iso(Clock.GetUtcNow()), body.GetProperty("createdAt").GetString());
        var cancelled = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Cancelled, cancelled.Status);
        Assert.Equal("account_changed", cancelled.LastErrorCode);
    }

    [Fact]
    public async Task ATokenThatBelongsToAnotherActiveDevice_IsAConflict()
    {
        var first = await Fixture.CreateAccountAsync();
        var second = await Fixture.CreateAccountAsync();
        var token = NotificationsSeeder.NewToken();
        Assert.Equal(
            HttpStatusCode.Created,
            (await CallAsync(HttpMethod.Post, "/api/v1/notifications/devices", first.Subject, Registration(token: token))).Status);

        var response = await CallAsync(
            HttpMethod.Post,
            "/api/v1/notifications/devices",
            second.Subject,
            Registration(token: token));

        Assert.Equal(HttpStatusCode.Conflict, response.Status);
        Assert.Equal("application/problem+json", response.ContentType);
        var problem = response.Json();
        Assert.Equal("Dispositivo já associado.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "Este token já está associado a outro dispositivo. Remova o vínculo antigo antes de registrar novamente.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task AnInstallationStillActiveForAnotherAccount_IsAConflict()
    {
        var first = await Fixture.CreateAccountAsync();
        var second = await Fixture.CreateAccountAsync();
        var installation = Guid.NewGuid();
        Assert.Equal(
            HttpStatusCode.Created,
            (await CallAsync(HttpMethod.Post, "/api/v1/notifications/devices", first.Subject, Registration(installation))).Status);

        var response = await CallAsync(
            HttpMethod.Post,
            "/api/v1/notifications/devices",
            second.Subject,
            Registration(installation));

        Assert.Equal(HttpStatusCode.Conflict, response.Status);
        var problem = response.Json();
        Assert.Equal("Dispositivo já associado.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "Este dispositivo ainda está associado a outra conta. Encerre a sessão anterior com conexão e tente novamente.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task AnInvalidToken_IsRejectedAsAnInvalidRegistration()
    {
        var account = await Fixture.CreateAccountAsync();

        var response = await CallAsync(
            HttpMethod.Post,
            "/api/v1/notifications/devices",
            account.Subject,
            Registration(token: "ExpoPushToken[a]"));

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.Equal("application/problem+json", response.ContentType);
        var problem = response.Json();
        Assert.Equal("Registro de dispositivo inválido.", problem.GetProperty("title").GetString());
        // Spec 0014 (Q8) removes the " (Parameter 'expoPushToken')" suffix when the module migrates.
        Assert.Equal(
            "O token Expo informado é inválido. (Parameter 'expoPushToken')",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task AMissingToken_IsRejectedAsAnInvalidRegistration()
    {
        var account = await Fixture.CreateAccountAsync();

        var response = await CallAsync(
            HttpMethod.Post,
            "/api/v1/notifications/devices",
            account.Subject,
            new { installationId = Guid.NewGuid(), platform = "android", projectId = PostgresCharacterizationFixture.ExpoProjectId });

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        var problem = response.Json();
        Assert.Equal("Registro de dispositivo inválido.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "Informe um token Expo. (Parameter 'registration')",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task AnUnknownPlatform_IsRefusedAndNothingIsStored()
    {
        var account = await Fixture.CreateAccountAsync();

        var response = await CallAsync(
            HttpMethod.Post,
            "/api/v1/notifications/devices",
            account.Subject,
            Registration(platform: "windows"));

        // The test host runs in Development, where the framework rethrows the bad request and the
        // exception handler answers 500 (a production host answers 400). Either way it is refused.
        Assert.True((int)response.Status >= 400);
        Assert.Equal(0, await Fixture.CountAsync("notification_devices", "account_id", account.AccountId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RegisteringWhilePushIsOffOrForAnotherProject_IsServiceUnavailable(bool pushEnabled)
    {
        var account = await Fixture.CreateAccountAsync();

        var response = await CallAsync(
            HttpMethod.Post,
            "/api/v1/notifications/devices",
            account.Subject,
            Registration(projectId: pushEnabled ? Guid.NewGuid() : null),
            pushEnabled);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.Status);
        Assert.Equal("application/problem+json", response.ContentType);
        var problem = response.Json();
        Assert.Equal("Notificações push indisponíveis.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "O registro push está desabilitado até configurar o projeto e as credenciais Expo.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task RemovingADevice_ReturnsNoContentCancelsItsDeliveriesAndThenNotFound()
    {
        var account = await Fixture.CreateAccountAsync();
        await Fixture.SetPreferencesAsync(account.AccountId, social: true);
        var device = await Fixture.AddDeviceAsync(account.AccountId);
        var delivery = await Fixture.AddPendingDeliveryAsync(account.AccountId, device.Id);

        var removed = await CallAsync(HttpMethod.Delete, $"/api/v1/notifications/devices/{device.Id}", account.Subject);
        var again = await CallAsync(HttpMethod.Delete, $"/api/v1/notifications/devices/{device.Id}", account.Subject);

        Assert.Equal(HttpStatusCode.NoContent, removed.Status);
        Assert.Equal(string.Empty, removed.Body);
        Assert.False((await Fixture.ReadDeviceAsync(device.Id)).IsActive);
        var cancelled = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Cancelled, cancelled.Status);
        Assert.Equal("device_removed", cancelled.LastErrorCode);
        AssertNotFoundBodyOfTheStatusCodePages(again);
    }

    [Fact]
    public async Task RemovingTheDeviceOfAnotherAccount_IsNotFound()
    {
        var owner = await Fixture.CreateAccountAsync();
        var other = await Fixture.CreateAccountAsync();
        var device = await Fixture.AddDeviceAsync(owner.AccountId);

        var response = await CallAsync(HttpMethod.Delete, $"/api/v1/notifications/devices/{device.Id}", other.Subject);

        AssertNotFoundBodyOfTheStatusCodePages(response);
        Assert.True((await Fixture.ReadDeviceAsync(device.Id)).IsActive);
    }

    [Fact]
    public async Task Inbox_PagesTwentyFiveItemsNewestFirstWithAnOpaqueCursor()
    {
        var account = await Fixture.CreateAccountAsync();
        var created = new List<UserNotification>();
        for (var index = 0; index < 26; index++)
        {
            created.Add(await Fixture.AddNotificationAsync(
                account.AccountId,
                createdAt: Clock.GetUtcNow().AddMinutes(-index)));
        }

        var first = await GetAsync(account.Subject, "/api/v1/notifications/inbox");

        Assert.Equal(HttpStatusCode.OK, first.Status);
        var page = first.Json();
        var items = page.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(25, items.Length);
        Assert.Equal(created[0].Id, items[0].GetProperty("id").GetGuid());
        Assert.Equal(created[24].Id, items[24].GetProperty("id").GetGuid());
        var newest = created[0];
        AssertSameJson(
            $$"""{"id":"{{newest.Id}}","category":"social","title":"Título","body":"Corpo da notificação.","deepLink":"/social","createdAt":"{{Iso(newest.CreatedAt)}}","readAt":null}""",
            items[0].GetRawText());
        var cursor = page.GetProperty("nextCursor").GetString()!;
        using var decoded = JsonDocument.Parse(Decode(cursor));
        Assert.Equal(created[24].Id, decoded.RootElement.GetProperty("Id").GetGuid());
        Assert.Equal(created[24].CreatedAt, decoded.RootElement.GetProperty("CreatedAt").GetDateTimeOffset());

        var second = await GetAsync(account.Subject, $"/api/v1/notifications/inbox?cursor={cursor}");

        var secondPage = second.Json();
        var rest = Assert.Single(secondPage.GetProperty("items").EnumerateArray());
        Assert.Equal(created[25].Id, rest.GetProperty("id").GetGuid());
        Assert.Equal(JsonValueKind.Null, secondPage.GetProperty("nextCursor").ValueKind);
    }

    [Fact]
    public async Task Inbox_OfANewAccount_IsEmpty()
    {
        var account = await Fixture.CreateAccountAsync();

        var response = await GetAsync(account.Subject, "/api/v1/notifications/inbox");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        AssertSameJson("""{"items":[],"nextCursor":null}""", response.Body);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("bnVsbA")]
    public async Task Inbox_RejectsAnInvalidCursor(string cursor)
    {
        var account = await Fixture.CreateAccountAsync();

        var response = await GetAsync(account.Subject, $"/api/v1/notifications/inbox?cursor={cursor}");

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.Equal("application/problem+json", response.ContentType);
        var problem = response.Json();
        Assert.Equal("Cursor inválido.", problem.GetProperty("title").GetString());
        // Spec 0014 (Q8) removes the " (Parameter 'encodedCursor')" suffix when the module migrates.
        Assert.Equal(
            "O cursor da caixa de entrada é inválido. (Parameter 'encodedCursor')",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task UnreadCountAndMarkRead_FollowTheInbox()
    {
        var account = await Fixture.CreateAccountAsync();
        var other = await Fixture.CreateAccountAsync();
        var first = await Fixture.AddNotificationAsync(account.AccountId);
        await Fixture.AddNotificationAsync(account.AccountId);
        var foreign = await Fixture.AddNotificationAsync(other.AccountId);

        var before = await GetAsync(account.Subject, "/api/v1/notifications/inbox/unread-count");
        var marked = await PutAsync(account.Subject, $"/api/v1/notifications/inbox/{first.Id}/read", new { });
        var after = await GetAsync(account.Subject, "/api/v1/notifications/inbox/unread-count");
        var foreignAttempt = await PutAsync(account.Subject, $"/api/v1/notifications/inbox/{foreign.Id}/read", new { });
        var unknown = await PutAsync(account.Subject, $"/api/v1/notifications/inbox/{Guid.NewGuid()}/read", new { });

        AssertSameJson("""{"count":2}""", before.Body);
        Assert.Equal(HttpStatusCode.NoContent, marked.Status);
        AssertSameJson("""{"count":1}""", after.Body);
        AssertNotFoundBodyOfTheStatusCodePages(foreignAttempt);
        AssertNotFoundBodyOfTheStatusCodePages(unknown);
        await using var dbContext = Fixture.CreateDbContext();
        Assert.Equal(
            Clock.GetUtcNow(),
            (await dbContext.UserNotifications.SingleAsync(item => item.Id == first.Id)).ReadAt);
        Assert.Null((await dbContext.UserNotifications.SingleAsync(item => item.Id == foreign.Id)).ReadAt);
    }

    [Fact]
    public async Task TheWriteRoutes_AreRateLimitedAfterEightRequestsInAMinute()
    {
        var account = await Fixture.CreateAccountAsync();
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var accepted = await PutAsync(
                account.Subject,
                "/api/v1/notifications/preferences",
                new { socialEnabled = true, newEpisodeEnabled = true, premiereEnabled = true });
            Assert.Equal(HttpStatusCode.OK, accepted.Status);
        }

        var response = await PutAsync(
            account.Subject,
            "/api/v1/notifications/preferences",
            new { socialEnabled = true, newEpisodeEnabled = true, premiereEnabled = true });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.Status);
        Assert.Equal("application/json", response.ContentType);
        var body = response.Json();
        Assert.Equal("Limite de solicitações excedido.", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task TheGamificationPreferencesRoute_IsRateLimitedWithTheSocialWritePolicy()
    {
        var account = await Fixture.CreateAccountAsync();
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var accepted = await PutAsync(
                account.Subject,
                "/api/v1/gamification/preferences",
                new { badgesPublic = true, rankingEnabled = false });
            Assert.Equal(HttpStatusCode.OK, accepted.Status);
        }

        var response = await PutAsync(
            account.Subject,
            "/api/v1/gamification/preferences",
            new { badgesPublic = true, rankingEnabled = false });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.Status);
    }

    private static void AssertNotFoundBodyOfTheStatusCodePages(ApiResult response)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.Status);
        Assert.Equal("application/problem+json", response.ContentType);
        var body = response.Json();
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", body.GetProperty("title").GetString());
        Assert.False(body.TryGetProperty("detail", out _));
    }

    private static string Decode(string cursor)
    {
        var padded = cursor.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }
}

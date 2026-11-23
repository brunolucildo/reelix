using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Reelix.Api.Authentication;
using Reelix.Api.Authentication.Contracts;
using Reelix.Application.Authentication;

namespace Reelix.UnitTests.Api;

/// <summary>
/// The pilot of the DTO pattern and of the single <c>sub</c> reader (spec 0013, R2 and R8):
/// <c>GET /auth/me</c> answers the same JSON as before and every consumer reads the caller through
/// one component.
/// </summary>
public sealed class AuthenticatedAccountContractTests(AuthenticationApiFactory factory)
    : IClassFixture<AuthenticationApiFactory>
{
    private static readonly Guid AccountId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private static readonly DateTimeOffset CreatedAt = DateTimeOffset.Parse("2026-03-04T05:06:07+00:00");

    [Fact]
    public void Response_SerializesWithTheHostOptionsToTheGoldenJson()
    {
        var response = AuthenticatedAccountResponse.From(
            new AuthenticatedAccount(AccountId, "cinefila@example.com", CreatedAt));

        HostJson.AssertSameJson(
            """
            {"id":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee","email":"cinefila@example.com","createdAt":"2026-03-04T05:06:07+00:00"}
            """,
            HostJson.Serialize(factory.Services, response));
    }

    [Fact]
    public void Response_IsByteIdenticalToTheApplicationResultItReplaced()
    {
        var account = new AuthenticatedAccount(AccountId, null, CreatedAt);

        Assert.Equal(
            HostJson.Serialize(factory.Services, account),
            HostJson.Serialize(factory.Services, AuthenticatedAccountResponse.From(account)));
    }

    [Fact]
    public async Task AccountEndpoint_ReturnsTheIdEmailAndCreatedAtOfTheAuthenticatedAccount()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateToken("valid", Guid.NewGuid()));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            ["id", "email", "createdAt"],
            body.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal(
            System.Text.Json.JsonValueKind.Null,
            body.RootElement.GetProperty("email").ValueKind);
        Assert.NotEqual(Guid.Empty, body.RootElement.GetProperty("id").GetGuid());
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("not-a-guid", false)]
    [InlineData("00000000-0000-0000-0000-000000000000", false)]
    [InlineData("11111111-2222-3333-4444-555555555555", true)]
    public void ReadSubjectId_AcceptsOnlyANonEmptyGuid(string? subject, bool expected)
    {
        var user = UserWith(subject);

        Assert.Equal(expected, user.ReadSubjectId(out var subjectId));
        Assert.Equal(expected ? Guid.Parse(subject!) : Guid.Empty, subjectId);
    }

    [Fact]
    public void GetSubject_ReturnsTheRawClaimUsedAsTheRateLimitPartition()
    {
        Assert.Equal("not-a-guid", UserWith("not-a-guid").GetSubject());
        Assert.Null(UserWith(null).GetSubject());
    }

    private static ClaimsPrincipal UserWith(string? subject) =>
        new(new ClaimsIdentity(
            subject is null ? [] : [new Claim("sub", subject)],
            "test"));
}

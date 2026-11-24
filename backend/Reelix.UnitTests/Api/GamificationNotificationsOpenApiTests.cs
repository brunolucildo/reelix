using System.Net;
using System.Text.Json;

namespace Reelix.UnitTests.Api;

/// <summary>
/// The generated OpenAPI documents still describe the 12 routes of Gamification and Notifications
/// after their handlers and DTOs moved (spec 0014, R14).
/// </summary>
public sealed class GamificationNotificationsOpenApiTests(AuthenticationApiFactory factory)
    : IClassFixture<AuthenticationApiFactory>
{
    private static readonly string[] OperationIds =
    [
        "GetGamificationDashboard",
        "UpdateGamificationPreferences",
        "GetGamificationStatistics",
        "GetPublicGamificationBadges",
        "GetMonthlyFriendsRanking",
        "GetNotificationPreferences",
        "UpdateNotificationPreferences",
        "RegisterNotificationDevice",
        "DeactivateNotificationDevice",
        "GetNotificationInbox",
        "GetNotificationUnreadCount",
        "MarkNotificationRead",
    ];

    [Theory]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/openapi/v1.json")]
    public async Task TheDocument_ListsEveryOperationOfBothModules(string path)
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(text);
        foreach (var operationId in OperationIds)
        {
            Assert.Contains($"\"{operationId}\"", text, StringComparison.Ordinal);
        }

        Assert.True(document.RootElement.TryGetProperty("paths", out var paths));
        Assert.True(paths.TryGetProperty("/api/v1/gamification/dashboard", out _));
        Assert.True(paths.TryGetProperty("/api/v1/notifications/devices", out _));
    }
}

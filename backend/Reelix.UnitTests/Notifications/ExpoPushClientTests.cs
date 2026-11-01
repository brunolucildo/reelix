using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Reelix.Application.Notifications;
using Reelix.Infrastructure.Notifications;

namespace Reelix.UnitTests.Notifications;

public sealed class ExpoPushClientTests
{
    [Fact]
    public async Task SendAsync_UsesAuthenticatedExpoRequestAndReturnsTicket()
    {
        var notificationId = Guid.NewGuid();
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("/push/send", request.RequestUri!.AbsolutePath, StringComparison.Ordinal);
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("private-access-token", request.Headers.Authorization?.Parameter);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"data":[{"status":"ok","id":"expo-ticket"}]}""",
                    Encoding.UTF8,
                    "application/json"),
            });
        });
        var pushClient = CreateClient(handler);

        var response = await pushClient.SendAsync(
            new NotificationPushWorkItem(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "ExpoPushToken[abc12345678901234567890]",
                notificationId,
                "Solicitação recebida",
                "Uma pessoa enviou uma solicitação.",
                "/social"),
            CancellationToken.None);

        Assert.True(response.Accepted);
        Assert.Equal("expo-ticket", response.TicketId);
        Assert.False(response.Retryable);
    }

    [Fact]
    public async Task GetReceiptAsync_DisablesUnregisteredDevice()
    {
        const string ticketId = "expo-ticket";
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"data":{"expo-ticket":{"status":"error","details":{"error":"DeviceNotRegistered"}}}}""",
                    Encoding.UTF8,
                    "application/json"),
            }));

        var response = await CreateClient(handler).GetReceiptAsync(ticketId, CancellationToken.None);

        Assert.True(response.Ready);
        Assert.False(response.Delivered);
        Assert.True(response.DeviceNotRegistered);
        Assert.Equal("DeviceNotRegistered", response.ErrorCode);
    }

    private static ExpoPushClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://exp.host/--/api/v2/"),
            },
            Options.Create(new ExpoPushOptions { AccessToken = "private-access-token" }));

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}

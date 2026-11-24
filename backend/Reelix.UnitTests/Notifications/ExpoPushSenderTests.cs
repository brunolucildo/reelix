using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Reelix.Application.Notifications;
using Reelix.Infrastructure.Notifications;

namespace Reelix.UnitTests.Notifications;

public sealed class ExpoPushSenderTests
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
            new PushMessage(
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

    [Fact]
    public async Task SendAsync_ATransportFailureBecomesARetryableResultWithTheSameCodeAsBefore()
    {
        var sender = CreateClient(new StubHttpMessageHandler((_, _) => throw new HttpRequestException("connection refused")));

        var response = await sender.SendAsync(Message(), CancellationToken.None);

        Assert.False(response.Accepted);
        Assert.True(response.Retryable);
        Assert.False(response.DeviceNotRegistered);
        Assert.Equal("expo_transport_error", response.ErrorCode);
    }

    [Fact]
    public async Task SendAsync_ATimeoutBecomesARetryableResultWithTheTimeoutCode()
    {
        var sender = CreateClient(new StubHttpMessageHandler((_, _) => throw new TaskCanceledException("timed out")));

        var response = await sender.SendAsync(Message(), CancellationToken.None);

        Assert.Equal("expo_timeout", response.ErrorCode);
        Assert.True(response.Retryable);
    }

    [Fact]
    public async Task SendAsync_AnUnreadableAnswerBecomesARetryableTransportError()
    {
        var sender = CreateClient(new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not json", Encoding.UTF8, "application/json"),
            })));

        var response = await sender.SendAsync(Message(), CancellationToken.None);

        Assert.Equal("expo_transport_error", response.ErrorCode);
        Assert.True(response.Retryable);
    }

    [Fact]
    public async Task SendAsync_ACancelledRequestStillThrows()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var sender = CreateClient(new StubHttpMessageHandler((_, token) =>
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sender.SendAsync(Message(), source.Token));
    }

    [Fact]
    public async Task SendAsync_TooManyRequestsWithRetryAfter_CarriesTheClampedWait()
    {
        var sender = CreateClient(new StubHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(99999));
            return Task.FromResult(response);
        }));

        var result = await sender.SendAsync(Message(), CancellationToken.None);

        Assert.Equal("http_429", result.ErrorCode);
        Assert.Equal(TimeSpan.FromSeconds(3600), result.RetryAfter);
        Assert.True(result.Retryable);
    }

    [Fact]
    public async Task GetReceiptAsync_ATransportFailureStillPropagates()
    {
        var sender = CreateClient(new StubHttpMessageHandler((_, _) => throw new HttpRequestException("connection refused")));

        await Assert.ThrowsAsync<HttpRequestException>(() => sender.GetReceiptAsync("ticket", CancellationToken.None));
    }

    private static PushMessage Message() =>
        new("ExpoPushToken[abc12345678901234567890]", Guid.NewGuid(), "Título", "Corpo", "/social");

    private static ExpoPushSender CreateClient(HttpMessageHandler handler) =>
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

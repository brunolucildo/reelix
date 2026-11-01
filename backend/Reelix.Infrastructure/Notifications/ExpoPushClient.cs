using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Reelix.Application.Notifications;

namespace Reelix.Infrastructure.Notifications;

public sealed class ExpoPushClient(
    HttpClient httpClient,
    IOptions<ExpoPushOptions> options) : IExpoPushClient
{
    private readonly ExpoPushOptions _options = options.Value;

    public async Task<ExpoPushResponse> SendAsync(
        NotificationPushWorkItem workItem,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "push/send")
        {
            Content = JsonContent.Create(new
            {
                to = workItem.PushToken,
                title = workItem.Title,
                body = workItem.Body,
                data = new
                {
                    notificationId = workItem.NotificationId,
                    deepLink = workItem.DeepLink,
                },
                sound = "default",
                priority = "high",
                channelId = "default",
            }),
        };
        AddAccessToken(request);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new ExpoPushResponse(
                false,
                null,
                $"http_{(int)response.StatusCode}",
                GetRetryAfter(response) ?? (
                    response.StatusCode == HttpStatusCode.TooManyRequests
                        ? TimeSpan.FromMinutes(1)
                        : null),
                false,
                response.StatusCode == HttpStatusCode.RequestTimeout ||
                    response.StatusCode == HttpStatusCode.TooManyRequests ||
                    (int)response.StatusCode >= 500);
        }

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Array &&
            data.GetArrayLength() > 0)
        {
            var ticket = data[0];
            if (ticket.ValueKind == JsonValueKind.Object &&
                ticket.TryGetProperty("status", out var status) &&
                status.ValueKind == JsonValueKind.String &&
                status.GetString() == "ok" &&
                ticket.TryGetProperty("id", out var id) &&
                id.ValueKind == JsonValueKind.String)
            {
                return new ExpoPushResponse(true, id.GetString(), null, null, false, false);
            }

            if (ticket.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("A resposta do Expo Push contém um ticket inválido.");
            }

            var error = ReadError(ticket);
            return new ExpoPushResponse(
                false,
                null,
                error,
                null,
                error == "DeviceNotRegistered",
                IsRetryableError(error));
        }

        throw new HttpRequestException("A resposta do Expo Push não contém um ticket válido.");
    }

    public async Task<ExpoReceiptResponse> GetReceiptAsync(
        string ticketId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "push/getReceipts")
        {
            Content = JsonContent.Create(new { ids = new[] { ticketId } }),
        };
        AddAccessToken(request);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new ExpoReceiptResponse(
                false,
                false,
                $"http_{(int)response.StatusCode}",
                false);
        }

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("data", out var data) ||
            data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty(ticketId, out var receipt))
        {
            return new ExpoReceiptResponse(false, false, "receipt_not_ready", false);
        }

        if (receipt.ValueKind == JsonValueKind.Object &&
            receipt.TryGetProperty("status", out var status) &&
            status.ValueKind == JsonValueKind.String &&
            status.GetString() == "ok")
        {
            return new ExpoReceiptResponse(true, true, null, false);
        }

        if (receipt.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("A resposta do Expo Push contém um receipt inválido.");
        }

        var error = ReadError(receipt);
        return new ExpoReceiptResponse(
            true,
            false,
            error,
            error == "DeviceNotRegistered");
    }

    private void AddAccessToken(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        }
    }

    private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is TimeSpan delta)
        {
            return TimeSpan.FromSeconds(Math.Clamp(delta.TotalSeconds, 1, 3600));
        }

        if (retryAfter?.Date is DateTimeOffset date)
        {
            return TimeSpan.FromSeconds(Math.Clamp(
                (date - DateTimeOffset.UtcNow).TotalSeconds,
                1,
                3600));
        }

        return null;
    }

    private static string ReadError(JsonElement item)
    {
        if (item.TryGetProperty("details", out var details) &&
            details.ValueKind == JsonValueKind.Object &&
            details.TryGetProperty("error", out var detailedError))
        {
            return detailedError.ValueKind == JsonValueKind.String
                ? NormalizeErrorCode(detailedError.GetString())
                : "expo_push_rejected";
        }

        if (item.TryGetProperty("message", out var message) &&
            message.ValueKind == JsonValueKind.String)
        {
            return NormalizeErrorCode(message.GetString());
        }

        return "expo_push_rejected";
    }

    private static bool IsRetryableError(string errorCode) =>
        errorCode is "MessageRateExceeded" or "ExpoServerError" or "ServiceUnavailable";

    private static string NormalizeErrorCode(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 80 &&
        value.All(character =>
            char.IsAsciiLetterOrDigit(character) ||
            character is '_' or '-' or '.')
            ? value
            : "expo_push_rejected";
}

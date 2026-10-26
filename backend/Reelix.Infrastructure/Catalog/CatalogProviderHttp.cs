using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Reelix.Application.Catalog;

namespace Reelix.Infrastructure.Catalog;

internal static class CatalogProviderHttp
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpRequestMessage request,
        CatalogProvider provider,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            return await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Catalog provider {Provider} request timed out.", provider);
            throw new CatalogProviderException(
                "catalog_provider_timeout",
                "The catalog provider request timed out.",
                (int)HttpStatusCode.GatewayTimeout);
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("Catalog provider {Provider} could not be reached.", provider);
            throw new CatalogProviderException(
                "catalog_provider_unavailable",
                "The catalog provider is temporarily unavailable.",
                (int)HttpStatusCode.ServiceUnavailable);
        }
    }

    public static async Task<T> ReadJsonAsync<T>(
        HttpResponseMessage response,
        CatalogProvider provider,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(
                JsonOptions,
                cancellationToken)
                ?? throw InvalidResponse(provider, logger);
        }
        catch (JsonException)
        {
            throw InvalidResponse(provider, logger);
        }
    }

    public static CatalogProviderException GetHttpError(
        HttpResponseMessage response,
        CatalogProvider provider,
        ILogger logger)
    {
        var status = response.StatusCode;
        var retryAfter = response.Headers.RetryAfter?.Delta;
        if (response.Headers.RetryAfter?.Date is { } retryDate)
        {
            retryAfter = retryDate - DateTimeOffset.UtcNow;
        }

        if (status == HttpStatusCode.TooManyRequests)
        {
            logger.LogWarning("Catalog provider {Provider} rate limit was reached.", provider);
            return new CatalogProviderException(
                "catalog_provider_rate_limited",
                "The catalog provider rate limit was reached. Try again later.",
                (int)HttpStatusCode.TooManyRequests,
                retryAfter is { } delay && delay > TimeSpan.Zero ? delay : null);
        }

        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            logger.LogError("Catalog provider {Provider} rejected its configured credentials.", provider);
            return new CatalogProviderException(
                "catalog_provider_configuration_error",
                "The catalog provider rejected its credentials. Check the server configuration.",
                (int)HttpStatusCode.ServiceUnavailable);
        }

        logger.LogWarning(
            "Catalog provider {Provider} returned HTTP {StatusCode}.",
            provider,
            (int)status);
        return new CatalogProviderException(
            (int)status >= (int)HttpStatusCode.InternalServerError
                ? "catalog_provider_unavailable"
                : "catalog_provider_bad_response",
            (int)status >= (int)HttpStatusCode.InternalServerError
                ? "The catalog provider is temporarily unavailable."
                : "The catalog provider returned an unexpected response.",
            (int)status >= (int)HttpStatusCode.InternalServerError
                ? (int)HttpStatusCode.ServiceUnavailable
                : (int)HttpStatusCode.BadGateway);
    }

    public static CatalogProviderException InvalidResponse(
        CatalogProvider provider,
        ILogger logger)
    {
        logger.LogWarning("Catalog provider {Provider} returned an invalid response.", provider);
        return new CatalogProviderException(
            "catalog_provider_invalid_response",
            "The catalog provider returned an invalid response.",
            (int)HttpStatusCode.BadGateway);
    }
}

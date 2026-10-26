using System.Globalization;
using System.Net;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reelix.Application.Catalog;

namespace Reelix.Infrastructure.Catalog;

public sealed class OmdbCatalogOptions
{
    public const string SectionName = "Catalog:Omdb";

    public string? ApiKey { get; set; }
}

public sealed class OmdbCatalogProvider(
    HttpClient httpClient,
    IOptions<OmdbCatalogOptions> options,
    ILogger<OmdbCatalogProvider> logger) : ICatalogProvider
{
    public CatalogProvider Provider => CatalogProvider.Omdb;

    public async Task<ProviderCatalogPage> SearchAsync(
        string query,
        CatalogContentType? contentType,
        int page,
        string language,
        CancellationToken cancellationToken)
    {
        _ = language;
        var parameters = new Dictionary<string, string>
        {
            ["s"] = query,
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
        };
        if (contentType is not null)
        {
            parameters["type"] = ToOmdbType(contentType.Value);
        }

        using var response = await SendAsync(parameters, cancellationToken);
        response.EnsureSuccessStatusCodeOrThrow(Provider, logger);
        var result = await CatalogProviderHttp.ReadJsonAsync<OmdbResponse>(
            response,
            Provider,
            logger,
            cancellationToken);
        if (!IsSuccessful(result.Response))
        {
            ThrowOmdbError(result.Error, searchRequest: true);
        }

        var items = result.Search
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.ImdbId) &&
                !string.IsNullOrWhiteSpace(item.Title) &&
                TryParseOmdbType(item.Type, out _))
            .Select(item =>
            {
                TryParseOmdbType(item.Type, out var type);
                return new ProviderCatalogTitle(
                    item.ImdbId!,
                    type,
                    item.Title!,
                    null,
                    null,
                    [],
                    ParseDate(item.Year),
                    null,
                    null,
                    null,
                    null);
            })
            .ToArray();
        var totalResults = ParsePositiveInt(result.TotalResults);

        return new ProviderCatalogPage(
            items,
            page,
            Math.Max(1, (int)Math.Ceiling(totalResults / 10d)),
            totalResults);
    }

    public async Task<ProviderCatalogTitle?> GetDetailsAsync(
        CatalogContentType contentType,
        string externalId,
        string language,
        CancellationToken cancellationToken)
    {
        _ = language;
        using var response = await SendAsync(
            new Dictionary<string, string>
            {
                ["i"] = externalId,
                ["plot"] = "full",
            },
            cancellationToken);
        response.EnsureSuccessStatusCodeOrThrow(Provider, logger);
        var result = await CatalogProviderHttp.ReadJsonAsync<OmdbResponse>(
            response,
            Provider,
            logger,
            cancellationToken);
        if (!IsSuccessful(result.Response))
        {
            ThrowOmdbError(result.Error, searchRequest: false);
        }

        if (string.IsNullOrWhiteSpace(result.ImdbId) ||
            string.IsNullOrWhiteSpace(result.Title) ||
            !TryParseOmdbType(result.Type, out var actualType) ||
            actualType != contentType)
        {
            return null;
        }

        return new ProviderCatalogTitle(
            result.ImdbId,
            actualType,
            result.Title,
            null,
            ParseValue(result.Plot),
            ParseGenres(result.Genre),
            ParseDate(result.Released),
            null,
            ParseRuntime(result.Runtime),
            null,
            null);
    }

    public async Task<ProviderCatalogSeason?> GetSeasonAsync(
        string externalSeriesId,
        int seasonNumber,
        string language,
        CancellationToken cancellationToken)
    {
        _ = language;
        using var response = await SendAsync(
            new Dictionary<string, string>
            {
                ["i"] = externalSeriesId,
                ["Season"] = seasonNumber.ToString(CultureInfo.InvariantCulture),
            },
            cancellationToken);
        response.EnsureSuccessStatusCodeOrThrow(Provider, logger);
        var result = await CatalogProviderHttp.ReadJsonAsync<OmdbResponse>(
            response,
            Provider,
            logger,
            cancellationToken);
        if (!IsSuccessful(result.Response))
        {
            ThrowOmdbError(result.Error, searchRequest: false);
        }

        return new ProviderCatalogSeason(
            ParseValue(result.Title),
            ParseNonNegativeInt(result.Season) ?? seasonNumber,
            ParseDate(result.Released),
            result.Episodes
                .Where(episode =>
                    !string.IsNullOrWhiteSpace(episode.ImdbId) &&
                    !string.IsNullOrWhiteSpace(episode.Title))
                .Select(episode => new ProviderCatalogEpisode(
                    episode.ImdbId!,
                    ParseOptionalPositiveInt(episode.Episode),
                    episode.Title!,
                    null,
                    ParseDate(episode.Released),
                    null))
                .ToArray());
    }

    private async Task<HttpResponseMessage> SendAsync(
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        var apiKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new CatalogProviderException(
                "catalog_provider_not_configured",
                "OMDb is not configured. Set Catalog:Omdb:ApiKey on the server.",
                (int)HttpStatusCode.ServiceUnavailable);
        }

        var query = new Dictionary<string, string>(parameters)
        {
            ["apikey"] = apiKey,
        };
        var queryString = string.Join(
            "&",
            query.Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
        using var request = new HttpRequestMessage(HttpMethod.Get, $"?{queryString}");
        request.Headers.Accept.ParseAdd("application/json");
        return await CatalogProviderHttp.SendAsync(
            httpClient,
            request,
            Provider,
            logger,
            cancellationToken);
    }

    private void ThrowOmdbError(string? error, bool searchRequest)
    {
        var normalizedError = error?.Trim() ?? "The provider returned an unsuccessful response.";
        if (normalizedError.Contains("request limit", StringComparison.OrdinalIgnoreCase) ||
            normalizedError.Contains("too many requests", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Catalog provider {Provider} rate limit was reached.", Provider);
            throw new CatalogProviderException(
                "catalog_provider_rate_limited",
                "The catalog provider rate limit was reached. Try again later.",
                (int)HttpStatusCode.TooManyRequests);
        }

        if (normalizedError.Contains("invalid api key", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError("Catalog provider {Provider} rejected its configured credentials.", Provider);
            throw new CatalogProviderException(
                "catalog_provider_configuration_error",
                "OMDb rejected its API key. Check the server configuration.",
                (int)HttpStatusCode.ServiceUnavailable);
        }

        if (normalizedError.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            if (searchRequest)
            {
                return;
            }

            throw new CatalogProviderException(
                "catalog_title_not_found",
                "The requested title was not found.",
                (int)HttpStatusCode.NotFound);
        }

        logger.LogWarning("Catalog provider {Provider} returned an unsuccessful API response.", Provider);
        throw new CatalogProviderException(
            "catalog_provider_bad_response",
            "The catalog provider could not complete the request.",
            (int)HttpStatusCode.BadGateway);
    }

    private static string ToOmdbType(CatalogContentType contentType) =>
        contentType == CatalogContentType.Movie ? "movie" : "series";

    private static bool TryParseOmdbType(string? value, out CatalogContentType type)
    {
        if (string.Equals(value, "movie", StringComparison.OrdinalIgnoreCase))
        {
            type = CatalogContentType.Movie;
            return true;
        }

        if (string.Equals(value, "series", StringComparison.OrdinalIgnoreCase))
        {
            type = CatalogContentType.Series;
            return true;
        }

        type = default;
        return false;
    }

    private static IReadOnlyList<string> ParseGenres(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

    private static string? ParseValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        string.Equals(value, "N/A", StringComparison.OrdinalIgnoreCase)
            ? null
            : value;

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    private static int? ParseRuntime(string? value)
    {
        var minutes = value?.Split(' ', StringSplitOptions.TrimEntries)[0];
        return int.TryParse(minutes, NumberStyles.None, CultureInfo.InvariantCulture, out var runtime) &&
            runtime > 0
                ? runtime
                : null;
    }

    private static int ParsePositiveInt(string? value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) &&
        number > 0
            ? number
            : 0;

    private static int? ParseNonNegativeInt(string? value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) &&
        number >= 0
            ? number
            : null;

    private static int? ParseOptionalPositiveInt(string? value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) &&
        number > 0
            ? number
            : null;

    private static bool IsSuccessful(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}

internal sealed class OmdbResponse
{
    public string? Response { get; set; }

    public string? Error { get; set; }

    public string? TotalResults { get; set; }

    public List<OmdbSearchItem> Search { get; set; } = [];

    [JsonPropertyName("imdbID")]
    public string? ImdbId { get; set; }

    public string? Type { get; set; }

    public string? Title { get; set; }

    public string? Year { get; set; }

    public string? Released { get; set; }

    public string? Genre { get; set; }

    public string? Runtime { get; set; }

    public string? Poster { get; set; }

    public string? Plot { get; set; }

    public string? Season { get; set; }

    public List<OmdbEpisode> Episodes { get; set; } = [];
}

internal sealed class OmdbSearchItem
{
    [JsonPropertyName("imdbID")]
    public string? ImdbId { get; set; }

    public string? Type { get; set; }

    public string? Title { get; set; }

    public string? Year { get; set; }

    public string? Poster { get; set; }
}

internal sealed class OmdbEpisode
{
    [JsonPropertyName("imdbID")]
    public string? ImdbId { get; set; }

    public string? Title { get; set; }

    public string? Episode { get; set; }

    public string? Released { get; set; }
}

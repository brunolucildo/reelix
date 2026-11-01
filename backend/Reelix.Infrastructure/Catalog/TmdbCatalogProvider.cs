using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reelix.Application.Catalog;

namespace Reelix.Infrastructure.Catalog;

public sealed class TmdbCatalogOptions
{
    public const string SectionName = "Catalog:Tmdb";

    public string? AccessToken { get; set; }
}

public sealed class TmdbCatalogProvider(
    HttpClient httpClient,
    IOptions<TmdbCatalogOptions> options,
    ILogger<TmdbCatalogProvider> logger,
    IMemoryCache? memoryCache = null) : ICatalogProvider, ICatalogGenreProvider, ICatalogDiscoveryProvider
{
    private static readonly TimeSpan DiscoveryCacheDuration = TimeSpan.FromMinutes(10);

    public CatalogProvider Provider => CatalogProvider.Tmdb;

    public async Task<IReadOnlyList<CatalogGenre>> GetGenresAsync(
        CatalogContentType contentType,
        string language,
        CancellationToken cancellationToken)
    {
        var kind = contentType == CatalogContentType.Movie ? "movie" : "tv";
        using var response = await SendAsync(
            $"3/genre/{kind}/list?language={Uri.EscapeDataString(language)}",
            cancellationToken);
        response.EnsureSuccessStatusCodeOrThrow(Provider, logger);
        var result = await CatalogProviderHttp.ReadJsonAsync<TmdbGenreListResponse>(
            response,
            Provider,
            logger,
            cancellationToken);

        return result.Genres
            .Where(genre => genre.Id > 0 && !string.IsNullOrWhiteSpace(genre.Name))
            .Select(genre => new CatalogGenre(genre.Id, genre.Name!.Trim()))
            .ToArray();
    }

    public async Task<ProviderCatalogPage> SearchAsync(
        string query,
        CatalogContentType? contentType,
        int page,
        string language,
        CancellationToken cancellationToken)
    {
        var path = contentType switch
        {
            CatalogContentType.Movie => "3/search/movie",
            CatalogContentType.Series => "3/search/tv",
            _ => "3/search/multi",
        };
        var uri = $"{path}?query={Uri.EscapeDataString(query)}" +
            $"&include_adult=false&page={page}&language={Uri.EscapeDataString(language)}";
        using var response = await SendAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCodeOrThrow(Provider, logger);

        var pageResult = await CatalogProviderHttp.ReadJsonAsync<TmdbPageResponse>(
            response,
            Provider,
            logger,
            cancellationToken);
        var items = pageResult.Results
            .Where(item => item.Id > 0 && !string.IsNullOrWhiteSpace(item.Title ?? item.Name))
            .Select(item => Map(item, contentType))
            .Where(item => item is not null)
            .Cast<ProviderCatalogTitle>()
            .ToArray();

        return new ProviderCatalogPage(
            items,
            Math.Max(1, pageResult.Page),
            Math.Max(1, pageResult.TotalPages),
            Math.Max(0, pageResult.TotalResults));
    }

    public async Task<ProviderCatalogPage> DiscoverAsync(
        CatalogContentType contentType,
        CatalogDiscoveryCategory category,
        IReadOnlyCollection<int> genreIds,
        int page,
        string language,
        CancellationToken cancellationToken)
    {
        var cacheKey = string.Join(
            ":",
            "tmdb-discovery",
            contentType.ToString(),
            category.ToString(),
            string.Join(",", genreIds.OrderBy(id => id)),
            page.ToString(CultureInfo.InvariantCulture),
            language);
        if (memoryCache is not null &&
            memoryCache.TryGetValue(cacheKey, out ProviderCatalogPage? cachedPage) &&
            cachedPage is not null)
        {
            return cachedPage;
        }

        var path = contentType == CatalogContentType.Movie ? "movie" : "tv";
        var dateField = contentType == CatalogContentType.Movie
            ? "primary_release_date"
            : "first_air_date";
        var sortOrder = category == CatalogDiscoveryCategory.Upcoming
            ? $"{dateField}.asc"
            : "popularity.desc";
        var query = new List<string>
        {
            "include_adult=false",
            $"language={Uri.EscapeDataString(language)}",
            $"page={page}",
            $"sort_by={sortOrder}",
        };
        if (category == CatalogDiscoveryCategory.Upcoming)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            query.Add($"{dateField}.gte={today:yyyy-MM-dd}");
            query.Add($"{dateField}.lte={today.AddMonths(12):yyyy-MM-dd}");
        }

        if (genreIds.Count > 0)
        {
            query.Add($"with_genres={string.Join(",", genreIds.OrderBy(id => id))}");
        }

        using var response = await SendAsync(
            $"3/discover/{path}?{string.Join('&', query)}",
            cancellationToken);
        response.EnsureSuccessStatusCodeOrThrow(Provider, logger);
        var pageResult = await CatalogProviderHttp.ReadJsonAsync<TmdbPageResponse>(
            response,
            Provider,
            logger,
            cancellationToken);
        var items = pageResult.Results
            .Where(item => item.Id > 0 && !string.IsNullOrWhiteSpace(item.Title ?? item.Name))
            .Select(item => Map(item, contentType))
            .Where(item => item is not null)
            .Cast<ProviderCatalogTitle>()
            .ToArray();
        var result = new ProviderCatalogPage(
            items,
            Math.Max(1, pageResult.Page),
            Math.Max(1, pageResult.TotalPages),
            Math.Max(0, pageResult.TotalResults));

        memoryCache?.Set(
            cacheKey,
            result,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DiscoveryCacheDuration,
                Size = 1,
            });
        return result;
    }

    public async Task<ProviderCatalogTitle?> GetDetailsAsync(
        CatalogContentType contentType,
        string externalId,
        string language,
        CancellationToken cancellationToken)
    {
        var id = ParseNumericId(externalId);
        var kind = contentType == CatalogContentType.Movie ? "movie" : "tv";
        using var response = await SendAsync(
            $"3/{kind}/{id}?language={Uri.EscapeDataString(language)}",
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCodeOrThrow(Provider, logger);
        var details = await CatalogProviderHttp.ReadJsonAsync<TmdbTitleResponse>(
            response,
            Provider,
            logger,
            cancellationToken);
        return Map(details, contentType);
    }

    public async Task<ProviderCatalogSeason?> GetSeasonAsync(
        string externalSeriesId,
        int seasonNumber,
        string language,
        CancellationToken cancellationToken)
    {
        var id = ParseNumericId(externalSeriesId);
        using var response = await SendAsync(
            $"3/tv/{id}/season/{seasonNumber}?language={Uri.EscapeDataString(language)}",
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCodeOrThrow(Provider, logger);
        var season = await CatalogProviderHttp.ReadJsonAsync<TmdbSeasonResponse>(
            response,
            Provider,
            logger,
            cancellationToken);
        return new ProviderCatalogSeason(
            EmptyToNull(season.Name),
            season.SeasonNumber,
            ParseDate(season.AirDate),
            season.Episodes
                .Where(episode => episode.Id > 0 && !string.IsNullOrWhiteSpace(episode.Name))
                .Select(episode => new ProviderCatalogEpisode(
                    episode.Id.ToString(CultureInfo.InvariantCulture),
                    episode.EpisodeNumber,
                    episode.Name!,
                    EmptyToNull(episode.Overview),
                    ParseDate(episode.AirDate),
                    episode.Runtime is > 0 ? episode.Runtime : null))
                .ToArray());
    }

    private async Task<HttpResponseMessage> SendAsync(string relativeUri, CancellationToken cancellationToken)
    {
        var token = options.Value.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogError("Catalog provider {Provider} is not configured. Set Catalog:Tmdb:AccessToken on the server.", Provider);
            throw new CatalogProviderException(
                "catalog_provider_not_configured",
                "O catálogo não está configurado no servidor.",
                (int)HttpStatusCode.ServiceUnavailable);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/json");
        return await CatalogProviderHttp.SendAsync(
            httpClient,
            request,
            Provider,
            logger,
            cancellationToken);
    }

    private static ProviderCatalogTitle? Map(
        TmdbTitleResponse item,
        CatalogContentType? requestedType)
    {
        var type = requestedType ?? item.MediaType switch
        {
            "movie" => CatalogContentType.Movie,
            "tv" => CatalogContentType.Series,
            _ => (CatalogContentType?)null,
        };
        if (type is null || item.Id <= 0)
        {
            return null;
        }

        var title = type == CatalogContentType.Movie ? item.Title : item.Name;
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        return new ProviderCatalogTitle(
            item.Id.ToString(CultureInfo.InvariantCulture),
            type.Value,
            title,
            EmptyToNull(type == CatalogContentType.Movie ? item.OriginalTitle : item.OriginalName),
            EmptyToNull(item.Overview),
            item.Genres
                .Select(genre => genre.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            ParseDate(type == CatalogContentType.Movie ? item.ReleaseDate : item.FirstAirDate),
            EmptyToNull(item.OriginalLanguage),
            item.Runtime is > 0 ? item.Runtime : null,
            GetImageUrl(item.PosterPath, "w500"),
            GetImageUrl(item.BackdropPath, "w1280"))
        {
            GenreIds = (item.GenreIds.Count > 0
                    ? item.GenreIds
                    : item.Genres.Select(genre => genre.Id).ToList())
                .Where(id => id > 0)
                .Distinct()
                .ToArray(),
        };
    }

    private static int ParseNumericId(string value)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
        {
            throw new CatalogProviderException(
                "catalog_invalid_external_id",
                "Identificadores TMDB devem ser números inteiros positivos.",
                (int)HttpStatusCode.BadRequest);
        }

        return id;
    }

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string? GetImageUrl(string? path, string size) =>
        string.IsNullOrWhiteSpace(path)
            ? null
            : $"https://image.tmdb.org/t/p/{size}/{path.TrimStart('/')}";

    private static ProviderCatalogTitle? Map(
        TmdbTitleResponse item,
        CatalogContentType contentType) =>
        Map(item, (CatalogContentType?)contentType);
}

internal static class HttpResponseMessageExtensions
{
    public static void EnsureSuccessStatusCodeOrThrow(
        this HttpResponseMessage response,
        CatalogProvider provider,
        ILogger logger)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw CatalogProviderHttp.GetHttpError(response, provider, logger);
        }
    }
}

internal sealed class TmdbPageResponse
{
    public int Page { get; set; }

    public List<TmdbTitleResponse> Results { get; set; } = [];

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("total_results")]
    public int TotalResults { get; set; }
}

internal sealed class TmdbTitleResponse
{
    public int Id { get; set; }

    [JsonPropertyName("media_type")]
    public string? MediaType { get; set; }

    public string? Title { get; set; }

    public string? Name { get; set; }

    [JsonPropertyName("original_title")]
    public string? OriginalTitle { get; set; }

    [JsonPropertyName("original_name")]
    public string? OriginalName { get; set; }

    public string? Overview { get; set; }

    [JsonPropertyName("genre_ids")]
    public List<int> GenreIds { get; set; } = [];

    public List<TmdbGenre> Genres { get; set; } = [];

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("first_air_date")]
    public string? FirstAirDate { get; set; }

    [JsonPropertyName("original_language")]
    public string? OriginalLanguage { get; set; }

    public int? Runtime { get; set; }

    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }

    [JsonPropertyName("backdrop_path")]
    public string? BackdropPath { get; set; }
}

internal sealed class TmdbGenre
{
    public int Id { get; set; }

    public string? Name { get; set; }
}

internal sealed class TmdbGenreListResponse
{
    public List<TmdbGenre> Genres { get; set; } = [];
}

internal sealed class TmdbSeasonResponse
{
    public string? Name { get; set; }

    [JsonPropertyName("season_number")]
    public int SeasonNumber { get; set; }

    [JsonPropertyName("air_date")]
    public string? AirDate { get; set; }

    public List<TmdbEpisodeResponse> Episodes { get; set; } = [];
}

internal sealed class TmdbEpisodeResponse
{
    public int Id { get; set; }

    [JsonPropertyName("episode_number")]
    public int? EpisodeNumber { get; set; }

    public string? Name { get; set; }

    public string? Overview { get; set; }

    [JsonPropertyName("air_date")]
    public string? AirDate { get; set; }

    public int? Runtime { get; set; }
}

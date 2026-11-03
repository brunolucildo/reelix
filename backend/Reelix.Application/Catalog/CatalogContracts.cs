namespace Reelix.Application.Catalog;

public enum CatalogProvider
{
    Tmdb,
    Omdb,
}

public enum CatalogContentType
{
    Movie,
    Series,
}

public enum CatalogDiscoveryCategory
{
    Popular,
    Upcoming,
}

public enum CatalogReferenceType
{
    Movie,
    Series,
    Season,
    Episode,
}

public sealed record CatalogReferenceKey(string ExternalId, CatalogReferenceType Type);

public sealed record ProviderCatalogTitle(
    string ExternalId,
    CatalogContentType ContentType,
    string Title,
    string? OriginalTitle,
    string? Overview,
    IReadOnlyList<string> Genres,
    DateOnly? ReleaseDate,
    string? OriginalLanguage,
    int? RuntimeMinutes,
    string? PosterUrl,
    string? BackdropUrl)
{
    public IReadOnlyList<int> GenreIds { get; init; } = [];
}

public sealed record CatalogTitle(
    Guid Id,
    CatalogProvider Provider,
    string ExternalId,
    CatalogContentType ContentType,
    string Title,
    string? OriginalTitle,
    string? Overview,
    IReadOnlyList<string> Genres,
    DateOnly? ReleaseDate,
    string? OriginalLanguage,
    int? RuntimeMinutes,
    string? PosterUrl,
    string? BackdropUrl);

public sealed record CatalogGenre(int Id, string Name);

public sealed record CatalogMetadataSnapshot(
    CatalogReferenceKey Reference,
    int? RuntimeMinutes,
    IReadOnlyList<int> GenreIds);

public sealed record ProviderCatalogPage(
    IReadOnlyList<ProviderCatalogTitle> Items,
    int Page,
    int TotalPages,
    int TotalResults);

public sealed record CatalogSearchPage(
    IReadOnlyList<CatalogTitle> Items,
    int Page,
    int TotalPages,
    int TotalResults,
    CatalogProvider Provider);

public sealed record CatalogDiscoveryPage(
    IReadOnlyList<CatalogTitle> Items,
    int Page,
    int TotalPages,
    int TotalResults,
    CatalogProvider Provider,
    CatalogContentType ContentType,
    CatalogDiscoveryCategory Category);

public sealed record ProviderCatalogEpisode(
    string ExternalId,
    int? EpisodeNumber,
    string Title,
    string? Overview,
    DateOnly? ReleaseDate,
    int? RuntimeMinutes,
    string? StillUrl = null);

public sealed record ProviderCatalogSeason(
    string? Title,
    int SeasonNumber,
    DateOnly? ReleaseDate,
    IReadOnlyList<ProviderCatalogEpisode> Episodes);

public sealed record ProviderCatalogSeasonSummary(
    int SeasonNumber,
    string? Title,
    DateOnly? ReleaseDate);

public sealed record CatalogSeasonSummary(
    int SeasonNumber,
    string? Title,
    DateOnly? ReleaseDate);

public sealed record CatalogEpisode(
    Guid Id,
    string ExternalId,
    int? EpisodeNumber,
    string Title,
    string? Overview,
    DateOnly? ReleaseDate,
    int? RuntimeMinutes,
    string? StillUrl = null);

public sealed record CatalogSeason(
    Guid Id,
    Guid SeriesId,
    CatalogProvider Provider,
    string ExternalSeriesId,
    string? Title,
    int SeasonNumber,
    DateOnly? ReleaseDate,
    IReadOnlyList<CatalogEpisode> Episodes);

public interface ICatalogProvider
{
    CatalogProvider Provider { get; }

    Task<ProviderCatalogPage> SearchAsync(
        string query,
        CatalogContentType? contentType,
        int page,
        string language,
        CancellationToken cancellationToken);

    Task<ProviderCatalogTitle?> GetDetailsAsync(
        CatalogContentType contentType,
        string externalId,
        string language,
        CancellationToken cancellationToken);

    Task<ProviderCatalogSeason?> GetSeasonAsync(
        string externalSeriesId,
        int seasonNumber,
        string language,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProviderCatalogSeasonSummary>?> GetSeasonsAsync(
        string externalSeriesId,
        string language,
        CancellationToken cancellationToken);
}

public interface ICatalogGenreProvider
{
    CatalogProvider Provider { get; }

    Task<IReadOnlyList<CatalogGenre>> GetGenresAsync(
        CatalogContentType contentType,
        string language,
        CancellationToken cancellationToken);
}

public interface ICatalogDiscoveryProvider
{
    CatalogProvider Provider { get; }

    Task<ProviderCatalogPage> DiscoverAsync(
        CatalogContentType contentType,
        CatalogDiscoveryCategory category,
        IReadOnlyCollection<int> genreIds,
        int page,
        string language,
        CancellationToken cancellationToken);
}

public interface IExternalCatalogReferenceRepository
{
    Task<IReadOnlyDictionary<CatalogReferenceKey, Guid>> GetOrCreateIdsAsync(
        CatalogProvider provider,
        IReadOnlyCollection<CatalogReferenceKey> references,
        CancellationToken cancellationToken);

    Task UpdateMetadataAsync(
        CatalogProvider provider,
        IReadOnlyCollection<CatalogMetadataSnapshot> snapshots,
        CancellationToken cancellationToken);

    Task LinkEpisodesToSeriesAsync(
        Guid seriesId,
        IReadOnlyCollection<Guid> episodeIds,
        CancellationToken cancellationToken);
}

public sealed class CatalogProviderException(
    string code,
    string message,
    int statusCode,
    TimeSpan? retryAfter = null) : Exception(message)
{
    public string Code { get; } = code;

    public int StatusCode { get; } = statusCode;

    public TimeSpan? RetryAfter { get; } = retryAfter;
}

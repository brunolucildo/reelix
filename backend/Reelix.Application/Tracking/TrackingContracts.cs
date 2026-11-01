using Reelix.Application.Catalog;
using Reelix.Domain.Tracking;

namespace Reelix.Application.Tracking;

public sealed record TrackingHistoryEntry(
    Guid Id,
    TrackingItemType Type,
    Guid TargetId,
    Guid? SeriesId,
    DateTimeOffset WatchedAt);

public sealed record TrackingPage<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record TrackedSeries(
    Guid SeriesId,
    int WatchedEpisodeCount);

public sealed record SeriesProgress(
    Guid SeriesId,
    int WatchedEpisodeCount,
    int? AvailableEpisodeCount,
    decimal? ProgressPercentage,
    bool? IsComplete);

public sealed record SeasonProgress(
    Guid SeriesId,
    Guid SeasonId,
    int SeasonNumber,
    int WatchedEpisodeCount,
    int AvailableEpisodeCount,
    int PendingEpisodeCount,
    decimal? ProgressPercentage,
    bool? IsComplete);

public sealed record PendingEpisode(
    Guid EpisodeId,
    int? EpisodeNumber,
    string Title,
    DateOnly? ReleaseDate);

public enum TrackingHistoryOrder
{
    NewestFirst,
    OldestFirst,
}

public interface ITrackingRepository
{
    Task<bool> CatalogReferenceExistsAsync(
        Guid id,
        CatalogReferenceType type,
        CancellationToken cancellationToken);

    Task<string?> GetSeriesExternalIdAsync(
        Guid seriesId,
        CatalogProvider provider,
        CancellationToken cancellationToken);

    Task<TrackingRecord?> GetAsync(
        Guid accountId,
        Guid targetId,
        CancellationToken cancellationToken);

    Task<TrackingRecord> AddOrUpdateAsync(
        TrackingRecord record,
        CancellationToken cancellationToken);

    Task RemoveAsync(
        TrackingRecord record,
        CancellationToken cancellationToken);

    Task<TrackingPage<TrackingHistoryEntry>> GetHistoryAsync(
        Guid accountId,
        TrackingItemType? type,
        int page,
        int pageSize,
        TrackingHistoryOrder order,
        CancellationToken cancellationToken);

    Task<TrackingPage<TrackedSeries>> GetSeriesAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> GetWatchedEpisodeCountAsync(
        Guid accountId,
        Guid seriesId,
        CancellationToken cancellationToken);

    Task<HashSet<Guid>> GetWatchedEpisodeIdsAsync(
        Guid accountId,
        Guid seriesId,
        IReadOnlyCollection<Guid> episodeIds,
        CancellationToken cancellationToken);
}

public interface ITrackingCatalog
{
    Task<CatalogSeason?> GetSeasonAsync(
        Guid seriesId,
        int seasonNumber,
        CatalogProvider provider,
        CancellationToken cancellationToken);
}

public sealed class TrackingContentNotFoundException()
    : Exception("O conteúdo informado não foi encontrado no catálogo.");

public sealed class TrackingSeriesMismatchException()
    : Exception("O episódio não pertence à série informada.");

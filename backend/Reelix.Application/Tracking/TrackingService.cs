using Reelix.Application.Authentication;
using Reelix.Application.Catalog;
using Reelix.Domain.Tracking;

namespace Reelix.Application.Tracking;

public sealed class TrackingService(
    IUserAccountRepository accounts,
    ITrackingRepository tracking,
    ITrackingCatalog catalog)
{
    public async Task<bool> IsMovieWatchedAsync(
        Guid supabaseUserId,
        Guid titleId,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        await RequireCatalogReferenceAsync(titleId, CatalogReferenceType.Movie, cancellationToken);
        return (await tracking.GetAsync(accountId, titleId, cancellationToken)) is
            { ItemType: TrackingItemType.Movie };
    }

    public async Task<TrackingHistoryEntry> MarkMovieAsync(
        Guid supabaseUserId,
        Guid titleId,
        DateTimeOffset? watchedAt,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        await RequireCatalogReferenceAsync(titleId, CatalogReferenceType.Movie, cancellationToken);
        var record = TrackingRecord.CreateMovie(accountId, titleId, watchedAt);
        return ToEntry(await tracking.AddOrUpdateAsync(record, cancellationToken));
    }

    public async Task RemoveMovieAsync(
        Guid supabaseUserId,
        Guid titleId,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        var record = await tracking.GetAsync(accountId, titleId, cancellationToken);
        if (record is null || record.ItemType != TrackingItemType.Movie)
        {
            return;
        }

        await tracking.RemoveAsync(record, cancellationToken);
    }

    public async Task<TrackingHistoryEntry> MarkEpisodeAsync(
        Guid supabaseUserId,
        Guid seriesId,
        Guid episodeId,
        DateTimeOffset? watchedAt,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        await RequireCatalogReferenceAsync(seriesId, CatalogReferenceType.Series, cancellationToken);
        await RequireCatalogReferenceAsync(episodeId, CatalogReferenceType.Episode, cancellationToken);

        var existing = await tracking.GetAsync(accountId, episodeId, cancellationToken);
        if (existing is { ItemType: TrackingItemType.Episode } &&
            existing.SeriesId != seriesId)
        {
            throw new TrackingSeriesMismatchException();
        }

        var record = TrackingRecord.CreateEpisode(accountId, seriesId, episodeId, watchedAt);
        return ToEntry(await tracking.AddOrUpdateAsync(record, cancellationToken));
    }

    public async Task RemoveEpisodeAsync(
        Guid supabaseUserId,
        Guid seriesId,
        Guid episodeId,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        var record = await tracking.GetAsync(accountId, episodeId, cancellationToken);
        if (record is null ||
            record.ItemType != TrackingItemType.Episode ||
            record.SeriesId != seriesId)
        {
            return;
        }

        await tracking.RemoveAsync(record, cancellationToken);
    }

    public async Task<TrackingPage<TrackingHistoryEntry>> GetHistoryAsync(
        Guid supabaseUserId,
        TrackingItemType? type,
        int page,
        int pageSize,
        TrackingHistoryOrder order,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        return await tracking.GetHistoryAsync(
            accountId,
            type,
            page,
            pageSize,
            order,
            cancellationToken);
    }

    public async Task<TrackingPage<TrackedSeries>> GetSeriesAsync(
        Guid supabaseUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        return await tracking.GetSeriesAsync(accountId, page, pageSize, cancellationToken);
    }

    public async Task<SeriesProgress> GetSeriesProgressAsync(
        Guid supabaseUserId,
        Guid seriesId,
        CancellationToken cancellationToken,
        CatalogProvider provider = CatalogProvider.Tmdb)
    {
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        await RequireCatalogReferenceAsync(seriesId, CatalogReferenceType.Series, cancellationToken);
        var seasons = await catalog.GetSeasonsAsync(seriesId, provider, cancellationToken);
        if (seasons is null)
        {
            var trackedCount = await tracking.GetWatchedEpisodeCountAsync(
                accountId, seriesId, cancellationToken);
            return new SeriesProgress(seriesId, trackedCount, null, null, null);
        }

        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var availableIds = new HashSet<Guid>();
        foreach (var summary in seasons
            .Where(item => item.SeasonNumber >= 0 &&
                (item.ReleaseDate is null || item.ReleaseDate <= todayUtc))
            .DistinctBy(item => item.SeasonNumber))
        {
            var season = await GetSeasonAsync(
                seriesId, summary.SeasonNumber, provider, cancellationToken);
            availableIds.UnionWith(GetAvailableEpisodes(season).Select(episode => episode.Id));
        }

        var watchedIds = await tracking.GetWatchedEpisodeIdsAsync(
            accountId, seriesId, availableIds, cancellationToken);
        var availableCount = availableIds.Count;
        var watchedCount = watchedIds.Count;
        return new SeriesProgress(
            seriesId,
            watchedCount,
            availableCount,
            availableCount == 0 ? null : decimal.Round(watchedCount * 100m / availableCount, 2),
            availableCount == 0 ? null : watchedCount == availableCount,
            availableCount - watchedCount);
    }

    public async Task<SeasonProgress> GetSeasonProgressAsync(
        Guid supabaseUserId,
        Guid seriesId,
        int seasonNumber,
        CatalogProvider provider,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        var season = await GetSeasonAsync(seriesId, seasonNumber, provider, cancellationToken);
        var availableEpisodes = GetAvailableEpisodes(season);
        var episodeIds = availableEpisodes.Select(episode => episode.Id).ToArray();
        var watchedIds = await tracking.GetWatchedEpisodeIdsAsync(
            accountId,
            seriesId,
            episodeIds,
            cancellationToken);
        var watchedCount = watchedIds.Count;
        var availableCount = episodeIds.Length;

        return new SeasonProgress(
            seriesId,
            season.Id,
            season.SeasonNumber,
            watchedCount,
            availableCount,
            availableCount - watchedCount,
            availableCount == 0 ? null : decimal.Round(watchedCount * 100m / availableCount, 2),
            availableCount == 0 ? null : watchedCount == availableCount);
    }

    public async Task<IReadOnlyList<PendingEpisode>> GetPendingEpisodesAsync(
        Guid supabaseUserId,
        Guid seriesId,
        int seasonNumber,
        CatalogProvider provider,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        var season = await GetSeasonAsync(seriesId, seasonNumber, provider, cancellationToken);
        var availableEpisodes = GetAvailableEpisodes(season);
        var watchedIds = await tracking.GetWatchedEpisodeIdsAsync(
            accountId,
            seriesId,
            availableEpisodes.Select(episode => episode.Id).ToArray(),
            cancellationToken);

        return availableEpisodes
            .Where(episode => !watchedIds.Contains(episode.Id))
            .Select(episode => new PendingEpisode(
                episode.Id,
                episode.EpisodeNumber,
                episode.Title,
                episode.ReleaseDate))
            .ToArray();
    }

    private async Task<Guid> GetAccountIdAsync(
        Guid supabaseUserId,
        CancellationToken cancellationToken)
    {
        if (supabaseUserId == Guid.Empty)
        {
            throw new ArgumentException("Informe uma conta autenticada.", nameof(supabaseUserId));
        }

        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return account.Id;
    }

    private async Task RequireCatalogReferenceAsync(
        Guid id,
        CatalogReferenceType type,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty ||
            !await tracking.CatalogReferenceExistsAsync(id, type, cancellationToken))
        {
            throw new TrackingContentNotFoundException();
        }
    }

    private async Task<CatalogSeason> GetSeasonAsync(
        Guid seriesId,
        int seasonNumber,
        CatalogProvider provider,
        CancellationToken cancellationToken)
    {
        if (seasonNumber < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(seasonNumber),
                "O número da temporada não pode ser negativo.");
        }

        await RequireCatalogReferenceAsync(seriesId, CatalogReferenceType.Series, cancellationToken);
        var season = await catalog.GetSeasonAsync(
            seriesId,
            seasonNumber,
            provider,
            cancellationToken);
        if (season is null || season.SeriesId != seriesId)
        {
            throw new TrackingContentNotFoundException();
        }

        return season;
    }

    private static CatalogEpisode[] GetAvailableEpisodes(CatalogSeason season)
    {
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        return season.Episodes
            .Where(episode =>
                episode.ReleaseDate is null ||
                episode.ReleaseDate.Value <= todayUtc)
            .DistinctBy(episode => episode.Id)
            .ToArray();
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page is < 1 or > 100_000)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "A página deve estar entre 1 e 100000.");
        }

        if (pageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "O tamanho da página deve estar entre 1 e 100.");
        }
    }

    private static TrackingHistoryEntry ToEntry(TrackingRecord record) =>
        new(record.Id, record.ItemType, record.TargetId, record.SeriesId, record.WatchedAt);
}

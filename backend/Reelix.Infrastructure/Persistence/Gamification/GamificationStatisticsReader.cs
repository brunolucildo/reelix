using Microsoft.EntityFrameworkCore;
using Reelix.Application.Gamification;
using Reelix.Domain.Tracking;

namespace Reelix.Infrastructure.Persistence.Gamification;

/// <summary>
/// Reads the watching statistics from the tracking records and the catalog references. The tracking
/// and catalog tables belong to other modules; this read keeps going to them directly until the
/// owning modules expose a port (spec 0016).
/// </summary>
public sealed class GamificationStatisticsReader(ReelixDbContext dbContext, TimeProvider clock)
    : IGamificationStatisticsReader
{
    public Task<GamificationStatistics> GetLifetimeAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        ReadAsync(accountId, null, null, cancellationToken);

    public async Task<GamificationStatistics> GetPeriodAsync(
        Guid accountId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.RepeatableRead,
            cancellationToken);
        var statistics = await ReadAsync(accountId, periodStart, periodEnd, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return statistics;
    }

    private async Task<GamificationStatistics> ReadAsync(
        Guid accountId,
        DateTimeOffset? periodStart,
        DateTimeOffset? periodEnd,
        CancellationToken cancellationToken)
    {
        IQueryable<TrackingRecord> records = dbContext.TrackingRecords
            .Where(record => record.AccountId == accountId);
        if (periodStart.HasValue && periodEnd.HasValue)
        {
            records = records.Where(record =>
                record.WatchedAt >= periodStart.Value &&
                record.WatchedAt < periodEnd.Value);
        }

        var moviesWatched = await records
            .CountAsync(record => record.ItemType == TrackingItemType.Movie, cancellationToken);
        var episodesWatched = await records
            .CountAsync(record => record.ItemType == TrackingItemType.Episode, cancellationToken);
        var seriesExplored = await records
            .Where(record => record.ItemType == TrackingItemType.Episode && record.SeriesId != null)
            .Select(record => record.SeriesId)
            .Distinct()
            .CountAsync(cancellationToken);
        var genresExplored = await (
                from record in records
                join genre in dbContext.CatalogReferenceGenres
                    on record.TargetId equals genre.CatalogReferenceId
                join reference in dbContext.ExternalCatalogReferences
                    on genre.CatalogReferenceId equals reference.Id
                where record.ItemType == TrackingItemType.Movie
                select new { reference.Provider, genre.GenreId })
            .Distinct()
            .CountAsync(cancellationToken);
        var moviesMissingGenreMetadata = await records
            .Where(record =>
                record.ItemType == TrackingItemType.Movie &&
                !dbContext.CatalogReferenceGenres.Any(genre =>
                    genre.CatalogReferenceId == record.TargetId))
            .CountAsync(cancellationToken);
        var knownRuntime = from record in records
                           join reference in dbContext.ExternalCatalogReferences
                               on record.TargetId equals reference.Id
                           where (record.ItemType == TrackingItemType.Movie ||
                                  record.ItemType == TrackingItemType.Episode) &&
                                 reference.RuntimeMinutes.HasValue &&
                                 reference.RuntimeMinutes.Value > 0 &&
                                 reference.RuntimeMinutes.Value <= 1440
                           select new
                           {
                               reference.RuntimeMinutes,
                               reference.Provider,
                               reference.MetadataVersion,
                               reference.MetadataUpdatedAt,
                           };
        var runtimeSummary = await knownRuntime
            .GroupBy(_ => 1)
            .Select(group => new
            {
                ItemsWithRuntime = group.Count(),
                EstimatedMinutes = group.Sum(item => item.RuntimeMinutes),
                MetadataVersion = group.Max(item => item.MetadataVersion),
                MetadataUpdatedAt = group.Max(item => item.MetadataUpdatedAt),
            })
            .SingleOrDefaultAsync(cancellationToken);
        var sources = await knownRuntime
            .Select(item => item.Provider)
            .Distinct()
            .OrderBy(provider => provider)
            .ToArrayAsync(cancellationToken);
        var itemsWithRuntime = runtimeSummary?.ItemsWithRuntime ?? 0;
        int? estimatedMinutes = runtimeSummary is null
            ? null
            : runtimeSummary.EstimatedMinutes;
        int? metadataVersion = runtimeSummary?.MetadataVersion;
        var metadataUpdatedAt = runtimeSummary?.MetadataUpdatedAt;

        return new GamificationStatistics(
            periodStart ?? DateTimeOffset.MinValue,
            periodEnd ?? DateTimeOffset.MaxValue,
            moviesWatched,
            episodesWatched,
            seriesExplored,
            genresExplored,
            moviesMissingGenreMetadata,
            itemsWithRuntime,
            moviesWatched + episodesWatched - itemsWithRuntime,
            estimatedMinutes,
            estimatedMinutes.HasValue ? estimatedMinutes.Value / 60m : null,
            sources,
            metadataVersion,
            metadataUpdatedAt,
            null,
            clock.GetUtcNow());
    }
}

using Reelix.Application.Authentication;
using Reelix.Application.Catalog;
using Reelix.Application.Tracking;
using Reelix.Domain.Catalog;
using Reelix.Domain.Tracking;
using Reelix.Domain.Users;

namespace Reelix.UnitTests.Tracking;

public sealed class TrackingServiceTests
{
    [Fact]
    public async Task MarkMovie_IsIdempotentAndAllowsDateCorrection()
    {
        var accounts = new StubAccountRepository();
        var repository = new StubTrackingRepository();
        var titleId = Guid.NewGuid();
        repository.AddCatalogReference(titleId, CatalogReferenceType.Movie);
        var service = new TrackingService(accounts, repository, new StubTrackingCatalog());
        var userId = Guid.NewGuid();

        var first = await service.MarkMovieAsync(
            userId,
            titleId,
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        var corrected = await service.MarkMovieAsync(
            userId,
            titleId,
            new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        Assert.Equal(first.Id, corrected.Id);
        Assert.Equal(new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero), corrected.WatchedAt);
        Assert.Single(repository.Records);
    }

    [Fact]
    public async Task History_IsScopedToAuthenticatedAccountAndPaginated()
    {
        var accounts = new StubAccountRepository();
        var repository = new StubTrackingRepository();
        var titleId = Guid.NewGuid();
        repository.AddCatalogReference(titleId, CatalogReferenceType.Movie);
        var service = new TrackingService(accounts, repository, new StubTrackingCatalog());

        await service.MarkMovieAsync(
            Guid.NewGuid(),
            titleId,
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        var userId = Guid.NewGuid();
        await service.MarkMovieAsync(
            userId,
            titleId,
            new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        var history = await service.GetHistoryAsync(
            userId,
            TrackingItemType.Movie,
            page: 1,
            pageSize: 1,
            TrackingHistoryOrder.NewestFirst,
            CancellationToken.None);

        var item = Assert.Single(history.Items);
        Assert.Equal(1, history.TotalCount);
        Assert.Equal(new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero), item.WatchedAt);
    }

    [Fact]
    public async Task PendingEpisodes_UsesCatalogIdsAndDoesNotInferWatchedFromDates()
    {
        var accountRepository = new StubAccountRepository();
        var trackingRepository = new StubTrackingRepository();
        var userId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var watchedEpisodeId = Guid.NewGuid();
        var specialWithoutNumberId = Guid.NewGuid();
        var upcomingEpisodeId = Guid.NewGuid();
        trackingRepository.AddCatalogReference(seriesId, CatalogReferenceType.Series);
        trackingRepository.AddCatalogReference(watchedEpisodeId, CatalogReferenceType.Episode);
        trackingRepository.AddCatalogReference(specialWithoutNumberId, CatalogReferenceType.Episode);
        trackingRepository.AddCatalogReference(upcomingEpisodeId, CatalogReferenceType.Episode);
        var season = new CatalogSeason(
            Guid.NewGuid(),
            seriesId,
            CatalogProvider.Tmdb,
            "provider-series-id",
            "Temporada",
            1,
            null,
            [
                Episode(watchedEpisodeId, 1, new DateOnly(2020, 1, 1)),
                Episode(specialWithoutNumberId, null, null),
                Episode(upcomingEpisodeId, 2, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1)),
            ]);
        var catalog = new StubTrackingCatalog { Season = season };
        var service = new TrackingService(accountRepository, trackingRepository, catalog);

        await service.MarkEpisodeAsync(
            userId,
            seriesId,
            watchedEpisodeId,
            null,
            CancellationToken.None);

        var pending = await service.GetPendingEpisodesAsync(
            userId,
            seriesId,
            1,
            CatalogProvider.Tmdb,
            CancellationToken.None);
        var seasonProgress = await service.GetSeasonProgressAsync(
            userId,
            seriesId,
            1,
            CatalogProvider.Tmdb,
            CancellationToken.None);
        var seriesProgress = await service.GetSeriesProgressAsync(
            userId,
            seriesId,
            CancellationToken.None);

        var pendingEpisode = Assert.Single(pending);
        Assert.Equal(specialWithoutNumberId, pendingEpisode.EpisodeId);
        Assert.Null(pendingEpisode.EpisodeNumber);
        Assert.Equal(2, seasonProgress.AvailableEpisodeCount);
        Assert.Equal(1, seasonProgress.WatchedEpisodeCount);
        Assert.Equal(1, seasonProgress.PendingEpisodeCount);
        Assert.False(seasonProgress.IsComplete);
        Assert.Equal(1, seriesProgress.WatchedEpisodeCount);
        Assert.Null(seriesProgress.AvailableEpisodeCount);
        Assert.Null(seriesProgress.IsComplete);
    }

    [Fact]
    public async Task RemoveEpisode_WithDifferentSeriesDoesNotRemoveTracking()
    {
        var accountRepository = new StubAccountRepository();
        var repository = new StubTrackingRepository();
        var userId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var otherSeriesId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();
        repository.AddCatalogReference(seriesId, CatalogReferenceType.Series);
        repository.AddCatalogReference(otherSeriesId, CatalogReferenceType.Series);
        repository.AddCatalogReference(episodeId, CatalogReferenceType.Episode);
        var service = new TrackingService(accountRepository, repository, new StubTrackingCatalog());
        await service.MarkEpisodeAsync(
            userId,
            seriesId,
            episodeId,
            null,
            CancellationToken.None);

        await service.RemoveEpisodeAsync(
            userId,
            otherSeriesId,
            episodeId,
            CancellationToken.None);

        Assert.Single(repository.Records);
    }

    private static CatalogEpisode Episode(Guid id, int? number, DateOnly? releaseDate) =>
        new(id, $"external-{id}", number, "Episódio", null, releaseDate, null);

    private sealed class StubAccountRepository : IUserAccountRepository
    {
        private readonly Dictionary<Guid, UserAccount> _accounts = [];

        public Task<UserAccount> GetOrCreateAsync(Guid supabaseUserId, CancellationToken cancellationToken)
        {
            if (!_accounts.TryGetValue(supabaseUserId, out var account))
            {
                account = UserAccount.Create(supabaseUserId);
                _accounts.Add(supabaseUserId, account);
            }

            return Task.FromResult(account);
        }
    }

    private sealed class StubTrackingRepository : ITrackingRepository
    {
        private readonly Dictionary<Guid, CatalogReferenceType> _catalogReferences = [];
        private readonly List<TrackingRecord> _records = [];

        public IReadOnlyList<TrackingRecord> Records => _records;

        public void AddCatalogReference(Guid id, CatalogReferenceType type) =>
            _catalogReferences.Add(id, type);

        public Task<bool> CatalogReferenceExistsAsync(
            Guid id,
            CatalogReferenceType type,
            CancellationToken cancellationToken) =>
            Task.FromResult(_catalogReferences.TryGetValue(id, out var actualType) && actualType == type);

        public Task<string?> GetSeriesExternalIdAsync(
            Guid seriesId,
            CatalogProvider provider,
            CancellationToken cancellationToken) =>
            Task.FromResult<string?>("provider-series-id");

        public Task<TrackingRecord?> GetAsync(
            Guid accountId,
            Guid targetId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_records.SingleOrDefault(record =>
                record.AccountId == accountId && record.TargetId == targetId));

        public Task<TrackingRecord> AddOrUpdateAsync(
            TrackingRecord record,
            CancellationToken cancellationToken)
        {
            var existing = _records.SingleOrDefault(candidate =>
                candidate.AccountId == record.AccountId && candidate.TargetId == record.TargetId);
            if (existing is null)
            {
                _records.Add(record);
                return Task.FromResult(record);
            }

            existing.CorrectWatchedAt(record.WatchedAt);
            return Task.FromResult(existing);
        }

        public Task RemoveAsync(TrackingRecord record, CancellationToken cancellationToken)
        {
            _records.Remove(record);
            return Task.CompletedTask;
        }

        public Task<TrackingPage<TrackingHistoryEntry>> GetHistoryAsync(
            Guid accountId,
            TrackingItemType? type,
            int page,
            int pageSize,
            TrackingHistoryOrder order,
            CancellationToken cancellationToken)
        {
            var query = _records
                .Where(record => record.AccountId == accountId && (type is null || record.ItemType == type))
                .OrderBy(record => record.WatchedAt);
            if (order == TrackingHistoryOrder.NewestFirst)
            {
                query = query.OrderByDescending(record => record.WatchedAt);
            }

            var entries = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(record => new TrackingHistoryEntry(
                    record.Id,
                    record.ItemType,
                    record.TargetId,
                    record.SeriesId,
                    record.WatchedAt))
                .ToArray();
            var total = _records.Count(record =>
                record.AccountId == accountId && (type is null || record.ItemType == type));
            return Task.FromResult(new TrackingPage<TrackingHistoryEntry>(entries, page, pageSize, total));
        }

        public Task<TrackingPage<TrackedSeries>> GetSeriesAsync(
            Guid accountId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var items = _records
                .Where(record => record.AccountId == accountId && record.SeriesId is not null)
                .GroupBy(record => record.SeriesId!.Value)
                .Select(group => new TrackedSeries(group.Key, group.Count()))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();
            var total = _records
                .Where(record => record.AccountId == accountId && record.SeriesId is not null)
                .Select(record => record.SeriesId)
                .Distinct()
                .Count();
            return Task.FromResult(new TrackingPage<TrackedSeries>(items, page, pageSize, total));
        }

        public Task<int> GetWatchedEpisodeCountAsync(
            Guid accountId,
            Guid seriesId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_records.Count(record =>
                record.AccountId == accountId &&
                record.ItemType == TrackingItemType.Episode &&
                record.SeriesId == seriesId));

        public Task<HashSet<Guid>> GetWatchedEpisodeIdsAsync(
            Guid accountId,
            Guid seriesId,
            IReadOnlyCollection<Guid> episodeIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(_records
                .Where(record =>
                    record.AccountId == accountId &&
                    record.ItemType == TrackingItemType.Episode &&
                    record.SeriesId == seriesId &&
                    episodeIds.Contains(record.TargetId))
                .Select(record => record.TargetId)
                .ToHashSet());
    }

    private sealed class StubTrackingCatalog : ITrackingCatalog
    {
        public CatalogSeason? Season { get; init; }

        public Task<CatalogSeason?> GetSeasonAsync(
            Guid seriesId,
            int seasonNumber,
            CatalogProvider provider,
            CancellationToken cancellationToken) =>
            Task.FromResult(Season);
    }
}

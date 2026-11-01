using Microsoft.EntityFrameworkCore;
using Reelix.Application.Gamification;
using Reelix.Domain.Gamification;
using Reelix.Domain.Social;
using Reelix.Domain.Tracking;
using Reelix.Domain.Users;

namespace Reelix.Infrastructure.Persistence.Gamification;

public sealed class GamificationRepository(ReelixDbContext dbContext) : IGamificationRepository
{
    public async Task<GamificationDashboard> GetDashboardAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.RepeatableRead,
            cancellationToken);
        var account = await dbContext.UserAccounts
            .FromSqlInterpolated(
                $"SELECT * FROM public.user_accounts WHERE id = {accountId} FOR UPDATE")
            .ToArrayAsync(cancellationToken);
        if (account.Length == 0)
        {
            throw new KeyNotFoundException("Conta não encontrada.");
        }

        var statistics = await GetStatisticsCoreAsync(
            accountId,
            null,
            null,
            cancellationToken);
        var definitions = await dbContext.AchievementDefinitions
            .Where(definition => definition.IsActive)
            .OrderBy(definition => definition.Code)
            .ToArrayAsync(cancellationToken);
        var existing = await dbContext.UserAchievements
            .Where(achievement => achievement.AccountId == accountId)
            .ToDictionaryAsync(achievement => achievement.DefinitionId, cancellationToken);

        foreach (var definition in definitions)
        {
            var value = GetRuleValue(definition.RuleType, statistics);
            if (value >= definition.Threshold)
            {
                if (existing.TryGetValue(definition.Id, out var achievement))
                {
                    achievement.Earn();
                }
                else
                {
                    achievement = UserAchievement.Create(accountId, definition.Id);
                    dbContext.UserAchievements.Add(achievement);
                    existing.Add(definition.Id, achievement);
                }
            }
            else if (existing.TryGetValue(definition.Id, out var achievement))
            {
                achievement.Revoke();
            }
        }

        var preferences = await GetOrCreatePreferencesAsync(accountId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var progress = definitions
            .Select(definition =>
            {
                existing.TryGetValue(definition.Id, out var achievement);
                return new AchievementProgress(
                    definition.Code,
                    definition.Title,
                    definition.Description,
                    GetRuleValue(definition.RuleType, statistics),
                    definition.Threshold,
                    achievement?.Status == UserAchievementStatus.Earned,
                    definition.RuleType == AchievementRuleType.MovieGenreCount &&
                        statistics.MoviesMissingGenreMetadata > 0,
                    achievement?.Status,
                    achievement?.FirstEarnedAt,
                    achievement?.LastRevokedAt,
                    achievement?.ReactivatedAt);
            })
            .ToArray();

        return new GamificationDashboard(
            ToPreferencesData(preferences),
            statistics,
            progress);
    }

    public async Task<GamificationPreferencesData> UpdatePreferencesAsync(
        Guid accountId,
        bool badgesPublic,
        bool rankingEnabled,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var account = await dbContext.UserAccounts
            .FromSqlInterpolated(
                $"SELECT * FROM public.user_accounts WHERE id = {accountId} FOR UPDATE")
            .ToArrayAsync(cancellationToken);
        if (account.Length == 0)
        {
            throw new KeyNotFoundException("Conta não encontrada.");
        }

        var preferences = await dbContext.GamificationPreferences
            .SingleOrDefaultAsync(item => item.AccountId == accountId, cancellationToken);
        if (preferences is null)
        {
            preferences = GamificationPreferences.Create(accountId);
            dbContext.GamificationPreferences.Add(preferences);
        }

        preferences.Update(badgesPublic, rankingEnabled);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToPreferencesData(preferences);
    }

    public async Task<GamificationStatistics> GetStatisticsAsync(
        Guid accountId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.RepeatableRead,
            cancellationToken);
        var statistics = await GetStatisticsCoreAsync(
            accountId,
            periodStart,
            periodEnd,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return statistics;
    }

    public async Task<PublicBadgesData> GetPublicBadgesAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        await GetDashboardAsync(accountId, cancellationToken);
        var preferences = await GetOrCreatePreferencesAsync(accountId, cancellationToken);
        if (!preferences.BadgesPublic)
        {
            return new PublicBadgesData(false, []);
        }

        var badges = await (
                from achievement in dbContext.UserAchievements
                join definition in dbContext.AchievementDefinitions
                    on achievement.DefinitionId equals definition.Id
                where achievement.AccountId == accountId &&
                      achievement.Status == UserAchievementStatus.Earned &&
                      definition.IsActive
                orderby achievement.FirstEarnedAt, definition.Code
                select new PublicBadge(
                    definition.Code,
                    definition.Title,
                    definition.Description,
                    achievement.FirstEarnedAt))
            .ToArrayAsync(cancellationToken);
        return new PublicBadgesData(true, badges);
    }

    public async Task<GamificationRankingPage> GetMonthlyRankingAsync(
        Guid accountId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        GamificationRankingCursor? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.RepeatableRead,
            cancellationToken);
        var viewerPreferences = await dbContext.GamificationPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.AccountId == accountId, cancellationToken);
        if (viewerPreferences?.RankingEnabled != true)
        {
            await transaction.CommitAsync(cancellationToken);
            return new GamificationRankingPage(
                false,
                periodStart,
                periodEnd,
                0,
                [],
                null);
        }

        var acceptedFriendIds = await dbContext.FriendRelationships
            .Where(relationship =>
                relationship.Status == FriendRelationshipStatus.Accepted &&
                (relationship.FirstAccountId == accountId ||
                 relationship.SecondAccountId == accountId))
            .Select(relationship =>
                relationship.FirstAccountId == accountId
                    ? relationship.SecondAccountId
                    : relationship.FirstAccountId)
            .ToArrayAsync(cancellationToken);
        var participantIds = acceptedFriendIds.Append(accountId).Distinct().ToArray();
        var optedInIds = await dbContext.GamificationPreferences
            .Where(preferences =>
                participantIds.Contains(preferences.AccountId) &&
                preferences.RankingEnabled)
            .Select(preferences => preferences.AccountId)
            .ToArrayAsync(cancellationToken);
        var counts = dbContext.TrackingRecords
            .Where(record =>
                optedInIds.Contains(record.AccountId) &&
                record.ItemType == TrackingItemType.Movie &&
                record.WatchedAt >= periodStart &&
                record.WatchedAt < periodEnd)
            .GroupBy(record => record.AccountId)
            .Select(group => new
            {
                AccountId = group.Key,
                MoviesWatched = group.Count(),
            });
        var ranking = from count in counts
                      join profile in dbContext.UserProfiles
                          on count.AccountId equals profile.AccountId into profiles
                      from profile in profiles.DefaultIfEmpty()
                      select new RankingCandidate(
                          count.AccountId,
                          profile == null || profile.DisplayName == null
                              ? "Usuário Reelix"
                              : profile.DisplayName,
                          count.MoviesWatched);
        var scoreDistribution = await counts
            .GroupBy(item => item.MoviesWatched)
            .Select(group => new
            {
                MoviesWatched = group.Key,
                Participants = group.Count(),
            })
            .ToArrayAsync(cancellationToken);
        var totalParticipants = scoreDistribution.Sum(item => item.Participants);
        if (cursor is not null)
        {
            ranking = ranking.Where(item =>
                item.MoviesWatched < cursor.MoviesWatched ||
                (item.MoviesWatched == cursor.MoviesWatched &&
                 item.AccountId.CompareTo(cursor.AccountId) > 0));
        }

        var page = await ranking
            .OrderByDescending(item => item.MoviesWatched)
            .ThenBy(item => item.AccountId)
            .Take(limit + 1)
            .ToArrayAsync(cancellationToken);
        var hasMore = page.Length > limit;
        var items = new List<GamificationRankingEntry>(Math.Min(limit, page.Length));
        foreach (var candidate in page.Take(limit))
        {
            var ahead = scoreDistribution
                .Where(item => item.MoviesWatched > candidate.MoviesWatched)
                .Sum(item => item.Participants);
            items.Add(new GamificationRankingEntry(
                ahead + 1,
                candidate.AccountId,
                candidate.DisplayName,
                candidate.MoviesWatched));
        }

        var nextCursor = hasMore && items.Count > 0
            ? EncodeCursor(items[^1])
            : null;
        await transaction.CommitAsync(cancellationToken);
        return new GamificationRankingPage(
            true,
            periodStart,
            periodEnd,
            totalParticipants,
            items,
            nextCursor);
    }

    private async Task<GamificationStatistics> GetStatisticsCoreAsync(
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
        var periodStartValue = periodStart ?? DateTimeOffset.MinValue;
        var periodEndValue = periodEnd ?? DateTimeOffset.MaxValue;

        return new GamificationStatistics(
            periodStartValue,
            periodEndValue,
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
            DateTimeOffset.UtcNow);
    }

    private async Task<GamificationPreferences> GetOrCreatePreferencesAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO public.gamification_preferences
                (account_id, badges_public, ranking_enabled, updated_at)
            VALUES ({accountId}, TRUE, FALSE, {DateTimeOffset.UtcNow})
            ON CONFLICT (account_id) DO NOTHING
            """,
            cancellationToken);
        return await dbContext.GamificationPreferences
            .SingleAsync(item => item.AccountId == accountId, cancellationToken);
    }

    private static int GetRuleValue(
        AchievementRuleType ruleType,
        GamificationStatistics statistics) =>
        ruleType switch
        {
            AchievementRuleType.MovieCount => statistics.MoviesWatched,
            AchievementRuleType.EpisodeCount => statistics.EpisodesWatched,
            AchievementRuleType.SeriesCount => statistics.SeriesExplored,
            AchievementRuleType.MovieGenreCount => statistics.GenresExplored,
            _ => throw new InvalidOperationException("Tipo de regra de conquista não suportado."),
        };

    private static GamificationPreferencesData ToPreferencesData(
        GamificationPreferences preferences) =>
        new(preferences.BadgesPublic, preferences.RankingEnabled, preferences.UpdatedAt);

    private static string EncodeCursor(GamificationRankingEntry entry) =>
        Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{entry.MoviesWatched}:{entry.AccountId}"))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private sealed record RankingCandidate(Guid AccountId, string DisplayName, int MoviesWatched);
}

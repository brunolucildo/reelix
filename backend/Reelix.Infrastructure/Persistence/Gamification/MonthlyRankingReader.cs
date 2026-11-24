using Microsoft.EntityFrameworkCore;
using Reelix.Application.Gamification;
using Reelix.Domain.Social;
using Reelix.Domain.Tracking;

namespace Reelix.Infrastructure.Persistence.Gamification;

/// <summary>
/// Reads the raw material of the monthly ranking in one consistent snapshot: who takes part (the
/// viewer and the accepted friends who opted in), how many movies each watched in the month and how
/// the scores are distributed. Positions and the next-page cursor are decided by the use case. The
/// friend, tracking and profile tables belong to other modules; this read keeps going to them
/// directly until the owning modules expose a port (spec 0016).
/// </summary>
public sealed class MonthlyRankingReader(ReelixDbContext dbContext) : IMonthlyRankingReader
{
    public async Task<MonthlyRankingData> GetAsync(
        Guid viewerAccountId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        GamificationRankingCursor? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        var accountId = viewerAccountId;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.RepeatableRead,
            cancellationToken);
        var viewerPreferences = await dbContext.GamificationPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.AccountId == accountId, cancellationToken);
        if (viewerPreferences?.RankingEnabled != true)
        {
            await transaction.CommitAsync(cancellationToken);
            return MonthlyRankingData.Disabled;
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
            .Select(group => new RankingScoreBucket(group.Key, group.Count()))
            .ToArrayAsync(cancellationToken);
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
        await transaction.CommitAsync(cancellationToken);
        return new MonthlyRankingData(true, page, scoreDistribution);
    }
}

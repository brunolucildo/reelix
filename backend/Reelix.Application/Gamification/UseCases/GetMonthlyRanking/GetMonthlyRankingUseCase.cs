using System.Globalization;
using Reelix.Application.Authentication;
using Reelix.Application.Common;
using Reelix.Application.Errors;

namespace Reelix.Application.Gamification;

/// <summary>
/// <c>GET /gamification/friends/ranking</c>: the monthly movie ranking among the viewer and the
/// friends who opted in. It never writes: a viewer without an account, or who did not opt in, gets
/// the disabled page.
/// </summary>
public sealed class GetMonthlyRankingUseCase(
    IUserAccountRepository accounts,
    IMonthlyRankingReader ranking)
{
    public async Task<GetMonthlyRankingResult> ExecuteAsync(
        GetMonthlyRankingQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Year is < 2000 or > 9998 || query.Month is < 1 or > 12)
        {
            throw new ValidationError("Informe um ano e mês válidos.");
        }

        if (query.Limit is < 1 or > 50)
        {
            throw new ValidationError("O limite deve estar entre 1 e 50.");
        }

        var start = new DateTimeOffset(query.Year, query.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMonths(1);
        var cursor = ParseCursor(query.Cursor);

        var account = await accounts.FindAsync(query.SubjectId, cancellationToken);
        var data = account is null
            ? MonthlyRankingData.Disabled
            : await ranking.GetAsync(account.Id, start, end, cursor, query.Limit, cancellationToken);
        if (!data.IsEnabled)
        {
            return new GetMonthlyRankingResult(false, start, end, 0, [], null);
        }

        var hasMore = data.Candidates.Count > query.Limit;
        var items = data.Candidates
            .Take(query.Limit)
            .Select(candidate => new GamificationRankingEntry(
                PositionOf(candidate, data.ScoreDistribution),
                candidate.AccountId,
                candidate.DisplayName,
                candidate.MoviesWatched))
            .ToArray();
        return new GetMonthlyRankingResult(
            true,
            start,
            end,
            data.ScoreDistribution.Sum(bucket => bucket.Participants),
            items,
            hasMore && items.Length > 0 ? EncodeCursor(items[^1]) : null);
    }

    // Competition ranking: everybody with the same score shares the position after the people ahead.
    private static int PositionOf(RankingCandidate candidate, IReadOnlyList<RankingScoreBucket> distribution) =>
        distribution
            .Where(bucket => bucket.MoviesWatched > candidate.MoviesWatched)
            .Sum(bucket => bucket.Participants) + 1;

    private static string EncodeCursor(GamificationRankingEntry entry) =>
        CursorCodec.EncodeText($"{entry.MoviesWatched}:{entry.AccountId}");

    private static GamificationRankingCursor? ParseCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        if (CursorCodec.TryDecodeText(cursor, out var decoded))
        {
            var parts = decoded.Split(':', 2);
            if (parts.Length == 2 &&
                int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var count) &&
                count >= 0 &&
                Guid.TryParse(parts[1], out var accountId) &&
                accountId != Guid.Empty)
            {
                return new GamificationRankingCursor(count, accountId);
            }
        }

        throw new ValidationError("O cursor de paginação é inválido.");
    }
}

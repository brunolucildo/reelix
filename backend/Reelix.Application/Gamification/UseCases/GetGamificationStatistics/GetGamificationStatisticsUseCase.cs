using Reelix.Application.Authentication;
using Reelix.Application.Errors;

namespace Reelix.Application.Gamification;

/// <summary>
/// <c>GET /gamification/statistics</c>: the watching statistics of a period. It never writes: an
/// account that does not exist yet gets the statistics of an account without activity.
/// </summary>
public sealed class GetGamificationStatisticsUseCase(
    IUserAccountRepository accounts,
    IGamificationStatisticsReader statistics,
    TimeProvider clock)
{
    private static readonly TimeSpan MaxPeriod = TimeSpan.FromDays(366);

    public async Task<GetGamificationStatisticsResult> ExecuteAsync(
        GetGamificationStatisticsQuery query,
        CancellationToken cancellationToken)
    {
        var start = query.Start.ToUniversalTime();
        var end = query.End.ToUniversalTime();
        if (start >= end || end - start > MaxPeriod)
        {
            throw new ValidationError("O período deve ser válido e não pode exceder 366 dias.");
        }

        var account = await accounts.FindAsync(query.SubjectId, cancellationToken);
        var result = account is null
            ? GamificationStatistics.ForAccountWithoutActivity(start, end, clock.GetUtcNow())
            : await statistics.GetPeriodAsync(account.Id, start, end, cancellationToken);
        return new GetGamificationStatisticsResult(result);
    }
}

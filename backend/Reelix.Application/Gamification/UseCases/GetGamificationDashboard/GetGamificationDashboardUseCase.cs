using Reelix.Application.Authentication;
using Reelix.Application.Errors;

namespace Reelix.Application.Gamification;

/// <summary>
/// <c>GET /gamification/dashboard</c>: provisions the account if it is new, reconciles its
/// achievements (the explicit write step, see <see cref="AchievementReconciler"/>) and returns the
/// preferences, the lifetime statistics and the progress of every achievement.
/// </summary>
public sealed class GetGamificationDashboardUseCase(
    IUserAccountRepository accounts,
    AchievementReconciler reconciler)
{
    public async Task<GetGamificationDashboardResult> ExecuteAsync(
        GetGamificationDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var account = await accounts.EnsureAsync(query.SubjectId, cancellationToken);
        var reconciliation = await reconciler.ReconcileAsync(account.Id, cancellationToken)
            ?? throw new NotFoundError("Conta não encontrada.");

        return new GetGamificationDashboardResult(
            reconciliation.Preferences,
            reconciliation.LifetimeStatistics,
            reconciliation.Achievements);
    }
}

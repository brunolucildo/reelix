namespace Reelix.Application.Gamification;

/// <summary>
/// <c>GET /gamification/users/{accountId}/badges</c>: the earned badges of another account, or
/// none when that account keeps them private.
/// </summary>
/// <remarks>
/// Preserved on purpose (spec 0014, Q1 option A): reading the badges of an account still
/// reconciles that account's achievements first, so this read writes to the target account. The
/// write is the explicit <see cref="AchievementReconciler"/> step and nothing else.
/// </remarks>
public sealed class GetPublicBadgesUseCase(
    AchievementReconciler reconciler,
    IPublicBadgesReader badges)
{
    /// <returns>The badges, or null when the account does not exist.</returns>
    public async Task<GetPublicBadgesResult?> ExecuteAsync(
        GetPublicBadgesQuery query,
        CancellationToken cancellationToken)
    {
        var reconciliation = await reconciler.ReconcileAsync(query.AccountId, cancellationToken);
        if (reconciliation is null)
        {
            return null;
        }

        if (!reconciliation.Preferences.BadgesPublic)
        {
            return new GetPublicBadgesResult(false, []);
        }

        return new GetPublicBadgesResult(
            true,
            await badges.ListEarnedAsync(query.AccountId, cancellationToken));
    }
}

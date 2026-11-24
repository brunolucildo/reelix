using Reelix.Domain.Gamification;

namespace Reelix.Application.Gamification;

/// <summary>What a reconciliation leaves behind: the preferences, the statistics and the progress per achievement.</summary>
public sealed record AchievementReconciliation(
    GamificationPreferencesData Preferences,
    GamificationStatistics LifetimeStatistics,
    IReadOnlyList<AchievementProgress> Achievements);

/// <summary>
/// The one explicit step that brings the achievements of an account in line with its activity:
/// it reads the statistics, lets the pure <see cref="AchievementEvaluator"/> decide, and stores
/// the result inside the reconciliation transaction. It is the only writer of
/// <c>user_achievements</c>.
/// </summary>
/// <remarks>
/// Spec 0014, Q1 (option A): <c>GET /gamification/dashboard</c> and
/// <c>GET /gamification/users/{id}/badges</c> still trigger it, so a person earns or loses an
/// achievement at the first read after the activity changed. Moving the trigger elsewhere
/// (tracking flows, a worker) changes when <c>firstEarnedAt</c> is stamped and is a spec of its own.
/// </remarks>
public sealed class AchievementReconciler(
    IAchievementReconciliationTransaction transaction,
    IGamificationStatisticsReader statistics,
    IAchievementCatalog catalog,
    IUserAchievements achievements,
    IGamificationPreferencesStore preferences,
    TimeProvider clock)
{
    /// <returns>The reconciliation, or null when the account does not exist.</returns>
    public async Task<AchievementReconciliation?> ReconcileAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        await using var scope = await transaction.BeginAsync(accountId, cancellationToken);
        if (scope is null)
        {
            return null;
        }

        var lifetime = await statistics.GetLifetimeAsync(accountId, cancellationToken);
        var definitions = await catalog.ListActiveAsync(cancellationToken);
        var existing = (await achievements.ListAsync(accountId, cancellationToken))
            .ToDictionary(achievement => achievement.DefinitionId);

        var evaluations = AchievementEvaluator.Evaluate(
            accountId,
            definitions,
            existing,
            new AchievementMetrics(
                lifetime.MoviesWatched,
                lifetime.EpisodesWatched,
                lifetime.SeriesExplored,
                lifetime.GenresExplored,
                lifetime.MoviesMissingGenreMetadata),
            clock.GetUtcNow());

        var currentPreferences = await preferences.EnsureAsync(accountId, cancellationToken);
        await achievements.SaveAsync(
            evaluations
                .Where(evaluation => evaluation.Change == AchievementChange.Granted)
                .Select(evaluation => evaluation.Achievement!)
                .ToArray(),
            cancellationToken);
        await scope.CommitAsync(cancellationToken);

        return new AchievementReconciliation(
            currentPreferences,
            lifetime,
            evaluations.Select(ToProgress).ToArray());
    }

    private static AchievementProgress ToProgress(AchievementEvaluation evaluation) =>
        new(
            evaluation.Definition.Code,
            evaluation.Definition.Title,
            evaluation.Definition.Description,
            evaluation.CurrentValue,
            evaluation.Definition.Threshold,
            evaluation.Achievement?.Status == UserAchievementStatus.Earned,
            evaluation.IsMetadataIncomplete,
            evaluation.Achievement is { } achievement ? ToStatus(achievement.Status) : null,
            evaluation.Achievement?.FirstEarnedAt,
            evaluation.Achievement?.LastRevokedAt,
            evaluation.Achievement?.ReactivatedAt);

    private static AchievementStatus ToStatus(UserAchievementStatus status) =>
        status switch
        {
            UserAchievementStatus.Earned => AchievementStatus.Earned,
            UserAchievementStatus.Revoked => AchievementStatus.Revoked,
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };
}

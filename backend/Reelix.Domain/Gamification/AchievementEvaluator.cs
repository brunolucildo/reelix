namespace Reelix.Domain.Gamification;

/// <summary>The numbers the achievement rules read from a person's activity.</summary>
public readonly record struct AchievementMetrics(
    int MoviesWatched,
    int EpisodesWatched,
    int SeriesExplored,
    int GenresExplored,
    int MoviesMissingGenreMetadata)
{
    /// <summary>The value the given rule compares with the threshold of a definition.</summary>
    public int ValueFor(AchievementRuleType ruleType) =>
        ruleType switch
        {
            AchievementRuleType.MovieCount => MoviesWatched,
            AchievementRuleType.EpisodeCount => EpisodesWatched,
            AchievementRuleType.SeriesCount => SeriesExplored,
            AchievementRuleType.MovieGenreCount => GenresExplored,
            _ => throw new InvalidOperationException("Tipo de regra de conquista não suportado."),
        };
}

/// <summary>What evaluating a definition did to the achievement of the person.</summary>
public enum AchievementChange
{
    None,
    Granted,
    Reactivated,
    Revoked,
}

/// <summary>
/// The outcome of one definition: the value read, whether the metadata behind it is incomplete,
/// the achievement of the person after the evaluation (null when there is none) and what changed.
/// </summary>
public sealed record AchievementEvaluation(
    AchievementDefinition Definition,
    int CurrentValue,
    bool IsMetadataIncomplete,
    UserAchievement? Achievement,
    AchievementChange Change);

/// <summary>
/// The pure rule of the achievements: reaching the threshold earns (or reactivates) an
/// achievement, falling below it revokes an earned one. It touches no storage; the caller
/// persists the achievements that were created or changed.
/// </summary>
public static class AchievementEvaluator
{
    /// <param name="accountId">The account the achievements belong to.</param>
    /// <param name="definitions">The active definitions, in the order the result is wanted.</param>
    /// <param name="existing">The achievements the account already has, by definition id.</param>
    /// <param name="metrics">The activity numbers of the account.</param>
    /// <param name="now">The instant stamped on every transition.</param>
    public static IReadOnlyList<AchievementEvaluation> Evaluate(
        Guid accountId,
        IEnumerable<AchievementDefinition> definitions,
        IReadOnlyDictionary<Guid, UserAchievement> existing,
        AchievementMetrics metrics,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(existing);

        var evaluations = new List<AchievementEvaluation>();
        foreach (var definition in definitions)
        {
            var value = metrics.ValueFor(definition.RuleType);
            existing.TryGetValue(definition.Id, out var achievement);
            var change = AchievementChange.None;
            if (value >= definition.Threshold)
            {
                if (achievement is null)
                {
                    achievement = UserAchievement.Create(accountId, definition.Id, now);
                    change = AchievementChange.Granted;
                }
                else if (achievement.Status != UserAchievementStatus.Earned)
                {
                    achievement.Earn(now);
                    change = AchievementChange.Reactivated;
                }
            }
            else if (achievement is { Status: UserAchievementStatus.Earned })
            {
                achievement.Revoke(now);
                change = AchievementChange.Revoked;
            }

            evaluations.Add(new AchievementEvaluation(
                definition,
                value,
                definition.RuleType == AchievementRuleType.MovieGenreCount &&
                    metrics.MoviesMissingGenreMetadata > 0,
                achievement,
                change));
        }

        return evaluations;
    }
}

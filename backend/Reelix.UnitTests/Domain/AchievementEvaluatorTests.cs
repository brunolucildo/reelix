using Reelix.Domain.Gamification;
using Reelix.UnitTests.Common;

namespace Reelix.UnitTests.Domain;

/// <summary>The achievement rules, with no database (spec 0014, R5).</summary>
public sealed class AchievementEvaluatorTests
{
    private static readonly Guid AccountId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Now = FixedTimeProvider.DefaultNow;

    private static readonly AchievementDefinition FirstMovie = AchievementDefinition.InitialCatalog
        .Single(definition => definition.Code == "first-movie");

    private static readonly AchievementDefinition Cinephile = AchievementDefinition.InitialCatalog
        .Single(definition => definition.Code == "cinephile");

    private static readonly AchievementDefinition Marathon = AchievementDefinition.InitialCatalog
        .Single(definition => definition.Code == "first-marathon");

    private static readonly AchievementDefinition Series = AchievementDefinition.InitialCatalog
        .Single(definition => definition.Code == "series-in-focus");

    private static readonly AchievementDefinition Genres = AchievementDefinition.InitialCatalog
        .Single(definition => definition.Code == "genre-explorer");

    private static AchievementMetrics Metrics(
        int movies = 0,
        int episodes = 0,
        int series = 0,
        int genres = 0,
        int missingGenres = 0) => new(movies, episodes, series, genres, missingGenres);

    private static IReadOnlyList<AchievementEvaluation> Evaluate(
        AchievementDefinition definition,
        AchievementMetrics metrics,
        UserAchievement? existing = null) =>
        AchievementEvaluator.Evaluate(
            AccountId,
            [definition],
            existing is null
                ? new Dictionary<Guid, UserAchievement>()
                : new Dictionary<Guid, UserAchievement> { [definition.Id] = existing },
            metrics,
            Now);

    public static TheoryData<string, int, bool> Thresholds => new()
    {
        { "first-movie", 0, false },
        { "first-movie", 1, true },
        { "first-movie", 2, true },
        { "cinephile", 9, false },
        { "cinephile", 10, true },
        { "cinephile", 11, true },
        { "first-marathon", 9, false },
        { "first-marathon", 10, true },
        { "first-marathon", 11, true },
        { "series-in-focus", 2, false },
        { "series-in-focus", 3, true },
        { "series-in-focus", 4, true },
        { "genre-explorer", 4, false },
        { "genre-explorer", 5, true },
        { "genre-explorer", 6, true },
    };

    [Theory]
    [MemberData(nameof(Thresholds))]
    public void AnAccountWithoutTheAchievement_GainsItOnlyAtTheThreshold(string code, int value, bool granted)
    {
        var definition = AchievementDefinition.InitialCatalog.Single(item => item.Code == code);
        var metrics = definition.RuleType switch
        {
            AchievementRuleType.MovieCount => Metrics(movies: value),
            AchievementRuleType.EpisodeCount => Metrics(episodes: value),
            AchievementRuleType.SeriesCount => Metrics(series: value),
            _ => Metrics(genres: value),
        };

        var evaluation = Assert.Single(Evaluate(definition, metrics));

        Assert.Equal(value, evaluation.CurrentValue);
        Assert.Equal(granted ? AchievementChange.Granted : AchievementChange.None, evaluation.Change);
        Assert.Equal(granted, evaluation.Achievement is not null);
        if (granted)
        {
            Assert.Equal(UserAchievementStatus.Earned, evaluation.Achievement!.Status);
            Assert.Equal(AccountId, evaluation.Achievement.AccountId);
            Assert.Equal(definition.Id, evaluation.Achievement.DefinitionId);
            Assert.Equal(Now, evaluation.Achievement.FirstEarnedAt);
        }
    }

    [Fact]
    public void AnEarnedAchievementBelowTheThreshold_IsRevokedAndKeepsItsFirstEarnedDate()
    {
        var firstEarned = Now.AddDays(-5);
        var existing = UserAchievement.Create(AccountId, Cinephile.Id, firstEarned);

        var evaluation = Assert.Single(Evaluate(Cinephile, Metrics(movies: 9), existing));

        Assert.Equal(AchievementChange.Revoked, evaluation.Change);
        Assert.Same(existing, evaluation.Achievement);
        Assert.Equal(UserAchievementStatus.Revoked, existing.Status);
        Assert.Equal(Now, existing.LastRevokedAt);
        Assert.Equal(firstEarned, existing.FirstEarnedAt);
    }

    [Fact]
    public void ARevokedAchievementAtTheThreshold_IsReactivatedKeepingTheFirstEarnedDate()
    {
        var firstEarned = Now.AddDays(-5);
        var existing = UserAchievement.Create(AccountId, Cinephile.Id, firstEarned);
        existing.Revoke(Now.AddDays(-2));

        var evaluation = Assert.Single(Evaluate(Cinephile, Metrics(movies: 10), existing));

        Assert.Equal(AchievementChange.Reactivated, evaluation.Change);
        Assert.Equal(UserAchievementStatus.Earned, existing.Status);
        Assert.Equal(firstEarned, existing.FirstEarnedAt);
        Assert.Equal(Now.AddDays(-2), existing.LastRevokedAt);
        Assert.Equal(Now, existing.ReactivatedAt);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(25)]
    public void AnAlreadyEarnedAchievementAtOrAboveTheThreshold_IsLeftAlone(int movies)
    {
        var existing = UserAchievement.Create(AccountId, Cinephile.Id, Now.AddDays(-1));

        var evaluation = Assert.Single(Evaluate(Cinephile, Metrics(movies: movies), existing));

        Assert.Equal(AchievementChange.None, evaluation.Change);
        Assert.Equal(Now.AddDays(-1), existing.UpdatedAt);
    }

    [Fact]
    public void ARevokedAchievementBelowTheThreshold_IsLeftAlone()
    {
        var existing = UserAchievement.Create(AccountId, Cinephile.Id, Now.AddDays(-3));
        existing.Revoke(Now.AddDays(-2));

        var evaluation = Assert.Single(Evaluate(Cinephile, Metrics(movies: 1), existing));

        Assert.Equal(AchievementChange.None, evaluation.Change);
        Assert.Equal(Now.AddDays(-2), existing.LastRevokedAt);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void OnlyTheGenreAchievement_ReportsIncompleteMetadata(int missingGenres, bool expected)
    {
        var metrics = Metrics(movies: 20, episodes: 20, series: 5, genres: 2, missingGenres: missingGenres);

        var evaluations = AchievementEvaluator.Evaluate(
            AccountId,
            [FirstMovie, Cinephile, Marathon, Series, Genres],
            new Dictionary<Guid, UserAchievement>(),
            metrics,
            Now);

        Assert.Equal(
            expected,
            Assert.Single(evaluations, item => item.Definition.Code == "genre-explorer").IsMetadataIncomplete);
        Assert.All(
            evaluations.Where(item => item.Definition.Code != "genre-explorer"),
            item => Assert.False(item.IsMetadataIncomplete));
    }

    [Fact]
    public void TheResultFollowsTheOrderOfTheDefinitions()
    {
        var evaluations = AchievementEvaluator.Evaluate(
            AccountId,
            [Genres, FirstMovie, Cinephile],
            new Dictionary<Guid, UserAchievement>(),
            Metrics(),
            Now);

        Assert.Equal(
            ["genre-explorer", "first-movie", "cinephile"],
            evaluations.Select(item => item.Definition.Code));
    }

    [Fact]
    public void AnUnsupportedRuleType_IsRejected()
    {
        var unsupported = (AchievementRuleType)99;

        Assert.Throws<InvalidOperationException>(() => Metrics().ValueFor(unsupported));
    }
}

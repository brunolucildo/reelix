using Reelix.Application.Authentication;
using Reelix.Application.Errors;
using Reelix.Application.Gamification;
using Reelix.Domain.Gamification;
using Reelix.Domain.Users;
using Reelix.UnitTests.Common;

namespace Reelix.UnitTests.Gamification;

/// <summary>The gamification use cases with in-memory ports: no database, no HTTP (spec 0014, R5 to R7 and R10).</summary>
public sealed class GamificationUseCaseTests
{
    private static readonly DateTimeOffset Now = FixedTimeProvider.DefaultNow;
    private static readonly Guid SubjectId = Guid.Parse("11111111-0000-0000-0000-000000000001");

    private sealed class Accounts : IUserAccountRepository
    {
        public UserAccount? Existing { get; set; }

        public List<string> Calls { get; } = [];

        public Task<UserAccount?> FindAsync(Guid supabaseUserId, CancellationToken cancellationToken)
        {
            Calls.Add("find");
            return Task.FromResult(Existing);
        }

        public Task<UserAccount> EnsureAsync(Guid supabaseUserId, CancellationToken cancellationToken)
        {
            Calls.Add("ensure");
            Existing ??= UserAccount.Create(supabaseUserId, Now);
            return Task.FromResult(Existing);
        }
    }

    private sealed class ReconciliationFakes :
        IAchievementReconciliationTransaction,
        IReconciliationScope,
        IGamificationStatisticsReader,
        IAchievementCatalog,
        IUserAchievements,
        IGamificationPreferencesStore
    {
        public bool AccountExists { get; set; } = true;

        public GamificationStatistics Statistics { get; set; } =
            GamificationStatistics.ForAccountWithoutActivity(null, null, Now);

        public List<UserAchievement> Stored { get; } = [];

        public List<UserAchievement> Saved { get; } = [];

        public GamificationPreferencesData Preferences { get; set; } = new(true, false, Now);

        public List<string> Events { get; } = [];

        public bool ThrowOnSave { get; set; }

        public Task<IReconciliationScope?> BeginAsync(Guid accountId, CancellationToken cancellationToken)
        {
            Events.Add("begin");
            return Task.FromResult<IReconciliationScope?>(AccountExists ? this : null);
        }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            Events.Add("commit");
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            Events.Add("dispose");
            return ValueTask.CompletedTask;
        }

        public Task<GamificationStatistics> GetLifetimeAsync(Guid accountId, CancellationToken cancellationToken)
        {
            Events.Add("statistics");
            return Task.FromResult(Statistics);
        }

        public Task<GamificationStatistics> GetPeriodAsync(
            Guid accountId,
            DateTimeOffset periodStart,
            DateTimeOffset periodEnd,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<AchievementDefinition>> ListActiveAsync(CancellationToken cancellationToken)
        {
            Events.Add("definitions");
            return Task.FromResult<IReadOnlyList<AchievementDefinition>>(
                AchievementDefinition.InitialCatalog.OrderBy(definition => definition.Code).ToArray());
        }

        public Task<IReadOnlyList<UserAchievement>> ListAsync(Guid accountId, CancellationToken cancellationToken)
        {
            Events.Add("achievements");
            return Task.FromResult<IReadOnlyList<UserAchievement>>(Stored.ToArray());
        }

        public Task SaveAsync(IReadOnlyCollection<UserAchievement> granted, CancellationToken cancellationToken)
        {
            Events.Add("save");
            if (ThrowOnSave)
            {
                throw new InvalidOperationException("storage down");
            }

            Saved.AddRange(granted);
            Stored.AddRange(granted);
            return Task.CompletedTask;
        }

        public Task<GamificationPreferencesData?> FindAsync(Guid accountId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GamificationPreferencesData> EnsureAsync(Guid accountId, CancellationToken cancellationToken)
        {
            Events.Add("preferences");
            return Task.FromResult(Preferences);
        }

        public Task<GamificationPreferencesData> SaveAsync(
            Guid accountId,
            bool badgesPublic,
            bool rankingEnabled,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private static AchievementReconciler Reconciler(ReconciliationFakes fakes) =>
        new(fakes, fakes, fakes, fakes, fakes, new FixedTimeProvider(Now));

    private static GamificationStatistics Stats(int movies = 0, int episodes = 0, int series = 0, int genres = 0, int missing = 0) =>
        GamificationStatistics.ForAccountWithoutActivity(null, null, Now) with
        {
            MoviesWatched = movies,
            EpisodesWatched = episodes,
            SeriesExplored = series,
            GenresExplored = genres,
            MoviesMissingGenreMetadata = missing,
        };

    [Fact]
    public async Task Dashboard_ReconcilesInsideTheTransactionInTheSameOrderAsBefore()
    {
        var fakes = new ReconciliationFakes { Statistics = Stats(movies: 1) };
        var accounts = new Accounts();
        var useCase = new GetGamificationDashboardUseCase(accounts, Reconciler(fakes));

        var result = await useCase.ExecuteAsync(new GetGamificationDashboardQuery(SubjectId), CancellationToken.None);

        Assert.Equal(["ensure"], accounts.Calls);
        Assert.Equal(
            ["begin", "statistics", "definitions", "achievements", "preferences", "save", "commit", "dispose"],
            fakes.Events);
        var granted = Assert.Single(fakes.Saved);
        Assert.Equal(UserAchievementStatus.Earned, granted.Status);
        Assert.Equal(["cinephile", "first-marathon", "first-movie", "genre-explorer", "series-in-focus"],
            result.Achievements.Select(item => item.Code));
        var firstMovie = result.Achievements.Single(item => item.Code == "first-movie");
        Assert.True(firstMovie.IsEarned);
        Assert.Equal(AchievementStatus.Earned, firstMovie.Status);
        Assert.Equal(Now, firstMovie.FirstEarnedAt);
        Assert.All(
            result.Achievements.Where(item => item.Code != "first-movie"),
            item =>
            {
                Assert.False(item.IsEarned);
                Assert.Null(item.Status);
            });
        Assert.Equal(fakes.Preferences, result.Preferences);
        Assert.Equal(1, result.LifetimeStatistics.MoviesWatched);
    }

    [Fact]
    public async Task Dashboard_RevokedAchievementsReportTheRevokedStatus()
    {
        var fakes = new ReconciliationFakes { Statistics = Stats(movies: 3) };
        var cinephile = AchievementDefinition.InitialCatalog.Single(item => item.Code == "cinephile");
        var existing = UserAchievement.Create(Guid.NewGuid(), cinephile.Id, Now.AddDays(-9));
        fakes.Stored.Add(existing);
        var useCase = new GetGamificationDashboardUseCase(new Accounts(), Reconciler(fakes));

        var result = await useCase.ExecuteAsync(new GetGamificationDashboardQuery(SubjectId), CancellationToken.None);

        var progress = result.Achievements.Single(item => item.Code == "cinephile");
        Assert.False(progress.IsEarned);
        Assert.Equal(AchievementStatus.Revoked, progress.Status);
        Assert.Equal(Now, progress.LastRevokedAt);
        Assert.Equal(Now.AddDays(-9), progress.FirstEarnedAt);
    }

    [Fact]
    public async Task Dashboard_ARollbackHappensWhenSavingFails()
    {
        var fakes = new ReconciliationFakes { Statistics = Stats(movies: 1), ThrowOnSave = true };
        var useCase = new GetGamificationDashboardUseCase(new Accounts(), Reconciler(fakes));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new GetGamificationDashboardQuery(SubjectId), CancellationToken.None));

        Assert.DoesNotContain("commit", fakes.Events);
        Assert.Equal("dispose", fakes.Events[^1]);
    }

    [Fact]
    public async Task PublicBadges_OfAnUnknownAccount_AreNull()
    {
        var fakes = new ReconciliationFakes { AccountExists = false };
        var useCase = new GetPublicBadgesUseCase(Reconciler(fakes), new BadgesReader([]));

        var result = await useCase.ExecuteAsync(new GetPublicBadgesQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task PublicBadges_StillReconcileTheTargetBeforeReadingIt_Q1OptionA()
    {
        var fakes = new ReconciliationFakes { Statistics = Stats(movies: 1) };
        var reader = new BadgesReader([new PublicBadge("first-movie", "t", "d", Now)]);
        var useCase = new GetPublicBadgesUseCase(Reconciler(fakes), reader);

        var result = await useCase.ExecuteAsync(new GetPublicBadgesQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Contains("save", fakes.Events);
        Assert.Contains("commit", fakes.Events);
        Assert.True(result!.IsPublic);
        Assert.Single(result.Badges);
    }

    [Fact]
    public async Task PublicBadges_OfAPrivateAccount_AreHiddenAndNotRead()
    {
        var fakes = new ReconciliationFakes { Preferences = new GamificationPreferencesData(false, false, Now) };
        var reader = new BadgesReader([new PublicBadge("first-movie", "t", "d", Now)]);
        var useCase = new GetPublicBadgesUseCase(Reconciler(fakes), reader);

        var result = await useCase.ExecuteAsync(new GetPublicBadgesQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result!.IsPublic);
        Assert.Empty(result.Badges);
        Assert.Equal(0, reader.Calls);
    }

    private sealed class BadgesReader(IReadOnlyList<PublicBadge> badges) : IPublicBadgesReader
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyList<PublicBadge>> ListEarnedAsync(Guid accountId, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(badges);
        }
    }

    private sealed class StatisticsSpy(GamificationStatistics statistics) : IGamificationStatisticsReader
    {
        public int Calls { get; private set; }

        public Task<GamificationStatistics> GetLifetimeAsync(Guid accountId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GamificationStatistics> GetPeriodAsync(
            Guid accountId,
            DateTimeOffset periodStart,
            DateTimeOffset periodEnd,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(statistics);
        }
    }

    [Fact]
    public async Task Statistics_OfAnAccountThatDoesNotExistYet_AreEmptyAndNothingIsReadOrCreated()
    {
        var accounts = new Accounts();
        var spy = new StatisticsSpy(Stats(movies: 9));
        var useCase = new GetGamificationStatisticsUseCase(accounts, spy, new FixedTimeProvider(Now));
        var start = new DateTimeOffset(2030, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var result = await useCase.ExecuteAsync(
            new GetGamificationStatisticsQuery(SubjectId, start, start.AddMonths(1)),
            CancellationToken.None);

        Assert.Equal(["find"], accounts.Calls);
        Assert.Equal(0, spy.Calls);
        Assert.Equal(0, result.Statistics.MoviesWatched);
        Assert.Equal(start, result.Statistics.PeriodStart);
        Assert.Equal(start.AddMonths(1), result.Statistics.PeriodEnd);
        Assert.Equal(Now, result.Statistics.CalculatedAt);
        Assert.Null(result.Statistics.EstimatedWatchMinutes);
        Assert.Empty(result.Statistics.RuntimeSources);
        Assert.Equal(0, result.Statistics.RuntimeCoverage);
    }

    [Fact]
    public async Task Statistics_OfAnExistingAccount_AreReadForThePeriodInUtc()
    {
        var accounts = new Accounts { Existing = UserAccount.Create(SubjectId, Now) };
        var spy = new StatisticsSpy(Stats(movies: 4));
        var useCase = new GetGamificationStatisticsUseCase(accounts, spy, new FixedTimeProvider(Now));

        var result = await useCase.ExecuteAsync(
            new GetGamificationStatisticsQuery(
                SubjectId,
                new DateTimeOffset(2030, 6, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2030, 6, 30, 12, 0, 0, TimeSpan.FromHours(-3))),
            CancellationToken.None);

        Assert.Equal(1, spy.Calls);
        Assert.Equal(4, result.Statistics.MoviesWatched);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0, 367)]
    public async Task Statistics_RejectTheEmptyReversedOrTooLongPeriodBeforeLookingAtTheAccount(
        int startDays,
        int endDays)
    {
        var accounts = new Accounts();
        var useCase = new GetGamificationStatisticsUseCase(
            accounts,
            new StatisticsSpy(Stats()),
            new FixedTimeProvider(Now));
        var start = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var error = await Assert.ThrowsAsync<ValidationError>(() => useCase.ExecuteAsync(
            new GetGamificationStatisticsQuery(SubjectId, start.AddDays(startDays), start.AddDays(endDays)),
            CancellationToken.None));

        Assert.Equal("O período deve ser válido e não pode exceder 366 dias.", error.Message);
        Assert.Empty(accounts.Calls);
    }

    private sealed class RankingReader(MonthlyRankingData data) : IMonthlyRankingReader
    {
        public int Calls { get; private set; }

        public GamificationRankingCursor? Cursor { get; private set; }

        public Task<MonthlyRankingData> GetAsync(
            Guid viewerAccountId,
            DateTimeOffset periodStart,
            DateTimeOffset periodEnd,
            GamificationRankingCursor? cursor,
            int limit,
            CancellationToken cancellationToken)
        {
            Calls++;
            Cursor = cursor;
            return Task.FromResult(data);
        }
    }

    [Fact]
    public async Task Ranking_WithoutAnAccount_IsTheDisabledPageAndDoesNotReadAnything()
    {
        var accounts = new Accounts();
        var reader = new RankingReader(MonthlyRankingData.Disabled);
        var useCase = new GetMonthlyRankingUseCase(accounts, reader);

        var result = await useCase.ExecuteAsync(
            new GetMonthlyRankingQuery(SubjectId, 2030, 6, null, 20),
            CancellationToken.None);

        Assert.Equal(["find"], accounts.Calls);
        Assert.Equal(0, reader.Calls);
        Assert.False(result.IsEnabled);
        Assert.Equal(new DateTimeOffset(2030, 6, 1, 0, 0, 0, TimeSpan.Zero), result.PeriodStart);
        Assert.Equal(new DateTimeOffset(2030, 7, 1, 0, 0, 0, TimeSpan.Zero), result.PeriodEnd);
        Assert.Equal(0, result.TotalParticipants);
        Assert.Empty(result.Items);
        Assert.Null(result.NextCursor);
    }

    [Fact]
    public async Task Ranking_ComputesCompetitionPositionsAndTheNextCursorOfTheLastEntry()
    {
        var first = Guid.Parse("00000000-0000-0000-0000-00000000000a");
        var second = Guid.Parse("00000000-0000-0000-0000-00000000000b");
        var third = Guid.Parse("00000000-0000-0000-0000-00000000000c");
        var data = new MonthlyRankingData(
            true,
            [
                new RankingCandidate(first, "Primeira", 5),
                new RankingCandidate(second, "Segunda", 2),
                new RankingCandidate(third, "Terceira", 2),
            ],
            [new RankingScoreBucket(5, 1), new RankingScoreBucket(2, 3)]);
        var accounts = new Accounts { Existing = UserAccount.Create(SubjectId, Now) };
        var useCase = new GetMonthlyRankingUseCase(accounts, new RankingReader(data));

        var result = await useCase.ExecuteAsync(
            new GetMonthlyRankingQuery(SubjectId, 2030, 6, null, 2),
            CancellationToken.None);

        Assert.True(result.IsEnabled);
        Assert.Equal(4, result.TotalParticipants);
        Assert.Equal([1, 2], result.Items.Select(item => item.Position));
        Assert.Equal([first, second], result.Items.Select(item => item.AccountId));
        Assert.Equal(
            Reelix.Application.Common.CursorCodec.EncodeText($"2:{second}"),
            result.NextCursor);
    }

    [Fact]
    public async Task Ranking_OnTheLastPage_HasNoNextCursor()
    {
        var only = Guid.NewGuid();
        var data = new MonthlyRankingData(
            true,
            [new RankingCandidate(only, "Única", 3)],
            [new RankingScoreBucket(3, 1)]);
        var accounts = new Accounts { Existing = UserAccount.Create(SubjectId, Now) };
        var useCase = new GetMonthlyRankingUseCase(accounts, new RankingReader(data));

        var result = await useCase.ExecuteAsync(
            new GetMonthlyRankingQuery(SubjectId, 2030, 6, null, 20),
            CancellationToken.None);

        Assert.Null(result.NextCursor);
        Assert.Equal(1, Assert.Single(result.Items).Position);
    }

    [Theory]
    [InlineData(1999, 6, 20, "Informe um ano e mês válidos.")]
    [InlineData(9999, 6, 20, "Informe um ano e mês válidos.")]
    [InlineData(2030, 0, 20, "Informe um ano e mês válidos.")]
    [InlineData(2030, 13, 20, "Informe um ano e mês válidos.")]
    [InlineData(2030, 6, 0, "O limite deve estar entre 1 e 50.")]
    [InlineData(2030, 6, 51, "O limite deve estar entre 1 e 50.")]
    public async Task Ranking_RejectsAnInvalidQueryBeforeLookingAtTheAccount(
        int year,
        int month,
        int limit,
        string message)
    {
        var accounts = new Accounts();
        var useCase = new GetMonthlyRankingUseCase(accounts, new RankingReader(MonthlyRankingData.Disabled));

        var error = await Assert.ThrowsAsync<ValidationError>(() => useCase.ExecuteAsync(
            new GetMonthlyRankingQuery(SubjectId, year, month, null, limit),
            CancellationToken.None));

        Assert.Equal(message, error.Message);
        Assert.Empty(accounts.Calls);
    }

    private sealed class PreferencesSpy : IGamificationPreferencesStore
    {
        public (Guid AccountId, bool Badges, bool Ranking)? Saved { get; private set; }

        public Task<GamificationPreferencesData?> FindAsync(Guid accountId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GamificationPreferencesData> EnsureAsync(Guid accountId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GamificationPreferencesData> SaveAsync(
            Guid accountId,
            bool badgesPublic,
            bool rankingEnabled,
            CancellationToken cancellationToken)
        {
            Saved = (accountId, badgesPublic, rankingEnabled);
            return Task.FromResult(new GamificationPreferencesData(badgesPublic, rankingEnabled, Now));
        }
    }

    [Fact]
    public async Task UpdatePreferences_ProvisionsTheAccountAndSavesForItsInternalId()
    {
        var accounts = new Accounts();
        var store = new PreferencesSpy();
        var useCase = new UpdateGamificationPreferencesUseCase(accounts, store);

        var result = await useCase.ExecuteAsync(
            new UpdateGamificationPreferencesCommand(SubjectId, false, true),
            CancellationToken.None);

        Assert.Equal(["ensure"], accounts.Calls);
        Assert.Equal(accounts.Existing!.Id, store.Saved!.Value.AccountId);
        Assert.NotEqual(SubjectId, store.Saved.Value.AccountId);
        Assert.False(result.Preferences.BadgesPublic);
        Assert.True(result.Preferences.RankingEnabled);
    }
}

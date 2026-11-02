using Reelix.Application.Authentication;
using Reelix.Application.Catalog;
using Reelix.Application.Ratings;
using Reelix.Application.Tracking;
using Reelix.Domain.Ratings;
using Reelix.Domain.Users;

namespace Reelix.UnitTests.Ratings;

public sealed class RatingsCommentsServiceTests
{
    [Fact]
    public async Task OwnRatings_ResolveAccountAndValidatePagination()
    {
        var subject = Guid.NewGuid();
        var accounts = new FakeAccounts(subject);
        var repository = new FakeRatingsCommentsRepository();
        var service = CreateService(accounts, repository);
        var page = await service.GetOwnRatingsAsync(subject, 2, 10, CancellationToken.None);
        Assert.Equal(accounts.Account.Id, repository.RatingsAccountId);
        Assert.Equal(2, page.Page);
        Assert.Equal(10, page.PageSize);
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetOwnRatingsAsync(subject, 0, 10, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetOwnRatingsAsync(subject, 1, 51, CancellationToken.None));
    }

    [Fact]
    public async Task UpsertRating_UsesResolvedAccountAndStoresItsAudience()
    {
        var subject = Guid.NewGuid();
        var accounts = new FakeAccounts(subject);
        var repository = new FakeRatingsCommentsRepository { CatalogTargetExists = true };
        var service = CreateService(accounts, repository);

        var result = await service.UpsertRatingAsync(
            subject,
            Guid.NewGuid(),
            ReviewTargetType.Movie,
            new RatingInput(0, ReviewAudience.Public),
            CancellationToken.None);

        Assert.Equal(accounts.Account.Id, repository.SavedRating!.AccountId);
        Assert.NotEqual(subject, repository.SavedRating.AccountId);
        Assert.Equal(0, repository.SavedRating.Value);
        Assert.Equal(ReviewAudience.Public, repository.SavedRating.Audience);
        Assert.Equal(0, result.PublicCount);
    }

    [Fact]
    public async Task UpsertRating_RejectsInvalidScoreBeforePersisting()
    {
        var subject = Guid.NewGuid();
        var repository = new FakeRatingsCommentsRepository { CatalogTargetExists = true };
        var service = CreateService(new FakeAccounts(subject), repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpsertRatingAsync(
                subject,
                Guid.NewGuid(),
                ReviewTargetType.Movie,
                new RatingInput(5.5m, ReviewAudience.Public),
                CancellationToken.None));

        Assert.Null(repository.SavedRating);
    }

    [Fact]
    public async Task UpsertEpisodeRating_RequiresEpisodeToBeTracked()
    {
        var subject = Guid.NewGuid();
        var repository = new FakeRatingsCommentsRepository
        {
            CatalogTargetExists = true,
            EpisodeBelongsToSeries = true,
        };
        var service = CreateService(
            new FakeAccounts(subject),
            repository,
            episodeIsTracked: false);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpsertRatingAsync(
                subject,
                Guid.NewGuid(),
                ReviewTargetType.Episode,
                new RatingInput(4, ReviewAudience.Private, Guid.NewGuid()),
                CancellationToken.None));

        Assert.Null(repository.SavedRating);
    }

    [Fact]
    public async Task EpisodeComment_RejectsAnEpisodeLinkedToAnotherSeries()
    {
        var subject = Guid.NewGuid();
        var repository = new FakeRatingsCommentsRepository
        {
            CatalogTargetExists = true,
            EpisodeBelongsToSeries = false,
        };
        var service = CreateService(new FakeAccounts(subject), repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateCommentAsync(
                subject,
                Guid.NewGuid(),
                ReviewTargetType.Episode,
                new CommentInput(
                    "Episode comment",
                    false,
                    ReviewAudience.Public,
                    false,
                    Guid.NewGuid()),
                CancellationToken.None));

        Assert.Null(repository.SavedComment);
    }

    [Fact]
    public async Task CreateComment_DoesNotQueueExternalAnalysisWithoutServerConfiguration()
    {
        var subject = Guid.NewGuid();
        var repository = new FakeRatingsCommentsRepository { CatalogTargetExists = true };
        var service = CreateService(
            new FakeAccounts(subject),
            repository,
            analysisEnabled: false);

        var result = await service.CreateCommentAsync(
            subject,
            Guid.NewGuid(),
            ReviewTargetType.Movie,
            new CommentInput(
                "Spoiler analysis was not requested externally.",
                false,
                ReviewAudience.Private,
                RequestAutomatedAnalysis: true),
            CancellationToken.None);

        Assert.NotNull(repository.SavedComment);
        Assert.False(repository.SavedComment.AutomatedAnalysisRequested);
        Assert.Equal(CommentModerationStatus.Published, result.ModerationStatus);
        Assert.Equal(ReviewAudience.Private, result.Audience);
    }

    [Fact]
    public async Task DeleteRating_UsesResolvedInternalAccountId()
    {
        var subject = Guid.NewGuid();
        var accounts = new FakeAccounts(subject);
        var repository = new FakeRatingsCommentsRepository();
        var service = CreateService(accounts, repository);

        await service.DeleteRatingAsync(
            subject,
            Guid.NewGuid(),
            ReviewTargetType.Movie,
            CancellationToken.None);

        Assert.Equal(accounts.Account.Id, repository.DeletedRatingAccountId);
        Assert.NotEqual(subject, repository.DeletedRatingAccountId);
        Assert.Equal(ReviewTargetType.Movie, repository.DeletedRatingTargetType);
    }

    private static RatingsCommentsService CreateService(
        FakeAccounts accounts,
        FakeRatingsCommentsRepository repository,
        bool analysisEnabled = false,
        bool episodeIsTracked = true) =>
        new(accounts, repository, new FakeEpisodeTrackingLookup(episodeIsTracked),
            new FakeModerationConfiguration(analysisEnabled));

    private sealed class FakeEpisodeTrackingLookup(bool isTracked) : IEpisodeTrackingLookup
    {
        public Task<bool> IsEpisodeTrackedAsync(
            Guid accountId,
            Guid episodeId,
            CancellationToken cancellationToken) =>
            Task.FromResult(isTracked);
    }

    private sealed class FakeAccounts(Guid subject) : IUserAccountRepository
    {
        public UserAccount Account { get; } = UserAccount.Create(subject);

        public Task<UserAccount> GetOrCreateAsync(Guid supabaseUserId, CancellationToken cancellationToken) =>
            Task.FromResult(supabaseUserId == subject
                ? Account
                : throw new InvalidOperationException("Unexpected authenticated subject."));
    }

    private sealed class FakeModerationConfiguration(bool isEnabled)
        : ICommentModerationConfiguration
    {
        public bool IsEnabled => isEnabled;
    }

    private sealed class FakeRatingsCommentsRepository : IRatingsCommentsRepository
    {
        public Guid? RatingsAccountId { get; private set; }
        public Task<OwnRatingsPage> GetOwnRatingsAsync(Guid accountId, int page, int pageSize, CancellationToken cancellationToken)
        {
            RatingsAccountId = accountId;
            return Task.FromResult(new OwnRatingsPage([], page, pageSize, 0));
        }

        public bool CatalogTargetExists { get; init; }

        public bool EpisodeBelongsToSeries { get; init; }

        public UserRating? SavedRating { get; private set; }

        public UserComment? SavedComment { get; private set; }

        public Guid? DeletedRatingAccountId { get; private set; }

        public ReviewTargetType? DeletedRatingTargetType { get; private set; }

        public Task<bool> CatalogTargetExistsAsync(
            Guid targetId,
            CatalogReferenceType expectedType,
            CancellationToken cancellationToken) =>
            Task.FromResult(CatalogTargetExists);

        public Task<bool> EpisodeBelongsToSeriesAsync(
            Guid episodeId,
            Guid seriesId,
            CancellationToken cancellationToken) =>
            Task.FromResult(EpisodeBelongsToSeries);

        public Task<RatingSummary> GetRatingSummaryAsync(
            Guid viewerAccountId,
            Guid targetId,
            ReviewTargetType targetType,
            CancellationToken cancellationToken) =>
            Task.FromResult(new RatingSummary(targetId, targetType, null, 0, null));

        public Task UpsertRatingAsync(UserRating rating, CancellationToken cancellationToken)
        {
            SavedRating = rating;
            return Task.CompletedTask;
        }

        public Task DeleteRatingAsync(
            Guid accountId,
            Guid targetId,
            ReviewTargetType targetType,
            CancellationToken cancellationToken)
        {
            DeletedRatingAccountId = accountId;
            DeletedRatingTargetType = targetType;
            return Task.CompletedTask;
        }

        public Task AddCommentAsync(UserComment comment, CancellationToken cancellationToken)
        {
            SavedComment = comment;
            return Task.CompletedTask;
        }

        public Task<CommentItem?> UpdateCommentAsync(
            Guid accountId,
            Guid commentId,
            string content,
            bool containsSpoiler,
            ReviewAudience audience,
            bool requestAutomatedAnalysis,
            bool automatedAnalysisEnabled,
            CancellationToken cancellationToken) =>
            Task.FromResult<CommentItem?>(null);

        public Task<bool> DeleteCommentAsync(
            Guid accountId,
            Guid commentId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<PagedComments> GetCommentsAsync(
            Guid viewerAccountId,
            Guid targetId,
            ReviewTargetType targetType,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PagedComments([], page, pageSize, 0));

        public Task<RevealedComment?> RevealCommentAsync(
            Guid viewerAccountId,
            Guid commentId,
            CancellationToken cancellationToken) =>
            Task.FromResult<RevealedComment?>(null);

        public Task<IReadOnlyList<ModerationWorkItem>> ClaimModerationWorkAsync(
            int batchSize,
            DateTimeOffset now,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ModerationWorkItem>>([]);

        public Task CompleteModerationAsync(
            ModerationWorkItem workItem,
            SpoilerClassification classification,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task FailModerationAsync(
            ModerationWorkItem workItem,
            int maxAttempts,
            DateTimeOffset retryAfter,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

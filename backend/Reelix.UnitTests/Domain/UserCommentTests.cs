using Reelix.Domain.Ratings;

namespace Reelix.UnitTests.Domain;

public sealed class UserCommentTests
{
    [Fact]
    public void Create_NormalizesContentAndPublishesWhenAnalysisIsDisabled()
    {
        var comment = UserComment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReviewTargetType.Movie,
            null,
            "  comentário  ",
            containsSpoiler: false,
            ReviewAudience.Private,
            requestAutomatedAnalysis: true,
            automatedAnalysisEnabled: false);

        Assert.Equal("comentário", comment.Content);
        Assert.Equal(CommentModerationStatus.Published, comment.ModerationStatus);
        Assert.False(comment.AutomatedAnalysisRequested);
    }

    [Fact]
    public void Create_QueuesOnlyWithExplicitConsentAndEnabledAnalysis()
    {
        var withoutConsent = CreateComment(requestAutomatedAnalysis: false);
        var withConsent = CreateComment(requestAutomatedAnalysis: true);

        Assert.Equal(CommentModerationStatus.Published, withoutConsent.ModerationStatus);
        Assert.Equal(CommentModerationStatus.Pending, withConsent.ModerationStatus);
        Assert.True(withConsent.AutomatedAnalysisRequested);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_RejectsBlankContent(string content)
    {
        Assert.Throws<ArgumentException>(() =>
            UserComment.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ReviewTargetType.Movie,
                null,
                content,
                false,
                ReviewAudience.Private,
                false,
                false));
    }

    [Fact]
    public void UpdateInvalidatesAnOlderAnalysisAndPreservesManualSpoiler()
    {
        var comment = CreateComment(containsSpoiler: true, requestAutomatedAnalysis: true);
        var leaseId = Guid.NewGuid();
        var initialVersion = comment.ModerationVersion;
        Assert.True(comment.TryClaimModeration(
            leaseId,
            DateTimeOffset.UtcNow.AddMinutes(2),
            DateTimeOffset.UtcNow));

        comment.Update(
            "Updated content",
            containsSpoiler: true,
            ReviewAudience.Public,
            requestAutomatedAnalysis: true,
            automatedAnalysisEnabled: true);
        comment.CompleteModeration(initialVersion, leaseId, possibleSpoiler: false);

        Assert.Equal(initialVersion + 1, comment.ModerationVersion);
        Assert.Equal(CommentModerationStatus.Pending, comment.ModerationStatus);
        Assert.True(comment.ContainsSpoiler);
        Assert.False(comment.PossibleSpoiler);
    }

    [Fact]
    public void RepeatedAnalysisFailuresPublishWithConservativeSpoilerFlag()
    {
        var comment = CreateComment(requestAutomatedAnalysis: true);
        var now = DateTimeOffset.UtcNow;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var leaseId = Guid.NewGuid();
            Assert.True(comment.TryClaimModeration(
                leaseId,
                now.AddMinutes(2),
                now));
            comment.RecordModerationFailure(
                comment.ModerationVersion,
                leaseId,
                maxAttempts: 3,
                retryAfter: now.AddMinutes(1));
            now = now.AddMinutes(2);
        }

        Assert.Equal(CommentModerationStatus.Published, comment.ModerationStatus);
        Assert.True(comment.PossibleSpoiler);
    }

    private static UserComment CreateComment(
        bool containsSpoiler = false,
        bool requestAutomatedAnalysis = false) =>
        UserComment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReviewTargetType.Series,
            null,
            "A review comment",
            containsSpoiler,
            ReviewAudience.Private,
            requestAutomatedAnalysis,
            automatedAnalysisEnabled: true);
}

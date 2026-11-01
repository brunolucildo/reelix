using Reelix.Domain.Ratings;

namespace Reelix.UnitTests.Domain;

public sealed class UserRatingTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(2.5)]
    [InlineData(5)]
    public void Create_AcceptsScoresInHalfStarSteps(decimal score)
    {
        var rating = UserRating.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReviewTargetType.Movie,
            null,
            score,
            ReviewAudience.Private);

        Assert.Equal(score, rating.Value);
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(0.1)]
    [InlineData(5.5)]
    public void Create_RejectsScoresOutsideTheHalfStarScale(decimal score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserRating.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ReviewTargetType.Movie,
                null,
                score,
                ReviewAudience.Private));
    }

    [Fact]
    public void Create_RequiresSeriesIdOnlyForEpisodes()
    {
        Assert.Throws<ArgumentException>(() =>
            UserRating.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ReviewTargetType.Episode,
                null,
                4,
                ReviewAudience.Public));
        Assert.Throws<ArgumentException>(() =>
            UserRating.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ReviewTargetType.Series,
                Guid.NewGuid(),
                4,
                ReviewAudience.Public));
    }
}

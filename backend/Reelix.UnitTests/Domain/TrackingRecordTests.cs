using Reelix.Domain.Tracking;

namespace Reelix.UnitTests.Domain;

public sealed class TrackingRecordTests
{
    [Fact]
    public void CreateMovie_UsesCurrentTimeWhenNoDateIsProvided()
    {
        var before = DateTimeOffset.UtcNow;

        var record = TrackingRecord.CreateMovie(Guid.NewGuid(), Guid.NewGuid());

        Assert.InRange(record.WatchedAt, before, DateTimeOffset.UtcNow);
        Assert.Equal(TrackingItemType.Movie, record.ItemType);
        Assert.Null(record.SeriesId);
    }

    [Fact]
    public void CreateEpisode_PreservesIdentityWithoutEpisodeNumber()
    {
        var seriesId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();
        var watchedAt = new DateTimeOffset(2020, 5, 1, 13, 30, 0, TimeSpan.FromHours(-3));

        var record = TrackingRecord.CreateEpisode(
            Guid.NewGuid(),
            seriesId,
            episodeId,
            watchedAt);

        Assert.Equal(episodeId, record.TargetId);
        Assert.Equal(seriesId, record.SeriesId);
        Assert.Equal(watchedAt.ToUniversalTime(), record.WatchedAt);
    }

    [Fact]
    public void CreateEpisode_RejectsEmptyCatalogIds()
    {
        Assert.Throws<ArgumentException>(() =>
            TrackingRecord.CreateEpisode(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void CorrectWatchedAt_RejectsFutureDates()
    {
        var record = TrackingRecord.CreateMovie(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<ArgumentException>(() =>
            record.CorrectWatchedAt(DateTimeOffset.UtcNow.AddDays(1)));
    }
}

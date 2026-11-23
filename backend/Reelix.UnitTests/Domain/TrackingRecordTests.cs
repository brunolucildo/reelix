using Reelix.Domain.Tracking;
using Reelix.UnitTests.Common;

namespace Reelix.UnitTests.Domain;

public sealed class TrackingRecordTests
{
    private static readonly DateTimeOffset Now = FixedTimeProvider.DefaultNow;

    [Fact]
    public void CreateMovie_UsesTheReceivedInstantWhenNoDateIsProvided()
    {
        var record = TrackingRecord.CreateMovie(Guid.NewGuid(), Guid.NewGuid(), Now);

        Assert.Equal(Now, record.WatchedAt);
        Assert.Equal(Now, record.CreatedAt);
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
            Now,
            watchedAt);

        Assert.Equal(episodeId, record.TargetId);
        Assert.Equal(seriesId, record.SeriesId);
        Assert.Equal(watchedAt.ToUniversalTime(), record.WatchedAt);
    }

    [Fact]
    public void CreateEpisode_RejectsEmptyCatalogIds()
    {
        Assert.Throws<ArgumentException>(() =>
            TrackingRecord.CreateEpisode(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Now));
    }

    [Fact]
    public void CorrectWatchedAt_RejectsDatesAfterTheReceivedInstant()
    {
        var record = TrackingRecord.CreateMovie(Guid.NewGuid(), Guid.NewGuid(), Now);

        Assert.Throws<ArgumentException>(() =>
            record.CorrectWatchedAt(Now.AddSeconds(1), Now));
        record.CorrectWatchedAt(Now, Now);
        Assert.Equal(Now, record.WatchedAt);
    }

    [Fact]
    public void CreateMovie_RejectsAWatchedDateAfterTheReceivedInstant()
    {
        Assert.Throws<ArgumentException>(() =>
            TrackingRecord.CreateMovie(Guid.NewGuid(), Guid.NewGuid(), Now, Now.AddMinutes(1)));
    }
}

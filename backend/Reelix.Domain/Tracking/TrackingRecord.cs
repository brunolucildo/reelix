namespace Reelix.Domain.Tracking;

public enum TrackingItemType
{
    Movie,
    Episode,
}

public sealed class TrackingRecord
{
    private TrackingRecord()
    {
    }

    private TrackingRecord(
        Guid id,
        Guid accountId,
        Guid targetId,
        TrackingItemType itemType,
        Guid? seriesId,
        DateTimeOffset watchedAt,
        DateTimeOffset createdAt)
    {
        Id = id;
        AccountId = accountId;
        TargetId = targetId;
        ItemType = itemType;
        SeriesId = seriesId;
        WatchedAt = watchedAt;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid TargetId { get; private set; }

    public TrackingItemType ItemType { get; private set; }

    public Guid? SeriesId { get; private set; }

    public DateTimeOffset WatchedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static TrackingRecord CreateMovie(
        Guid accountId,
        Guid titleId,
        DateTimeOffset now,
        DateTimeOffset? watchedAt = null)
    {
        ValidateIdentity(accountId, titleId, nameof(titleId));
        return Create(accountId, titleId, TrackingItemType.Movie, null, now, watchedAt);
    }

    public static TrackingRecord CreateEpisode(
        Guid accountId,
        Guid seriesId,
        Guid episodeId,
        DateTimeOffset now,
        DateTimeOffset? watchedAt = null)
    {
        ValidateIdentity(accountId, seriesId, nameof(seriesId));
        ValidateIdentity(accountId, episodeId, nameof(episodeId));
        return Create(accountId, episodeId, TrackingItemType.Episode, seriesId, now, watchedAt);
    }

    public void CorrectWatchedAt(DateTimeOffset watchedAt, DateTimeOffset now)
    {
        WatchedAt = NormalizeWatchedAt(watchedAt, now);
    }

    private static TrackingRecord Create(
        Guid accountId,
        Guid targetId,
        TrackingItemType itemType,
        Guid? seriesId,
        DateTimeOffset now,
        DateTimeOffset? watchedAt)
    {
        var timestamp = NormalizeWatchedAt(watchedAt ?? now, now);
        return new TrackingRecord(
            Guid.NewGuid(),
            accountId,
            targetId,
            itemType,
            seriesId,
            timestamp,
            now);
    }

    private static void ValidateIdentity(Guid accountId, Guid targetId, string parameterName)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Informe uma conta válida.", nameof(accountId));
        }

        if (targetId == Guid.Empty)
        {
            throw new ArgumentException("Informe um identificador de catálogo válido.", parameterName);
        }
    }

    private static DateTimeOffset NormalizeWatchedAt(DateTimeOffset watchedAt, DateTimeOffset now)
    {
        if (watchedAt > now)
        {
            throw new ArgumentException("A data de visualização não pode estar no futuro.", nameof(watchedAt));
        }

        return watchedAt.ToUniversalTime();
    }
}

namespace Reelix.Domain.Ratings;

public enum ReviewTargetType
{
    Movie,
    Series,
    Episode,
}

public enum ReviewAudience
{
    Private,
    Public,
}

public enum CommentModerationStatus
{
    Pending,
    Published,
    Blocked,
}

namespace Reelix.Domain.Ratings;

public sealed class UserComment
{
    private UserComment()
    {
    }

    private UserComment(
        Guid id,
        Guid accountId,
        Guid targetId,
        ReviewTargetType targetType,
        Guid? seriesId,
        string content,
        bool containsSpoiler,
        ReviewAudience audience,
        bool automatedAnalysisRequested,
        CommentModerationStatus moderationStatus,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        AccountId = accountId;
        TargetId = targetId;
        TargetType = targetType;
        SeriesId = seriesId;
        Content = content;
        ContainsSpoiler = containsSpoiler;
        Audience = audience;
        AutomatedAnalysisRequested = automatedAnalysisRequested;
        ModerationStatus = moderationStatus;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid TargetId { get; private set; }

    public ReviewTargetType TargetType { get; private set; }

    public Guid? SeriesId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public bool ContainsSpoiler { get; private set; }

    public bool PossibleSpoiler { get; private set; }

    public ReviewAudience Audience { get; private set; }

    public bool AutomatedAnalysisRequested { get; private set; }

    public CommentModerationStatus ModerationStatus { get; private set; }

    public int ModerationVersion { get; private set; } = 1;

    public int ModerationAttempts { get; private set; }

    public Guid? ModerationLeaseId { get; private set; }

    public DateTimeOffset? ModerationLeaseUntil { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool TryClaimModeration(Guid leaseId, DateTimeOffset leaseUntil, DateTimeOffset now)
    {
        if (!AutomatedAnalysisRequested ||
            ModerationStatus != CommentModerationStatus.Pending ||
            ModerationLeaseUntil > now ||
            leaseId == Guid.Empty)
        {
            return false;
        }

        ModerationLeaseId = leaseId;
        ModerationLeaseUntil = leaseUntil;
        return true;
    }

    public static UserComment Create(
        Guid accountId,
        Guid targetId,
        ReviewTargetType targetType,
        Guid? seriesId,
        string content,
        bool containsSpoiler,
        ReviewAudience audience,
        bool requestAutomatedAnalysis,
        bool automatedAnalysisEnabled)
    {
        ValidateTarget(accountId, targetId, targetType, seriesId);
        ValidateAudience(audience);
        var normalizedContent = NormalizeContent(content);
        var now = DateTimeOffset.UtcNow;
        return new UserComment(
            Guid.NewGuid(),
            accountId,
            targetId,
            targetType,
            seriesId,
            normalizedContent,
            containsSpoiler,
            audience,
            requestAutomatedAnalysis && automatedAnalysisEnabled,
            requestAutomatedAnalysis && automatedAnalysisEnabled
                ? CommentModerationStatus.Pending
                : CommentModerationStatus.Published,
            now,
            now);
    }

    public void Update(
        string content,
        bool containsSpoiler,
        ReviewAudience audience,
        bool requestAutomatedAnalysis,
        bool automatedAnalysisEnabled)
    {
        ValidateAudience(audience);
        var normalizedContent = NormalizeContent(content);
        Content = normalizedContent;
        ContainsSpoiler = containsSpoiler;
        Audience = audience;
        AutomatedAnalysisRequested = requestAutomatedAnalysis && automatedAnalysisEnabled;
        PossibleSpoiler = false;
        ModerationVersion++;
        ModerationAttempts = 0;
        ModerationLeaseId = null;
        ModerationLeaseUntil = null;
        if (ModerationStatus != CommentModerationStatus.Blocked)
        {
            ModerationStatus = AutomatedAnalysisRequested
                ? CommentModerationStatus.Pending
                : CommentModerationStatus.Published;
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void CompleteModeration(
        int version,
        Guid leaseId,
        bool possibleSpoiler)
    {
        if (version != ModerationVersion ||
            leaseId == Guid.Empty ||
            ModerationLeaseId != leaseId ||
            ModerationStatus != CommentModerationStatus.Pending)
        {
            return;
        }

        PossibleSpoiler = possibleSpoiler;
        ModerationStatus = CommentModerationStatus.Published;
        ModerationLeaseId = null;
        ModerationLeaseUntil = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordModerationFailure(
        int version,
        Guid leaseId,
        int maxAttempts,
        DateTimeOffset retryAfter)
    {
        if (version != ModerationVersion ||
            leaseId == Guid.Empty ||
            ModerationLeaseId != leaseId ||
            ModerationStatus != CommentModerationStatus.Pending)
        {
            return;
        }

        ModerationAttempts++;
        ModerationLeaseId = null;
        ModerationLeaseUntil = ModerationAttempts >= maxAttempts
            ? null
            : retryAfter;
        if (ModerationAttempts >= maxAttempts)
        {
            PossibleSpoiler = true;
            ModerationStatus = CommentModerationStatus.Published;
        }
    }

    public static string NormalizeContent(string content)
    {
        var normalized = content?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 2000)
        {
            throw new ArgumentException(
                "O comentário deve ter entre 1 e 2.000 caracteres.",
                nameof(content));
        }

        return normalized;
    }

    private static void ValidateAudience(ReviewAudience audience)
    {
        if (!Enum.IsDefined(audience))
        {
            throw new ArgumentOutOfRangeException(nameof(audience), "A audiência do comentário é inválida.");
        }
    }

    private static void ValidateTarget(
        Guid accountId,
        Guid targetId,
        ReviewTargetType targetType,
        Guid? seriesId)
    {
        if (accountId == Guid.Empty || targetId == Guid.Empty)
        {
            throw new ArgumentException("Informe uma conta e um alvo válidos.");
        }

        if (!Enum.IsDefined(targetType))
        {
            throw new ArgumentOutOfRangeException(nameof(targetType), "O tipo de alvo é inválido.");
        }

        if (targetType == ReviewTargetType.Episode
                ? !seriesId.HasValue || seriesId == Guid.Empty
                : seriesId.HasValue)
        {
            throw new ArgumentException("Informe a série somente para comentários de episódios.", nameof(seriesId));
        }
    }
}

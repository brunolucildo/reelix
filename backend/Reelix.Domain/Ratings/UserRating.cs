namespace Reelix.Domain.Ratings;

public sealed class UserRating
{
    private UserRating()
    {
    }

    private UserRating(
        Guid id,
        Guid accountId,
        Guid targetId,
        ReviewTargetType targetType,
        Guid? seriesId,
        decimal value,
        ReviewAudience audience,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        AccountId = accountId;
        TargetId = targetId;
        TargetType = targetType;
        SeriesId = seriesId;
        Value = value;
        Audience = audience;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid TargetId { get; private set; }

    public ReviewTargetType TargetType { get; private set; }

    public Guid? SeriesId { get; private set; }

    public decimal Value { get; private set; }

    public ReviewAudience Audience { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static UserRating Create(
        Guid accountId,
        Guid targetId,
        ReviewTargetType targetType,
        Guid? seriesId,
        decimal value,
        ReviewAudience audience)
    {
        ValidateTarget(accountId, targetId, targetType, seriesId);
        ValidateValue(value);
        ValidateAudience(audience);
        var now = DateTimeOffset.UtcNow;
        return new UserRating(
            Guid.NewGuid(),
            accountId,
            targetId,
            targetType,
            seriesId,
            value,
            audience,
            now,
            now);
    }

    public void Update(decimal value, ReviewAudience audience)
    {
        ValidateValue(value);
        ValidateAudience(audience);
        Value = value;
        Audience = audience;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static void ValidateValue(decimal value)
    {
        if (value is < 0 or > 5 || value * 2 != decimal.Truncate(value * 2))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "A avaliação deve estar entre 0 e 5, em passos de meia estrela.");
        }

    }

    private static void ValidateAudience(ReviewAudience audience)
    {
        if (!Enum.IsDefined(audience))
        {
            throw new ArgumentOutOfRangeException(nameof(audience), "A audiência da avaliação é inválida.");
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
            throw new ArgumentException("Informe a série somente para avaliações de episódios.", nameof(seriesId));
        }
    }
}

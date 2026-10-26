namespace Reelix.Domain.Catalog;

public sealed class ExternalCatalogReference
{
    private ExternalCatalogReference()
    {
    }

    private ExternalCatalogReference(
        Guid id,
        string provider,
        string externalId,
        string contentType,
        DateTimeOffset createdAt)
    {
        Id = id;
        Provider = provider;
        ExternalId = externalId;
        ContentType = contentType;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string ExternalId { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public static ExternalCatalogReference Create(
        string provider,
        string externalId,
        string contentType)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("A provider is required.", nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException("An external identifier is required.", nameof(externalId));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("A content type is required.", nameof(contentType));
        }

        return new ExternalCatalogReference(
            Guid.NewGuid(),
            provider.Trim().ToLowerInvariant(),
            externalId.Trim(),
            contentType.Trim().ToLowerInvariant(),
            DateTimeOffset.UtcNow);
    }
}

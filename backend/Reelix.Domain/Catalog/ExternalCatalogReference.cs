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

    public int? RuntimeMinutes { get; private set; }

    public DateTimeOffset? MetadataUpdatedAt { get; private set; }

    public int MetadataVersion { get; private set; }

    public static ExternalCatalogReference Create(
        string provider,
        string externalId,
        string contentType)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Informe o provedor do catálogo.", nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException("Informe o identificador externo.", nameof(externalId));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Informe o tipo de conteúdo.", nameof(contentType));
        }

        return new ExternalCatalogReference(
            Guid.NewGuid(),
            provider.Trim().ToLowerInvariant(),
            externalId.Trim(),
            contentType.Trim().ToLowerInvariant(),
            DateTimeOffset.UtcNow);
    }

    public void UpdateMetadata(int? runtimeMinutes, bool hasGenreIds, bool hasChanged)
    {
        if (runtimeMinutes is <= 0 or > 1440)
        {
            runtimeMinutes = null;
        }

        if (runtimeMinutes is not null)
        {
            RuntimeMinutes = runtimeMinutes;
        }

        if (hasChanged && (runtimeMinutes is not null || hasGenreIds))
        {
            MetadataUpdatedAt = DateTimeOffset.UtcNow;
            MetadataVersion++;
        }
    }
}

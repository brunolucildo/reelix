using System.Net;

namespace Reelix.Application.Catalog;

/// <summary>Short-lived cache of provider genre lists; the instance owns its state.</summary>
public interface ICatalogGenreCache
{
    bool TryGet(string key, out IReadOnlyList<CatalogGenre> genres);

    void Set(string key, IReadOnlyList<CatalogGenre> genres, TimeSpan timeToLive);
}

public sealed class CatalogGenreService(
    IEnumerable<ICatalogGenreProvider> providers,
    ICatalogGenreCache cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

    public Task<IReadOnlyList<CatalogGenre>> GetGenresAsync(
        CatalogProvider provider,
        CatalogContentType contentType,
        string language,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(language) || language.Length > 16)
        {
            throw new ArgumentException("Informe um código de idioma válido.", nameof(language));
        }

        var catalogProvider = providers.SingleOrDefault(candidate => candidate.Provider == provider)
            ?? throw new CatalogProviderException(
                "catalog_provider_not_supported",
                $"O provedor de catálogo {provider} não oferece lista de gêneros.",
                (int)HttpStatusCode.BadRequest);
        var key = $"catalog-genres:{provider}:{contentType}:{language}";
        if (cache.TryGet(key, out var cached))
        {
            return Task.FromResult(cached);
        }

        return FetchAndCacheAsync(
            key,
            catalogProvider,
            contentType,
            language,
            cancellationToken);
    }

    private async Task<IReadOnlyList<CatalogGenre>> FetchAndCacheAsync(
        string key,
        ICatalogGenreProvider provider,
        CatalogContentType contentType,
        string language,
        CancellationToken cancellationToken)
    {
        var genres = await provider.GetGenresAsync(contentType, language, cancellationToken);
        cache.Set(key, genres, CacheDuration);
        return genres;
    }
}

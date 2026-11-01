using System.Collections.Concurrent;
using System.Net;

namespace Reelix.Application.Catalog;

public sealed class CatalogGenreService(
    IEnumerable<ICatalogGenreProvider> providers)
{
    private static readonly ConcurrentDictionary<string, CachedGenres> Cache = new();

    public Task<IReadOnlyList<CatalogGenre>> GetGenresAsync(
        CatalogProvider provider,
        CatalogContentType contentType,
        string language,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(language) || language.Length > 16)
        {
            throw new ArgumentException("A valid language code is required.", nameof(language));
        }

        var catalogProvider = providers.SingleOrDefault(candidate => candidate.Provider == provider)
            ?? throw new CatalogProviderException(
                "catalog_provider_not_supported",
                $"The {provider} catalog provider does not supply genre lists.",
                (int)HttpStatusCode.BadRequest);
        var key = $"catalog-genres:{provider}:{contentType}:{language}";
        if (Cache.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return Task.FromResult(cached.Items);
        }

        return FetchAndCacheAsync(
            key,
            catalogProvider,
            contentType,
            language,
            cancellationToken);
    }

    private static async Task<IReadOnlyList<CatalogGenre>> FetchAndCacheAsync(
        string key,
        ICatalogGenreProvider provider,
        CatalogContentType contentType,
        string language,
        CancellationToken cancellationToken)
    {
        var genres = await provider.GetGenresAsync(contentType, language, cancellationToken);
        Cache[key] = new CachedGenres(genres, DateTimeOffset.UtcNow.AddHours(6));
        return genres;
    }

    private sealed record CachedGenres(IReadOnlyList<CatalogGenre> Items, DateTimeOffset ExpiresAt);
}
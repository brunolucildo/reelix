using System.Collections.Concurrent;
using Reelix.Application.Catalog;

namespace Reelix.Infrastructure.Catalog;

/// <summary>
/// Process-local genre cache. Entries expire by the injected clock, and every instance owns its
/// own dictionary, so two hosts never share state.
/// </summary>
public sealed class InMemoryCatalogGenreCache(TimeProvider clock) : ICatalogGenreCache
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    public bool TryGet(string key, out IReadOnlyList<CatalogGenre> genres)
    {
        if (_entries.TryGetValue(key, out var entry) && entry.ExpiresAt > clock.GetUtcNow())
        {
            genres = entry.Genres;
            return true;
        }

        genres = [];
        return false;
    }

    public void Set(string key, IReadOnlyList<CatalogGenre> genres, TimeSpan timeToLive) =>
        _entries[key] = new Entry(genres, clock.GetUtcNow().Add(timeToLive));

    private sealed record Entry(IReadOnlyList<CatalogGenre> Genres, DateTimeOffset ExpiresAt);
}

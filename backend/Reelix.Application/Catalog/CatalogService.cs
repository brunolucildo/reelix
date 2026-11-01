using System.Net;

namespace Reelix.Application.Catalog;

public sealed class CatalogService(
    IEnumerable<ICatalogProvider> providers,
    IExternalCatalogReferenceRepository references)
{
    public async Task<CatalogSearchPage> SearchAsync(
        CatalogProvider provider,
        string query,
        CatalogContentType? contentType,
        int page,
        string language,
        CancellationToken cancellationToken)
    {
        ValidateSearch(query, page, language);
        var result = await GetProvider(provider).SearchAsync(
            query.Trim(),
            contentType,
            page,
            language,
            cancellationToken);
        var ids = await ResolveIdsAsync(provider, result.Items, cancellationToken);

        return new CatalogSearchPage(
            result.Items.Select(item => ToTitle(provider, item, ids)).ToArray(),
            result.Page,
            result.TotalPages,
            result.TotalResults,
            provider);
    }

    public async Task<CatalogDiscoveryPage> DiscoverAsync(
        CatalogProvider provider,
        CatalogContentType contentType,
        CatalogDiscoveryCategory category,
        IReadOnlyCollection<int> genreIds,
        int page,
        string language,
        CancellationToken cancellationToken)
    {
        ValidateDiscoveryRequest(contentType, category, genreIds, page, language);
        var catalogProvider = GetProvider(provider);
        if (catalogProvider is not ICatalogDiscoveryProvider discoveryProvider)
        {
            throw new CatalogProviderException(
                "catalog_discovery_not_supported",
                "A descoberta não está disponível para este provedor de catálogo.",
                (int)HttpStatusCode.BadRequest);
        }

        var result = await discoveryProvider.DiscoverAsync(
            contentType,
            category,
            genreIds,
            page,
            language,
            cancellationToken);
        var ids = await ResolveIdsAsync(provider, result.Items, cancellationToken);

        return new CatalogDiscoveryPage(
            result.Items.Select(item => ToTitle(provider, item, ids)).ToArray(),
            result.Page,
            result.TotalPages,
            result.TotalResults,
            provider,
            contentType,
            category);
    }

    public async Task<CatalogTitle?> GetDetailsAsync(
        CatalogProvider provider,
        CatalogContentType contentType,
        string externalId,
        string language,
        CancellationToken cancellationToken)
    {
        ValidateExternalId(externalId);
        ValidateLanguage(language);

        var item = await GetProvider(provider).GetDetailsAsync(
            contentType,
            externalId.Trim(),
            language,
            cancellationToken);
        if (item is null)
        {
            return null;
        }

        var key = new CatalogReferenceKey(item.ExternalId, ToReferenceType(item.ContentType));
        var ids = await references.GetOrCreateIdsAsync(provider, [key], cancellationToken);
        return ToTitle(provider, item, ids);
    }

    public async Task<CatalogSeason?> GetSeasonAsync(
        CatalogProvider provider,
        string externalSeriesId,
        int seasonNumber,
        string language,
        CancellationToken cancellationToken)
    {
        ValidateExternalId(externalSeriesId);
        ValidateLanguage(language);
        if (seasonNumber < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seasonNumber), "O número da temporada não pode ser negativo.");
        }

        var result = await GetProvider(provider).GetSeasonAsync(
            externalSeriesId.Trim(),
            seasonNumber,
            language,
            cancellationToken);
        if (result is null)
        {
            return null;
        }

        var seriesKey = new CatalogReferenceKey(
            externalSeriesId.Trim(),
            CatalogReferenceType.Series);
        var seasonKey = new CatalogReferenceKey(
            $"{externalSeriesId.Trim()}:{result.SeasonNumber}",
            CatalogReferenceType.Season);
        var episodeKeys = result.Episodes
            .Select(episode => new CatalogReferenceKey(
                episode.ExternalId,
                CatalogReferenceType.Episode))
            .ToArray();
        var ids = await references.GetOrCreateIdsAsync(
            provider,
            [seriesKey, seasonKey, .. episodeKeys],
            cancellationToken);

        return new CatalogSeason(
            ids[seasonKey],
            ids[seriesKey],
            provider,
            externalSeriesId.Trim(),
            result.Title,
            result.SeasonNumber,
            result.ReleaseDate,
            result.Episodes.Select((episode, index) => new CatalogEpisode(
                ids[episodeKeys[index]],
                episode.ExternalId,
                episode.EpisodeNumber,
                episode.Title,
                episode.Overview,
                episode.ReleaseDate,
                episode.RuntimeMinutes)).ToArray());
    }

    private ICatalogProvider GetProvider(CatalogProvider provider) =>
        providers.SingleOrDefault(candidate => candidate.Provider == provider)
        ?? throw new CatalogProviderException(
            "catalog_provider_not_supported",
            $"O provedor de catálogo {provider} não está disponível.",
            (int)HttpStatusCode.BadRequest);

    private async Task<IReadOnlyDictionary<CatalogReferenceKey, Guid>> ResolveIdsAsync(
        CatalogProvider provider,
        IReadOnlyCollection<ProviderCatalogTitle> items,
        CancellationToken cancellationToken)
    {
        var keys = items
            .Select(item => new CatalogReferenceKey(
                item.ExternalId,
                ToReferenceType(item.ContentType)))
            .Distinct()
            .ToArray();
        return keys.Length == 0
            ? new Dictionary<CatalogReferenceKey, Guid>()
            : await references.GetOrCreateIdsAsync(provider, keys, cancellationToken);
    }

    private static CatalogTitle ToTitle(
        CatalogProvider provider,
        ProviderCatalogTitle item,
        IReadOnlyDictionary<CatalogReferenceKey, Guid> ids)
    {
        var key = new CatalogReferenceKey(item.ExternalId, ToReferenceType(item.ContentType));
        return new CatalogTitle(
            ids[key],
            provider,
            item.ExternalId,
            item.ContentType,
            item.Title,
            item.OriginalTitle,
            item.Overview,
            item.Genres,
            item.ReleaseDate,
            item.OriginalLanguage,
            item.RuntimeMinutes,
            item.PosterUrl,
            item.BackdropUrl);
    }

    private static CatalogReferenceType ToReferenceType(CatalogContentType contentType) =>
        contentType == CatalogContentType.Movie
            ? CatalogReferenceType.Movie
            : CatalogReferenceType.Series;

    private static void ValidateSearch(string query, int page, string language)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length > 200)
        {
            throw new ArgumentException("A busca deve ter entre 1 e 200 caracteres.", nameof(query));
        }

        if (page is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "A página deve estar entre 1 e 100.");
        }

        ValidateLanguage(language);
    }

    private static void ValidateDiscoveryRequest(
        CatalogContentType contentType,
        CatalogDiscoveryCategory category,
        IReadOnlyCollection<int> genreIds,
        int page,
        string language)
    {
        ArgumentNullException.ThrowIfNull(genreIds);
        if (!Enum.IsDefined(contentType) || !Enum.IsDefined(category))
        {
            throw new ArgumentException("Informe um tipo de conteúdo e uma seção válidos.");
        }

        if (page is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "A página deve estar entre 1 e 100.");
        }

        if (genreIds.Count > 20 || genreIds.Any(id => id <= 0) ||
            genreIds.Distinct().Count() != genreIds.Count)
        {
            throw new ArgumentException("Informe até 20 gêneros válidos e sem repetições.", nameof(genreIds));
        }

        ValidateLanguage(language);
    }

    private static void ValidateExternalId(string externalId)
    {
        if (string.IsNullOrWhiteSpace(externalId) || externalId.Length > 64)
        {
            throw new ArgumentException("Informe um identificador externo válido.", nameof(externalId));
        }
    }

    private static void ValidateLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language) || language.Length > 16)
        {
            throw new ArgumentException("Informe um código de idioma válido.", nameof(language));
        }
    }
}

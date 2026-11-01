namespace Reelix.Domain.Catalog;

public sealed class CatalogEpisodeSeries
{
    private CatalogEpisodeSeries()
    {
    }

    private CatalogEpisodeSeries(Guid episodeId, Guid seriesId)
    {
        EpisodeId = episodeId;
        SeriesId = seriesId;
    }

    public Guid EpisodeId { get; private set; }

    public Guid SeriesId { get; private set; }

    public static CatalogEpisodeSeries Create(Guid episodeId, Guid seriesId)
    {
        if (episodeId == Guid.Empty || seriesId == Guid.Empty || episodeId == seriesId)
        {
            throw new ArgumentException("Informe uma associação válida entre episódio e série.");
        }

        return new CatalogEpisodeSeries(episodeId, seriesId);
    }
}

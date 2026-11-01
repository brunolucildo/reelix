namespace Reelix.Domain.Catalog;

public sealed class CatalogReferenceGenre
{
    private CatalogReferenceGenre()
    {
    }

    private CatalogReferenceGenre(Guid catalogReferenceId, int genreId)
    {
        CatalogReferenceId = catalogReferenceId;
        GenreId = genreId;
    }

    public Guid CatalogReferenceId { get; private set; }

    public int GenreId { get; private set; }

    public static CatalogReferenceGenre Create(Guid catalogReferenceId, int genreId)
    {
        if (catalogReferenceId == Guid.Empty || genreId <= 0)
        {
            throw new ArgumentException("Informe uma referência e um gênero válidos.");
        }

        return new CatalogReferenceGenre(catalogReferenceId, genreId);
    }
}

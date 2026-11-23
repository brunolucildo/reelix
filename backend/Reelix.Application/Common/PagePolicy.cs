namespace Reelix.Application.Common;

/// <summary>
/// Named pagination limits. The limits and the error texts are part of the public contract of
/// each flow and are intentionally different (spec 0013 does not unify them); a new flow adds
/// its own named policy here.
/// </summary>
public sealed class PagePolicy
{
    private readonly int _maxPage;
    private readonly int? _maxPageSize;
    private readonly Func<PageViolation, Exception> _error;

    private PagePolicy(int maxPage, int? maxPageSize, Func<PageViolation, Exception> error)
    {
        _maxPage = maxPage;
        _maxPageSize = maxPageSize;
        _error = error;
    }

    /// <summary>Favorites, watchlist and custom lists: pages 1 to 1,000,000 and 1 to 100 titles.</summary>
    public static PagePolicy Lists { get; } = new(
        1_000_000,
        100,
        _ => new ArgumentException(
            "Informe uma página entre 1 e 1.000.000 e um limite entre 1 e 100 títulos."));

    /// <summary>Own ratings and comments: pages 1 to 100,000 and 1 to 50 items.</summary>
    public static PagePolicy Ratings { get; } = new(
        100_000,
        50,
        _ => new ArgumentException(
            "A página deve ser positiva e o tamanho deve estar entre 1 e 50."));

    /// <summary>Tracking history and series: pages 1 to 100,000 and 1 to 100 items, one error per parameter.</summary>
    public static PagePolicy Tracking { get; } = new(
        100_000,
        100,
        violation => violation == PageViolation.Page
            ? new ArgumentOutOfRangeException("page", "A página deve estar entre 1 e 100000.")
            : new ArgumentOutOfRangeException("pageSize", "O tamanho da página deve estar entre 1 e 100."));

    /// <summary>Catalog search and discovery: pages 1 to 100 (the provider fixes the page size).</summary>
    public static PagePolicy Catalog { get; } = new(
        100,
        null,
        _ => new ArgumentOutOfRangeException("page", "A página deve estar entre 1 e 100."));

    internal Exception? Check(int page, int? pageSize)
    {
        if (page < 1 || page > _maxPage)
        {
            return _error(PageViolation.Page);
        }

        if (_maxPageSize is { } maxPageSize &&
            (pageSize is null || pageSize < 1 || pageSize > maxPageSize))
        {
            return _error(PageViolation.PageSize);
        }

        return null;
    }
}

public enum PageViolation
{
    Page,
    PageSize,
}

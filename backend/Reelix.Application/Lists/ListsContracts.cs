namespace Reelix.Application.Lists;

public sealed record TitleListItem(
    Guid TitleId,
    string ContentType,
    string Provider,
    string ExternalId,
    DateTimeOffset? AddedAt,
    int? Position);

public sealed record PagedTitleList(
    IReadOnlyList<TitleListItem> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record TitleRelationStatus(
    Guid TitleId,
    bool IsFavorite,
    bool IsInWatchlist);

public sealed record CustomListSummary(
    Guid Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int TotalCount);

public sealed record PagedCustomLists(
    IReadOnlyList<CustomListSummary> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record CustomListData(
    CustomListSummary List,
    PagedTitleList Titles);

public sealed record CreateCustomList(string Name, string? Description);

public sealed record UpdateCustomList(string Name, string? Description);

public enum ListItemAddResult
{
    Added,
    AlreadyExists,
    ListNotFound,
}

public enum ListReorderResult
{
    Updated,
    ListNotFound,
    TitlesChanged,
}

public interface IUserListsRepository
{
    Task<IReadOnlyList<TitleRelationStatus>> GetTitleStatusesAsync(
        Guid accountId,
        IReadOnlyCollection<Guid> titleIds,
        CancellationToken cancellationToken);

    Task<bool> CatalogTitleExistsAsync(
        Guid titleId,
        string contentType,
        CancellationToken cancellationToken);

    Task<PagedTitleList> GetFavoritesAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> AddFavoriteAsync(
        Guid accountId,
        Guid titleId,
        string contentType,
        CancellationToken cancellationToken);

    Task RemoveFavoriteAsync(Guid accountId, Guid titleId, CancellationToken cancellationToken);

    Task<PagedTitleList> GetWatchlistAsync(
        Guid accountId,
        int page,
        int pageSize,
        bool oldestFirst,
        CancellationToken cancellationToken);

    Task<bool> AddWatchlistTitleAsync(
        Guid accountId,
        Guid titleId,
        string contentType,
        CancellationToken cancellationToken);

    Task RemoveWatchlistTitleAsync(Guid accountId, Guid titleId, CancellationToken cancellationToken);

    Task<PagedCustomLists> GetCustomListsAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<CustomListData?> GetCustomListAsync(
        Guid accountId,
        Guid listId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<CustomListSummary> CreateCustomListAsync(
        Guid accountId,
        CreateCustomList request,
        CancellationToken cancellationToken);

    Task<CustomListSummary?> UpdateCustomListAsync(
        Guid accountId,
        Guid listId,
        UpdateCustomList request,
        CancellationToken cancellationToken);

    Task<bool> DeleteCustomListAsync(
        Guid accountId,
        Guid listId,
        CancellationToken cancellationToken);

    Task<ListItemAddResult> AddCustomListTitleAsync(
        Guid accountId,
        Guid listId,
        Guid titleId,
        string contentType,
        CancellationToken cancellationToken);

    Task<bool> RemoveCustomListTitleAsync(
        Guid accountId,
        Guid listId,
        Guid titleId,
        CancellationToken cancellationToken);

    Task<ListReorderResult> ReorderCustomListTitlesAsync(
        Guid accountId,
        Guid listId,
        IReadOnlyList<Guid> titleIds,
        CancellationToken cancellationToken);
}

public sealed class CatalogTitleNotFoundException()
    : Exception("O título não está disponível no catálogo.");

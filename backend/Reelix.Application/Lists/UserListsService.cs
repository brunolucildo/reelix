using Reelix.Application.Authentication;
using Reelix.Application.Catalog;
using Reelix.Application.Users;

namespace Reelix.Application.Lists;

public sealed class UserListsService(
    IUserAccountRepository accounts,
    IUserPreferencesRepository preferences,
    IUserListsRepository lists)
{
    public async Task<IReadOnlyList<TitleRelationStatus>> GetTitleStatusesAsync(
        Guid supabaseUserId,
        IReadOnlyCollection<Guid> titleIds,
        CancellationToken cancellationToken)
    {
        if (titleIds is null || titleIds.Count > 50 ||
            titleIds.Any(titleId => titleId == Guid.Empty) ||
            titleIds.Distinct().Count() != titleIds.Count)
        {
            throw new ArgumentException("Informe até 50 títulos únicos e válidos.", nameof(titleIds));
        }

        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await lists.GetTitleStatusesAsync(account.Id, titleIds, cancellationToken);
    }

    public async Task<PagedTitleList> GetFavoritesAsync(
        Guid supabaseUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await lists.GetFavoritesAsync(
            account.Id,
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<bool> AddFavoriteAsync(
        Guid supabaseUserId,
        Guid titleId,
        CatalogContentType contentType,
        CancellationToken cancellationToken)
    {
        var contentTypeValue = ToContentTypeValue(contentType);
        await EnsureCatalogTitleExistsAsync(titleId, contentTypeValue, cancellationToken);
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        await preferences.GetOrCreateAsync(account.Id, cancellationToken);
        return await lists.AddFavoriteAsync(
            account.Id,
            titleId,
            contentTypeValue,
            cancellationToken);
    }

    public async Task RemoveFavoriteAsync(
        Guid supabaseUserId,
        Guid titleId,
        CancellationToken cancellationToken)
    {
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        await lists.RemoveFavoriteAsync(account.Id, titleId, cancellationToken);
    }

    public async Task<PagedTitleList> GetWatchlistAsync(
        Guid supabaseUserId,
        int page,
        int pageSize,
        bool oldestFirst,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await lists.GetWatchlistAsync(
            account.Id,
            page,
            pageSize,
            oldestFirst,
            cancellationToken);
    }

    public async Task<bool> AddWatchlistTitleAsync(
        Guid supabaseUserId,
        Guid titleId,
        CatalogContentType contentType,
        CancellationToken cancellationToken)
    {
        var contentTypeValue = ToContentTypeValue(contentType);
        await EnsureCatalogTitleExistsAsync(titleId, contentTypeValue, cancellationToken);
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await lists.AddWatchlistTitleAsync(
            account.Id,
            titleId,
            contentTypeValue,
            cancellationToken);
    }

    public async Task RemoveWatchlistTitleAsync(
        Guid supabaseUserId,
        Guid titleId,
        CancellationToken cancellationToken)
    {
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        await lists.RemoveWatchlistTitleAsync(account.Id, titleId, cancellationToken);
    }

    public async Task<PagedCustomLists> GetCustomListsAsync(
        Guid supabaseUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await lists.GetCustomListsAsync(account.Id, page, pageSize, cancellationToken);
    }

    public async Task<CustomListData?> GetCustomListAsync(
        Guid supabaseUserId,
        Guid listId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await lists.GetCustomListAsync(account.Id, listId, page, pageSize, cancellationToken);
    }

    public async Task<CustomListSummary> CreateCustomListAsync(
        Guid supabaseUserId,
        CreateCustomList request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentException("Informe os dados da lista.", nameof(request));
        }

        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await lists.CreateCustomListAsync(account.Id, request, cancellationToken);
    }

    public async Task<CustomListSummary?> UpdateCustomListAsync(
        Guid supabaseUserId,
        Guid listId,
        UpdateCustomList request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentException("Informe os dados da lista.", nameof(request));
        }

        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await lists.UpdateCustomListAsync(account.Id, listId, request, cancellationToken);
    }

    public async Task<bool> DeleteCustomListAsync(
        Guid supabaseUserId,
        Guid listId,
        CancellationToken cancellationToken)
    {
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await lists.DeleteCustomListAsync(account.Id, listId, cancellationToken);
    }

    public async Task<ListItemAddResult> AddCustomListTitleAsync(
        Guid supabaseUserId,
        Guid listId,
        Guid titleId,
        CatalogContentType contentType,
        CancellationToken cancellationToken)
    {
        var contentTypeValue = ToContentTypeValue(contentType);
        await EnsureCatalogTitleExistsAsync(titleId, contentTypeValue, cancellationToken);
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await lists.AddCustomListTitleAsync(
            account.Id,
            listId,
            titleId,
            contentTypeValue,
            cancellationToken);
    }

    public async Task<bool> RemoveCustomListTitleAsync(
        Guid supabaseUserId,
        Guid listId,
        Guid titleId,
        CancellationToken cancellationToken)
    {
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await lists.RemoveCustomListTitleAsync(account.Id, listId, titleId, cancellationToken);
    }

    public async Task<ListReorderResult> ReorderCustomListTitlesAsync(
        Guid supabaseUserId,
        Guid listId,
        IReadOnlyList<Guid> titleIds,
        CancellationToken cancellationToken)
    {
        if (titleIds is null || titleIds.Any(titleId => titleId == Guid.Empty) ||
            titleIds.Distinct().Count() != titleIds.Count)
        {
            throw new ArgumentException("A ordenação deve conter títulos únicos e válidos.", nameof(titleIds));
        }

        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await lists.ReorderCustomListTitlesAsync(
            account.Id,
            listId,
            titleIds,
            cancellationToken);
    }

    private async Task EnsureCatalogTitleExistsAsync(
        Guid titleId,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (titleId == Guid.Empty ||
            !await lists.CatalogTitleExistsAsync(titleId, contentType, cancellationToken))
        {
            throw new CatalogTitleNotFoundException();
        }
    }

    private static string ToContentTypeValue(CatalogContentType contentType) =>
        contentType switch
        {
            CatalogContentType.Movie => "movie",
            CatalogContentType.Series => "series",
            _ => throw new ArgumentException("Informe um tipo de conteúdo válido.", nameof(contentType)),
        };

    private static void ValidatePage(int page, int pageSize)
    {
        if (page is < 1 or > 1_000_000 || pageSize is < 1 or > 100)
        {
            throw new ArgumentException("Informe uma página entre 1 e 1.000.000 e um limite entre 1 e 100 títulos.");
        }
    }
}

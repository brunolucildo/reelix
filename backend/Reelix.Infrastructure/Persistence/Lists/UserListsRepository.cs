using Microsoft.EntityFrameworkCore;
using Npgsql;
using Reelix.Application.Lists;
using Reelix.Domain.Catalog;
using Reelix.Domain.Users;

namespace Reelix.Infrastructure.Persistence.Lists;

public sealed class UserListsRepository(ReelixDbContext dbContext) : IUserListsRepository
{
    public async Task<IReadOnlyList<TitleRelationStatus>> GetTitleStatusesAsync(
        Guid accountId,
        IReadOnlyCollection<Guid> titleIds,
        CancellationToken cancellationToken)
    {
        if (titleIds.Count == 0)
        {
            return [];
        }

        var favorites = await dbContext.UserFavoriteTitles
            .Where(item => item.AccountId == accountId && titleIds.Contains(item.TitleId))
            .Select(item => item.TitleId)
            .ToArrayAsync(cancellationToken);
        var watchlist = await dbContext.Set<UserWatchlistTitle>()
            .Where(item => item.AccountId == accountId && titleIds.Contains(item.TitleId))
            .Select(item => item.TitleId)
            .ToArrayAsync(cancellationToken);
        var favoriteIds = favorites.ToHashSet();
        var watchlistIds = watchlist.ToHashSet();

        return titleIds.Select(titleId => new TitleRelationStatus(
            titleId,
            favoriteIds.Contains(titleId),
            watchlistIds.Contains(titleId))).ToArray();
    }

    public Task<bool> CatalogTitleExistsAsync(
        Guid titleId,
        string contentType,
        CancellationToken cancellationToken) =>
        dbContext.ExternalCatalogReferences.AnyAsync(
            reference => reference.Id == titleId && reference.ContentType == contentType,
            cancellationToken);

    public async Task<PagedTitleList> GetFavoritesAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var favorites = dbContext.UserFavoriteTitles
            .Where(favorite => favorite.AccountId == accountId);
        var totalCount = await favorites.CountAsync(cancellationToken);
        var items = await (
                from favorite in favorites
                join reference in dbContext.ExternalCatalogReferences
                    on favorite.TitleId equals reference.Id
                orderby favorite.TitleId
                select new TitleListItem(
                    favorite.TitleId,
                    favorite.ContentType.ToLower(),
                    reference.Provider,
                    reference.ExternalId,
                    null,
                    null))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        return new PagedTitleList(items, page, pageSize, totalCount);
    }

    public async Task<bool> AddFavoriteAsync(
        Guid accountId,
        Guid titleId,
        string contentType,
        CancellationToken cancellationToken)
    {
        var favorite = UserFavoriteTitle.Create(accountId, titleId, contentType);
        dbContext.UserFavoriteTitles.Add(favorite);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            dbContext.Entry(favorite).State = EntityState.Detached;
            return false;
        }
        catch (DbUpdateException exception) when (IsForeignKeyViolation(exception))
        {
            dbContext.Entry(favorite).State = EntityState.Detached;
            if (!await CatalogTitleExistsAsync(titleId, contentType, cancellationToken))
            {
                throw new CatalogTitleNotFoundException();
            }

            throw;
        }
    }

    public async Task RemoveFavoriteAsync(
        Guid accountId,
        Guid titleId,
        CancellationToken cancellationToken)
    {
        await dbContext.UserFavoriteTitles
            .Where(favorite => favorite.AccountId == accountId && favorite.TitleId == titleId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<PagedTitleList> GetWatchlistAsync(
        Guid accountId,
        int page,
        int pageSize,
        bool oldestFirst,
        CancellationToken cancellationToken)
    {
        var watchlist = dbContext.Set<UserWatchlistTitle>()
            .Where(title => title.AccountId == accountId);
        var totalCount = await watchlist.CountAsync(cancellationToken);
        var ordered = oldestFirst
            ? watchlist.OrderBy(title => title.AddedAt).ThenBy(title => title.TitleId)
            : watchlist.OrderByDescending(title => title.AddedAt).ThenBy(title => title.TitleId);
        var items = await (
                from title in ordered
                join reference in dbContext.ExternalCatalogReferences
                    on title.TitleId equals reference.Id
                select new TitleListItem(
                    title.TitleId,
                    title.ContentType,
                    reference.Provider,
                    reference.ExternalId,
                    title.AddedAt,
                    null))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        return new PagedTitleList(items, page, pageSize, totalCount);
    }

    public async Task<bool> AddWatchlistTitleAsync(
        Guid accountId,
        Guid titleId,
        string contentType,
        CancellationToken cancellationToken)
    {
        var item = UserWatchlistTitle.Create(accountId, titleId, contentType);
        dbContext.Set<UserWatchlistTitle>().Add(item);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            dbContext.Entry(item).State = EntityState.Detached;
            return false;
        }
        catch (DbUpdateException exception) when (IsForeignKeyViolation(exception))
        {
            dbContext.Entry(item).State = EntityState.Detached;
            if (!await CatalogTitleExistsAsync(titleId, contentType, cancellationToken))
            {
                throw new CatalogTitleNotFoundException();
            }

            throw;
        }
    }

    public async Task RemoveWatchlistTitleAsync(
        Guid accountId,
        Guid titleId,
        CancellationToken cancellationToken)
    {
        await dbContext.Set<UserWatchlistTitle>()
            .Where(item => item.AccountId == accountId && item.TitleId == titleId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<PagedCustomLists> GetCustomListsAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var lists = dbContext.Set<UserCustomList>()
            .Where(list => list.AccountId == accountId)
            .OrderByDescending(list => list.UpdatedAt)
            .ThenBy(list => list.Id);
        var totalCount = await lists.CountAsync(cancellationToken);
        var items = await lists
            .Select(list => new CustomListSummary(
                list.Id,
                list.Name,
                list.Description,
                list.CreatedAt,
                list.UpdatedAt,
                dbContext.Set<UserCustomListTitle>().Count(title => title.ListId == list.Id)))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        return new PagedCustomLists(items, page, pageSize, totalCount);
    }

    public async Task<CustomListData?> GetCustomListAsync(
        Guid accountId,
        Guid listId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var list = await dbContext.Set<UserCustomList>()
            .Where(candidate => candidate.AccountId == accountId && candidate.Id == listId)
            .Select(candidate => new CustomListSummary(
                candidate.Id,
                candidate.Name,
                candidate.Description,
                candidate.CreatedAt,
                candidate.UpdatedAt,
                dbContext.Set<UserCustomListTitle>().Count(title => title.ListId == candidate.Id)))
            .SingleOrDefaultAsync(cancellationToken);
        if (list is null)
        {
            return null;
        }

        var titles = dbContext.Set<UserCustomListTitle>()
            .Where(title => title.ListId == listId);
        var totalCount = await titles.CountAsync(cancellationToken);
        var items = await (
                from title in titles
                join reference in dbContext.ExternalCatalogReferences
                    on title.TitleId equals reference.Id
                orderby title.Position, title.AddedAt, title.TitleId
                select new TitleListItem(
                    title.TitleId,
                    title.ContentType,
                    reference.Provider,
                    reference.ExternalId,
                    title.AddedAt,
                    title.Position))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        return new CustomListData(list, new PagedTitleList(items, page, pageSize, totalCount));
    }

    public async Task<CustomListSummary> CreateCustomListAsync(
        Guid accountId,
        CreateCustomList request,
        CancellationToken cancellationToken)
    {
        var list = UserCustomList.Create(accountId, request.Name, request.Description);
        dbContext.Set<UserCustomList>().Add(list);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSummary(list, 0);
    }

    public async Task<CustomListSummary?> UpdateCustomListAsync(
        Guid accountId,
        Guid listId,
        UpdateCustomList request,
        CancellationToken cancellationToken)
    {
        var list = await dbContext.Set<UserCustomList>()
            .SingleOrDefaultAsync(
                candidate => candidate.AccountId == accountId && candidate.Id == listId,
                cancellationToken);
        if (list is null)
        {
            return null;
        }

        list.Update(request.Name, request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (!await dbContext.Set<UserCustomList>()
                .AnyAsync(candidate => candidate.AccountId == accountId && candidate.Id == listId, cancellationToken))
        {
            return null;
        }

        var count = await dbContext.Set<UserCustomListTitle>()
            .CountAsync(title => title.ListId == listId, cancellationToken);
        return ToSummary(list, count);
    }

    public async Task<bool> DeleteCustomListAsync(
        Guid accountId,
        Guid listId,
        CancellationToken cancellationToken)
    {
        var deletedCount = await dbContext.Set<UserCustomList>()
            .Where(list => list.AccountId == accountId && list.Id == listId)
            .ExecuteDeleteAsync(cancellationToken);
        return deletedCount != 0;
    }

    public async Task<ListItemAddResult> AddCustomListTitleAsync(
        Guid accountId,
        Guid listId,
        Guid titleId,
        string contentType,
        CancellationToken cancellationToken)
    {
        var listExists = await dbContext.Set<UserCustomList>()
            .AnyAsync(list => list.AccountId == accountId && list.Id == listId, cancellationToken);
        if (!listExists)
        {
            return ListItemAddResult.ListNotFound;
        }

        var existing = await dbContext.Set<UserCustomListTitle>()
            .AnyAsync(item => item.ListId == listId && item.TitleId == titleId, cancellationToken);
        if (existing)
        {
            return ListItemAddResult.AlreadyExists;
        }

        var position = await dbContext.Set<UserCustomListTitle>()
            .Where(item => item.ListId == listId)
            .Select(item => (int?)item.Position)
            .MaxAsync(cancellationToken) ?? -1;
        var item = UserCustomListTitle.Create(listId, titleId, contentType, position + 1);
        dbContext.Set<UserCustomListTitle>().Add(item);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return ListItemAddResult.Added;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            dbContext.Entry(item).State = EntityState.Detached;
            return ListItemAddResult.AlreadyExists;
        }
        catch (DbUpdateException exception) when (IsForeignKeyViolation(exception))
        {
            dbContext.Entry(item).State = EntityState.Detached;
            if (!await CatalogTitleExistsAsync(titleId, contentType, cancellationToken))
            {
                throw new CatalogTitleNotFoundException();
            }

            return ListItemAddResult.ListNotFound;
        }
    }

    public async Task<bool> RemoveCustomListTitleAsync(
        Guid accountId,
        Guid listId,
        Guid titleId,
        CancellationToken cancellationToken)
    {
        var listExists = await dbContext.Set<UserCustomList>()
            .AnyAsync(list => list.AccountId == accountId && list.Id == listId, cancellationToken);
        if (!listExists)
        {
            return false;
        }

        await dbContext.Set<UserCustomListTitle>()
            .Where(item => item.ListId == listId && item.TitleId == titleId)
            .ExecuteDeleteAsync(cancellationToken);
        return true;
    }

    public async Task<ListReorderResult> ReorderCustomListTitlesAsync(
        Guid accountId,
        Guid listId,
        IReadOnlyList<Guid> titleIds,
        CancellationToken cancellationToken)
    {
        var listExists = await dbContext.Set<UserCustomList>()
            .AnyAsync(list => list.AccountId == accountId && list.Id == listId, cancellationToken);
        if (!listExists)
        {
            return ListReorderResult.ListNotFound;
        }

        var items = await dbContext.Set<UserCustomListTitle>()
            .Where(item => item.ListId == listId)
            .ToArrayAsync(cancellationToken);
        if (items.Length != titleIds.Count ||
            !items.Select(item => item.TitleId).ToHashSet().SetEquals(titleIds))
        {
            return ListReorderResult.TitlesChanged;
        }

        var byId = items.ToDictionary(item => item.TitleId);
        for (var position = 0; position < titleIds.Count; position++)
        {
            byId[titleIds[position]].MoveTo(position);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await dbContext.Set<UserCustomList>()
                .AnyAsync(list => list.AccountId == accountId && list.Id == listId, cancellationToken)
            ? ListReorderResult.Updated
            : ListReorderResult.ListNotFound;
    }

    private static CustomListSummary ToSummary(UserCustomList list, int totalCount) =>
        new(list.Id, list.Name, list.Description, list.CreatedAt, list.UpdatedAt, totalCount);

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static bool IsForeignKeyViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };
}

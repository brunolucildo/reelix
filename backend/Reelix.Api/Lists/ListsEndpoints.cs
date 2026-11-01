using Microsoft.AspNetCore.Mvc;
using Reelix.Application.Catalog;
using Reelix.Application.Lists;

namespace Reelix.Api.Lists;

public static class ListsEndpoints
{
    public static RouteGroupBuilder MapListsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1")
            .WithTags("Favorites and Lists")
            .RequireAuthorization();

        group.MapGet("/favorites", GetFavorites)
            .WithName("GetFavorites")
            .WithSummary("Get the authenticated user's favorites.")
            .Produces<PagedTitleList>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPost("/title-status", GetTitleStatuses)
            .WithName("GetTitleRelationStatuses")
            .WithSummary("Get favorite and watchlist state for catalog titles.")
            .Produces<IReadOnlyList<TitleRelationStatus>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPut("/favorites/{titleId:guid}", AddFavorite)
            .WithName("AddFavorite")
            .WithSummary("Add a catalog title to favorites.")
            .Produces<MutationResult>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete("/favorites/{titleId:guid}", RemoveFavorite)
            .WithName("RemoveFavorite")
            .WithSummary("Remove a title from favorites.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/watchlist", GetWatchlist)
            .WithName("GetWatchlist")
            .WithSummary("Get the authenticated user's watchlist.")
            .Produces<PagedTitleList>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPut("/watchlist/{titleId:guid}", AddWatchlistTitle)
            .WithName("AddWatchlistTitle")
            .WithSummary("Add a catalog title to the watchlist.")
            .Produces<MutationResult>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete("/watchlist/{titleId:guid}", RemoveWatchlistTitle)
            .WithName("RemoveWatchlistTitle")
            .WithSummary("Remove a title from the watchlist.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/lists", GetLists)
            .WithName("GetCustomLists")
            .WithSummary("Get the authenticated user's private lists.")
            .Produces<PagedCustomLists>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPost("/lists", CreateList)
            .WithName("CreateCustomList")
            .WithSummary("Create a private custom list.")
            .Produces<CustomListSummary>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/lists/{listId:guid}", GetList)
            .WithName("GetCustomList")
            .WithSummary("Get one of the authenticated user's private lists.")
            .Produces<CustomListData>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut("/lists/{listId:guid}", UpdateList)
            .WithName("UpdateCustomList")
            .WithSummary("Update one of the authenticated user's private lists.")
            .Produces<CustomListSummary>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete("/lists/{listId:guid}", DeleteList)
            .WithName("DeleteCustomList")
            .WithSummary("Delete one of the authenticated user's private lists.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPut("/lists/{listId:guid}/titles/{titleId:guid}", AddListTitle)
            .WithName("AddCustomListTitle")
            .WithSummary("Add a catalog title to a private list.")
            .Produces<MutationResult>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete("/lists/{listId:guid}/titles/{titleId:guid}", RemoveListTitle)
            .WithName("RemoveCustomListTitle")
            .WithSummary("Remove a title from a private list.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut("/lists/{listId:guid}/titles/order", ReorderListTitles)
            .WithName("ReorderCustomListTitles")
            .WithSummary("Set the order of every title in a private list.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> GetFavorites(
        HttpContext context,
        UserListsService service,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(
            async () => Results.Ok(await service.GetFavoritesAsync(
                userId,
                page,
                pageSize,
                cancellationToken)));
    }

    private static async Task<IResult> GetTitleStatuses(
        HttpContext context,
        UserListsService service,
        TitleStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(async () => Results.Ok(
            await service.GetTitleStatusesAsync(userId, request.TitleIds, cancellationToken)));
    }

    private static async Task<IResult> AddFavorite(
        HttpContext context,
        UserListsService service,
        Guid titleId,
        TitleMutationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(async () => Results.Ok(new MutationResult(
            await service.AddFavoriteAsync(userId, titleId, request.ContentType, cancellationToken))));
    }

    private static async Task<IResult> RemoveFavorite(
        HttpContext context,
        UserListsService service,
        Guid titleId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        await service.RemoveFavoriteAsync(userId, titleId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetWatchlist(
        HttpContext context,
        UserListsService service,
        int page = 1,
        int pageSize = 20,
        bool oldestFirst = false,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(async () => Results.Ok(await service.GetWatchlistAsync(
            userId,
            page,
            pageSize,
            oldestFirst,
            cancellationToken)));
    }

    private static async Task<IResult> AddWatchlistTitle(
        HttpContext context,
        UserListsService service,
        Guid titleId,
        TitleMutationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(async () => Results.Ok(new MutationResult(
            await service.AddWatchlistTitleAsync(userId, titleId, request.ContentType, cancellationToken))));
    }

    private static async Task<IResult> RemoveWatchlistTitle(
        HttpContext context,
        UserListsService service,
        Guid titleId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        await service.RemoveWatchlistTitleAsync(userId, titleId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetLists(
        HttpContext context,
        UserListsService service,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(async () => Results.Ok(
            await service.GetCustomListsAsync(userId, page, pageSize, cancellationToken)));
    }

    private static async Task<IResult> CreateList(
        HttpContext context,
        UserListsService service,
        CreateCustomList request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(async () =>
        {
            var list = await service.CreateCustomListAsync(userId, request, cancellationToken);
            return Results.Created($"/api/v1/lists/{list.Id}", list);
        });
    }

    private static async Task<IResult> GetList(
        HttpContext context,
        UserListsService service,
        Guid listId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(async () =>
        {
            var list = await service.GetCustomListAsync(
                userId,
                listId,
                page,
                pageSize,
                cancellationToken);
            return list is null ? MissingList() : Results.Ok(list);
        });
    }

    private static async Task<IResult> UpdateList(
        HttpContext context,
        UserListsService service,
        Guid listId,
        UpdateCustomList request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(async () =>
        {
            var list = await service.UpdateCustomListAsync(userId, listId, request, cancellationToken);
            return list is null ? MissingList() : Results.Ok(list);
        });
    }

    private static async Task<IResult> DeleteList(
        HttpContext context,
        UserListsService service,
        Guid listId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        await service.DeleteCustomListAsync(userId, listId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> AddListTitle(
        HttpContext context,
        UserListsService service,
        Guid listId,
        Guid titleId,
        TitleMutationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(async () =>
        {
            var result = await service.AddCustomListTitleAsync(
                userId,
                listId,
                titleId,
                request.ContentType,
                cancellationToken);
            return result == ListItemAddResult.ListNotFound
                ? MissingList()
                : Results.Ok(new MutationResult(result == ListItemAddResult.Added));
        });
    }

    private static async Task<IResult> RemoveListTitle(
        HttpContext context,
        UserListsService service,
        Guid listId,
        Guid titleId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        var listExists = await service.RemoveCustomListTitleAsync(
            userId,
            listId,
            titleId,
            cancellationToken);
        return listExists ? Results.NoContent() : MissingList();
    }

    private static async Task<IResult> ReorderListTitles(
        HttpContext context,
        UserListsService service,
        Guid listId,
        ReorderTitlesRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(async () =>
        {
            var reordered = await service.ReorderCustomListTitlesAsync(
                userId,
                listId,
                request.TitleIds,
                cancellationToken);
            return reordered switch
            {
                ListReorderResult.Updated => Results.NoContent(),
                ListReorderResult.ListNotFound => MissingList(),
                _ => Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "A lista foi alterada.",
                    detail: "Os títulos da lista mudaram desde a última consulta. Atualize os dados e tente novamente."),
            };
        });
    }

    private static async Task<IResult> ExecuteAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (CatalogTitleNotFoundException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Título não encontrado.",
                detail: exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Dados da lista inválidos.",
                detail: exception.ToUserMessage());
        }
    }

    private static IResult MissingList() =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Lista não encontrada.",
            detail: "A lista não existe ou não está disponível para esta conta.");

    private static bool TryGetUserId(HttpContext context, out Guid userId) =>
        Guid.TryParse(context.User.FindFirst("sub")?.Value, out userId) && userId != Guid.Empty;
}

public sealed record TitleMutationRequest(CatalogContentType ContentType);

public sealed record ReorderTitlesRequest(IReadOnlyList<Guid> TitleIds);

public sealed record TitleStatusRequest(IReadOnlyList<Guid> TitleIds);

public sealed record MutationResult(bool Added);

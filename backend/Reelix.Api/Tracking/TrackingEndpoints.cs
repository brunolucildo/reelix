using Microsoft.AspNetCore.Mvc;
using Reelix.Application.Catalog;
using Reelix.Application.Tracking;
using Reelix.Domain.Tracking;

namespace Reelix.Api.Tracking;

public sealed record MarkMovieWatchedRequest(DateTimeOffset? WatchedAt);

public sealed record MarkEpisodeWatchedRequest(DateTimeOffset? WatchedAt);

public static class TrackingEndpoints
{
    public static RouteGroupBuilder MapTrackingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/tracking")
            .WithTags("Tracking")
            .RequireAuthorization();

        group.MapGet("/history", GetHistory)
            .WithName("GetWatchHistory")
            .WithSummary("Get the authenticated user's paginated watch history.")
            .Produces<TrackingPage<TrackingHistoryEntry>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPut("/movies/{titleId:guid}", MarkMovie)
            .WithName("MarkMovieWatched")
            .WithSummary("Mark a movie as watched or correct its watch date.")
            .Produces<TrackingHistoryEntry>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapGet("/movies/{titleId:guid}", GetMovieStatus)
            .WithName("GetMovieTrackingStatus")
            .WithSummary("Get whether a movie is in the authenticated user's watch history.")
            .Produces<bool>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete("/movies/{titleId:guid}", RemoveMovie)
            .WithName("RemoveWatchedMovie")
            .WithSummary("Remove a movie from the authenticated user's watch history.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPut("/series/{seriesId:guid}/episodes/{episodeId:guid}", MarkEpisode)
            .WithName("MarkEpisodeWatched")
            .WithSummary("Mark an episode as watched or correct its watch date.")
            .Produces<TrackingHistoryEntry>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapDelete("/series/{seriesId:guid}/episodes/{episodeId:guid}", RemoveEpisode)
            .WithName("RemoveWatchedEpisode")
            .WithSummary("Remove an episode from the authenticated user's watch history.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/series", GetSeries)
            .WithName("GetTrackedSeries")
            .WithSummary("Get series with watched episodes and their watched counts.")
            .Produces<TrackingPage<TrackedSeries>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/series/{seriesId:guid}/progress", GetSeriesProgress)
            .WithName("GetSeriesProgress")
            .WithSummary("Get released episode progress across all seasons, including specials.")
            .Produces<SeriesProgress>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout);
        group.MapGet(
                "/series/{seriesId:guid}/seasons/{seasonNumber:int}/progress",
                GetSeasonProgress)
            .WithName("GetSeasonProgress")
            .WithSummary("Get catalog-backed progress for one season.")
            .Produces<SeasonProgress>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout);
        group.MapGet(
                "/series/{seriesId:guid}/seasons/{seasonNumber:int}/episodes/pending",
                GetPendingEpisodes)
            .WithName("GetPendingEpisodes")
            .WithSummary("Get unwatched available episodes from one catalog season.")
            .Produces<IReadOnlyList<PendingEpisode>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout);

        return group;
    }

    private static async Task<IResult> GetHistory(
        HttpContext context,
        TrackingService tracking,
        int page = 1,
        int pageSize = 20,
        string? type = null,
        string sort = "newest")
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        if (!TryParseItemType(type, out var itemType) ||
            !TryParseOrder(sort, out var order))
        {
            return InvalidRequest();
        }

        return await ExecuteAsync(async () => Results.Ok(await tracking.GetHistoryAsync(
            userId,
            itemType,
            page,
            pageSize,
            order,
            context.RequestAborted)));
    }

    private static async Task<IResult> MarkMovie(
        HttpContext context,
        TrackingService tracking,
        Guid titleId,
        MarkMovieWatchedRequest request)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(async () => Results.Ok(await tracking.MarkMovieAsync(
            userId,
            titleId,
            request.WatchedAt,
            context.RequestAborted)));
    }

    private static async Task<IResult> GetMovieStatus(
        HttpContext context,
        TrackingService tracking,
        Guid titleId)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(async () => Results.Ok(await tracking.IsMovieWatchedAsync(
            userId,
            titleId,
            context.RequestAborted)));
    }

    private static async Task<IResult> RemoveMovie(
        HttpContext context,
        TrackingService tracking,
        Guid titleId)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(async () =>
        {
            await tracking.RemoveMovieAsync(userId, titleId, context.RequestAborted);
            return Results.NoContent();
        });
    }

    private static async Task<IResult> MarkEpisode(
        HttpContext context,
        TrackingService tracking,
        Guid seriesId,
        Guid episodeId,
        MarkEpisodeWatchedRequest request)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(async () => Results.Ok(await tracking.MarkEpisodeAsync(
            userId,
            seriesId,
            episodeId,
            request.WatchedAt,
            context.RequestAborted)));
    }

    private static async Task<IResult> RemoveEpisode(
        HttpContext context,
        TrackingService tracking,
        Guid seriesId,
        Guid episodeId)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(async () =>
        {
            await tracking.RemoveEpisodeAsync(
                userId,
                seriesId,
                episodeId,
                context.RequestAborted);
            return Results.NoContent();
        });
    }

    private static async Task<IResult> GetSeries(
        HttpContext context,
        TrackingService tracking,
        int page = 1,
        int pageSize = 20)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await ExecuteAsync(async () => Results.Ok(await tracking.GetSeriesAsync(
            userId,
            page,
            pageSize,
            context.RequestAborted)));
    }

    private static async Task<IResult> GetSeriesProgress(
        HttpContext context,
        TrackingService tracking,
        Guid seriesId,
        string provider = "tmdb")
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        if (!TryParseProvider(provider, out var parsedProvider))
        {
            return InvalidRequest();
        }

        return await ExecuteAsync(async () => Results.Ok(await tracking.GetSeriesProgressAsync(
            userId,
            seriesId,
            context.RequestAborted,
            parsedProvider)));
    }

    private static async Task<IResult> GetSeasonProgress(
        HttpContext context,
        TrackingService tracking,
        Guid seriesId,
        int seasonNumber,
        string provider = "tmdb")
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        if (!TryParseProvider(provider, out var parsedProvider))
        {
            return InvalidRequest();
        }

        return await ExecuteAsync(async () => Results.Ok(await tracking.GetSeasonProgressAsync(
            userId,
            seriesId,
            seasonNumber,
            parsedProvider,
            context.RequestAborted)));
    }

    private static async Task<IResult> GetPendingEpisodes(
        HttpContext context,
        TrackingService tracking,
        Guid seriesId,
        int seasonNumber,
        string provider = "tmdb")
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        if (!TryParseProvider(provider, out var parsedProvider))
        {
            return InvalidRequest();
        }

        return await ExecuteAsync(async () => Results.Ok(await tracking.GetPendingEpisodesAsync(
            userId,
            seriesId,
            seasonNumber,
            parsedProvider,
            context.RequestAborted)));
    }

    private static async Task<IResult> ExecuteAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (TrackingContentNotFoundException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Conteúdo não encontrado.",
                detail: "O conteúdo não está disponível no catálogo.");
        }
        catch (TrackingSeriesMismatchException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Episódio incompatível.",
                detail: "O episódio não pertence à série informada.");
        }
        catch (CatalogProviderException exception)
        {
            return Results.Problem(
                statusCode: exception.StatusCode,
                title: "Não foi possível consultar o catálogo.",
                detail: "Tente novamente mais tarde.");
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitação de acompanhamento inválida.",
                detail: exception.ToUserMessage());
        }
    }

    private static bool TryGetUserId(HttpContext context, out Guid userId) =>
        Guid.TryParse(context.User.FindFirst("sub")?.Value, out userId) && userId != Guid.Empty;

    private static bool TryParseItemType(
        string? value,
        out TrackingItemType? type)
    {
        if (value is null)
        {
            type = null;
            return true;
        }

        if (Enum.TryParse(value, ignoreCase: true, out TrackingItemType parsed) &&
            Enum.IsDefined(parsed))
        {
            type = parsed;
            return true;
        }

        type = null;
        return false;
    }

    private static bool TryParseOrder(string value, out TrackingHistoryOrder order)
    {
        if (string.Equals(value, "oldest", StringComparison.OrdinalIgnoreCase))
        {
            order = TrackingHistoryOrder.OldestFirst;
            return true;
        }

        if (string.Equals(value, "newest", StringComparison.OrdinalIgnoreCase))
        {
            order = TrackingHistoryOrder.NewestFirst;
            return true;
        }

        order = default;
        return false;
    }

    private static bool TryParseProvider(string value, out CatalogProvider provider)
    {
        if (Enum.TryParse(value, ignoreCase: true, out provider) &&
            Enum.IsDefined(provider))
        {
            return true;
        }

        provider = default;
        return false;
    }

    private static IResult InvalidRequest() =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Parâmetro inválido.",
            detail: "Confira os parâmetros e tente novamente.");
}

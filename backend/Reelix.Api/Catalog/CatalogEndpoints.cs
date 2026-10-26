using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Reelix.Application.Catalog;

namespace Reelix.Api.Catalog;

public static class CatalogEndpoints
{
    public static RouteGroupBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/catalog")
            .WithTags("Catalog")
            .RequireAuthorization();

        group.MapGet("/search", Search)
            .WithName("SearchCatalog")
            .WithSummary("Search movies and series in a catalog provider.")
            .WithDescription("Use provider 'tmdb' or 'omdb'. Omit type to search movies and series.")
            .Produces<CatalogSearchPage>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout);

        group.MapGet("/{provider}/{contentType}/{externalId}", GetDetails)
            .WithName("GetCatalogTitle")
            .WithSummary("Get normalized details for a movie or series.")
            .WithDescription("Use contentType 'movie' or 'series'. External IDs are provider-specific.")
            .Produces<CatalogTitle>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout);

        group.MapGet("/{provider}/series/{externalSeriesId}/seasons/{seasonNumber:int}", GetSeason)
            .WithName("GetCatalogSeason")
            .WithSummary("Get a series season and its available episodes.")
            .Produces<CatalogSeason>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout);

        return group;
    }

    private static async Task<IResult> Search(
        HttpContext context,
        CatalogService catalog,
        string query,
        string? type = null,
        string provider = "tmdb",
        int page = 1,
        string language = "pt-BR")
    {
        if (!TryParseProvider(provider, out var parsedProvider) ||
            (type is not null && !TryParseContentType(type, out _)))
        {
            return InvalidParameter();
        }

        CatalogContentType? contentType = type is null
            ? null
            : Enum.Parse<CatalogContentType>(type, ignoreCase: true);

        return await ExecuteAsync(
            context,
            async () => Results.Ok(await catalog.SearchAsync(
                    parsedProvider,
                    query,
                    contentType,
                    page,
                    language,
                    context.RequestAborted)));
    }

    private static async Task<IResult> GetDetails(
        HttpContext context,
        CatalogService catalog,
        string provider,
        string contentType,
        string externalId,
        string language = "pt-BR")
    {
        if (!TryParseProvider(provider, out var parsedProvider) ||
            !TryParseContentType(contentType, out var parsedType))
        {
            return InvalidParameter();
        }

        return await ExecuteAsync(
            context,
            async () =>
            {
                var title = await catalog.GetDetailsAsync(
                    parsedProvider,
                    parsedType,
                    externalId,
                    language,
                    context.RequestAborted);
                return title is null ? Results.NotFound() : Results.Ok(title);
            });
    }

    private static async Task<IResult> GetSeason(
        HttpContext context,
        CatalogService catalog,
        string provider,
        string externalSeriesId,
        int seasonNumber,
        string language = "pt-BR")
    {
        if (!TryParseProvider(provider, out var parsedProvider))
        {
            return InvalidParameter();
        }

        return await ExecuteAsync(
            context,
            async () =>
            {
                var season = await catalog.GetSeasonAsync(
                    parsedProvider,
                    externalSeriesId,
                    seasonNumber,
                    language,
                    context.RequestAborted);
                return season is null ? Results.NotFound() : Results.Ok(season);
            });
    }

    private static async Task<IResult> ExecuteAsync(
        HttpContext context,
        Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (CatalogProviderException exception)
        {
            if (exception.RetryAfter is { } retryAfter && retryAfter > TimeSpan.Zero)
            {
                context.Response.Headers.RetryAfter =
                    Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
            }

            var problem = new ProblemDetails
            {
                Status = exception.StatusCode,
                Title = exception.Message,
                Type = $"https://reelix.local/problems/{exception.Code}",
            };
            problem.Extensions["code"] = exception.Code;
            return Results.Problem(problem);
        }
        catch (ArgumentException exception)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid catalog request.",
                Detail = exception.Message,
            };
            return Results.Problem(problem);
        }
    }

    private static bool TryParseProvider(string value, out CatalogProvider provider) =>
        Enum.TryParse(value, ignoreCase: true, out provider) &&
        Enum.IsDefined(provider);

    private static bool TryParseContentType(string value, out CatalogContentType contentType) =>
        Enum.TryParse(value, ignoreCase: true, out contentType) &&
        Enum.IsDefined(contentType);

    private static IResult InvalidParameter() =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid provider or content type.",
            detail: "Use provider 'tmdb' or 'omdb' and content type 'movie' or 'series'.");
}

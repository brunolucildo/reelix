using Microsoft.AspNetCore.Mvc;
using Reelix.Application.Catalog;
using Reelix.Application.Users;

namespace Reelix.Api.Users;

public static class OnboardingEndpoints
{
    public static RouteGroupBuilder MapOnboardingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/onboarding")
            .WithTags("Onboarding")
            .RequireAuthorization();

        group.MapGet("", Get)
            .WithName("GetOnboardingPreferences")
            .Produces<OnboardingPreferences>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPut("/progress", SaveProgress)
            .WithName("SaveOnboardingProgress")
            .Produces<OnboardingPreferences>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        group.MapPost("/complete", Complete)
            .WithName("CompleteOnboarding")
            .Produces<OnboardingPreferences>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> Get(
        HttpContext context,
        OnboardingService onboarding,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(await onboarding.GetAsync(userId, cancellationToken));
    }

    private static async Task<IResult> SaveProgress(
        HttpContext context,
        OnboardingService onboarding,
        SaveOnboardingProgress progress,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            return Results.Ok(await onboarding.SaveProgressAsync(userId, progress, cancellationToken));
        }
        catch (CatalogProviderException exception)
        {
            return Results.Problem(
                statusCode: exception.StatusCode,
                title: exception.Message,
                type: $"https://reelix.local/problems/{exception.Code}");
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid onboarding preferences.",
                detail: exception.Message);
        }
    }

    private static async Task<IResult> Complete(
        HttpContext context,
        OnboardingService onboarding,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(await onboarding.CompleteAsync(userId, cancellationToken));
    }

    private static bool TryGetUserId(HttpContext context, out Guid userId) =>
        Guid.TryParse(context.User.FindFirst("sub")?.Value, out userId) && userId != Guid.Empty;
}
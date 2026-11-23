using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Reelix.Application.Errors;

namespace Reelix.Api.Errors;

/// <summary>
/// The single translation from an expected failure to an HTTP problem. Only the typed application
/// errors and <see cref="ArgumentException"/> are mapped; anything else is a defect and is left to
/// the exception handler of the host (500 without internals).
/// </summary>
public static class ApplicationProblemMapper
{
    public static bool CanMap(Exception exception) =>
        exception is ApplicationError or ArgumentException;

    public static IResult Map(
        Exception exception,
        ApplicationProblemOptions options,
        HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(response);

        return exception switch
        {
            ArgumentException argument => Problem(
                StatusCodes.Status400BadRequest,
                options.ValidationTitle,
                argument.ToUserMessage()),
            ValidationError validation => Problem(
                StatusCodes.Status400BadRequest,
                validation.Title ?? options.ValidationTitle,
                validation.Message),
            NotFoundError notFound => Problem(
                StatusCodes.Status404NotFound,
                notFound.Title ?? options.NotFoundTitle,
                options.NotFoundDetail ?? notFound.Message),
            ConflictError conflict => Problem(
                StatusCodes.Status409Conflict,
                conflict.Title ?? "Conflito.",
                conflict.Message),
            ServiceUnavailableError unavailable => Problem(
                StatusCodes.Status503ServiceUnavailable,
                unavailable.Title ?? "Serviço indisponível.",
                unavailable.Message),
            ExternalDependencyError dependency => MapDependency(dependency, options, response),
            UnauthenticatedError => Results.Unauthorized(),
            _ => throw new InvalidOperationException(
                $"The application error type {exception.GetType().Name} has no HTTP mapping."),
        };
    }

    private static IResult MapDependency(
        ExternalDependencyError error,
        ApplicationProblemOptions options,
        HttpResponse response)
    {
        var type = $"https://reelix.local/problems/{error.Code}";
        switch (options.Dependency)
        {
            case DependencyProblemStyle.Opaque:
                return Results.Problem(
                    statusCode: error.StatusCode,
                    title: options.OpaqueDependencyTitle,
                    detail: options.OpaqueDependencyDetail);
            case DependencyProblemStyle.TypeOnly:
                return Results.Problem(
                    statusCode: error.StatusCode,
                    title: error.Message,
                    type: type);
            default:
                if (error.RetryAfter is { } retryAfter && retryAfter > TimeSpan.Zero)
                {
                    response.Headers.RetryAfter =
                        Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                var problem = new ProblemDetails
                {
                    Status = error.StatusCode,
                    Title = error.Message,
                    Type = type,
                };
                problem.Extensions["code"] = error.Code;
                return Results.Problem(problem);
        }
    }

    private static IResult Problem(int statusCode, string title, string detail) =>
        Results.Problem(statusCode: statusCode, title: title, detail: detail);
}

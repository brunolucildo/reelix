using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Reelix.Api.Authentication;

namespace Reelix.Api.RateLimiting;

public static class RateLimitingExtensions
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The named policies applied with <c>.RequireRateLimiting("&lt;policy&gt;")</c>: permits per
    /// one-minute fixed window and caller. A new abusable write or search adds its row here.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, int> PermitsPerMinute =
        new Dictionary<string, int>
        {
            ["reviews-write"] = 10,
            ["social-search"] = 20,
            ["social-write"] = 8,
            ["notifications-write"] = 8,
        };

    public static IServiceCollection AddReelixRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            foreach (var (policyName, permitLimit) in PermitsPerMinute)
            {
                options.AddPolicy(policyName, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetPartitionKey(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = Window,
                            QueueLimit = 0,
                            AutoReplenishment = true,
                        }));
            }

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Limite de solicitações excedido.",
                        Detail = "Aguarde um minuto antes de tentar novamente.",
                    },
                    cancellationToken);
            };
        });

    // The caller (token subject), then the remote address, then a shared bucket.
    private static string GetPartitionKey(HttpContext context) =>
        context.User.GetSubject() ??
        context.Connection.RemoteIpAddress?.ToString() ??
        "unknown";
}

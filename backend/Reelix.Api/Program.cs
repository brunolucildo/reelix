using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Reelix.Application;
using Reelix.Application.Authentication;
using Reelix.Api.Catalog;
using Reelix.Api.Lists;
using Reelix.Api.Tracking;
using Reelix.Api.Ratings;
using Reelix.Api.Users;
using Reelix.Api.Social;
using Reelix.Api.Notifications;
using Reelix.Api.Gamification;
using Reelix.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Reelix API",
        Version = "v1",
        Description = "API do Reelix, incluindo autenticação, catálogo, onboarding e perfil.",
    });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Token JWT do usuário autenticado. Informe somente o token.",
    });
    options.AddSecurityRequirement(document =>
        new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = [],
        });
});
builder.Logging.AddFilter(
    "System.Net.Http.HttpClient.Reelix.Infrastructure.Catalog.OmdbCatalogProvider",
    LogLevel.Warning);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy("App", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

var supabaseUrl = builder.Configuration["Supabase:Url"]
    ?? throw new InvalidOperationException("Configure Supabase:Url before starting the API.");
if (!Uri.TryCreate(supabaseUrl, UriKind.Absolute, out var supabaseUri) ||
    (supabaseUri.Scheme != Uri.UriSchemeHttps &&
     !(builder.Environment.IsDevelopment() && supabaseUri.Scheme == Uri.UriSchemeHttp)))
{
    throw new InvalidOperationException("Supabase:Url must be an absolute HTTPS URL (HTTP is allowed only in Development).");
}

var issuer = $"{supabaseUrl.TrimEnd('/')}/auth/v1";
var audience = builder.Configuration["Supabase:Audience"] ?? "authenticated";
var jwtSecret = builder.Configuration["Supabase:JwtSecret"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = issuer;
        options.Audience = audience;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "sub",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidAlgorithms =
            [
                SecurityAlgorithms.EcdsaSha256,
                SecurityAlgorithms.RsaSha256,
                SecurityAlgorithms.HmacSha256,
            ],
        };

        if (!string.IsNullOrWhiteSpace(jwtSecret))
        {
            options.TokenValidationParameters.IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        }
    });

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("reviews-write", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst("sub")?.Value ??
                context.Connection.RemoteIpAddress?.ToString() ??
                "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
    options.AddPolicy("social-search", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst("sub")?.Value ??
                context.Connection.RemoteIpAddress?.ToString() ??
                "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
    options.AddPolicy("social-write", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst("sub")?.Value ??
                context.Connection.RemoteIpAddress?.ToString() ??
                "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
    options.AddPolicy("notifications-write", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst("sub")?.Value ??
                context.Connection.RemoteIpAddress?.ToString() ??
                "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Limite de solicitações excedido.",
                Detail = "Aguarde um minuto antes de tentar novamente.",
            },
            cancellationToken);
    };
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<CommentModerationWorker>();
builder.Services.AddHostedService<SocialNotificationWorker>();
builder.Services.AddHostedService<NotificationDeliveryWorker>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles();
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Reelix API v1");
    });
    app.MapGet("/api/v1/dev/auth/config", () => Results.Ok(new
    {
        supabaseUrl = supabaseUrl.TrimEnd('/'),
    }))
        .ExcludeFromDescription();
}

app.UseCors("App");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapGet("/api/v1/auth/me", async (
    HttpContext context,
    GetAuthenticatedAccount getAuthenticatedAccount,
    CancellationToken cancellationToken) =>
{
    var subject = context.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(subject, out var supabaseUserId) || supabaseUserId == Guid.Empty)
    {
        return Results.Unauthorized();
    }

    var email = context.User.FindFirst("email")?.Value;
    var account = await getAuthenticatedAccount.ExecuteAsync(
        supabaseUserId,
        email,
        cancellationToken);

    return Results.Ok(account);
})
    .RequireAuthorization()
    .WithName("GetAuthenticatedAccount")
    .WithTags("Authentication");

app.MapCatalogEndpoints();
app.MapOnboardingEndpoints();
app.MapProfileEndpoints();
app.MapTrackingEndpoints();
app.MapListsEndpoints();
app.MapRatingsCommentsEndpoints();
app.MapSocialEndpoints();
app.MapNotificationsEndpoints();
app.MapGamificationEndpoints();

app.Run();

public partial class Program
{
}

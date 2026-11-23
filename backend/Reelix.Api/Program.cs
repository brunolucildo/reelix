using System.Text.Json;
using System.Text.Json.Serialization;
using Reelix.Api.Authentication;
using Reelix.Api.Catalog;
using Reelix.Api.Gamification;
using Reelix.Api.Hosting;
using Reelix.Api.Lists;
using Reelix.Api.Notifications;
using Reelix.Api.Ratings;
using Reelix.Api.RateLimiting;
using Reelix.Api.Social;
using Reelix.Api.Tracking;
using Reelix.Api.Users;
using Reelix.Application;
using Reelix.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddReelixApiDocumentation();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
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

var supabaseUrl = builder.AddSupabaseAuthentication();
builder.Services.AddReelixRateLimiting();
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

app.MapAuthenticationEndpoints();
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

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Reelix.Application;
using Reelix.Application.Authentication;
using Reelix.Api.Catalog;
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
        Description = "API do Reelix, incluindo pesquisa e detalhes de catálogo.",
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
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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

app.Run();

public partial class Program
{
}

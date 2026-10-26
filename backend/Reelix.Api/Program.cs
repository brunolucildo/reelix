using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Reelix.Application;
using Reelix.Application.Authentication;
using Reelix.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

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
    app.MapOpenApi();
}

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

app.Run();

public partial class Program
{
}

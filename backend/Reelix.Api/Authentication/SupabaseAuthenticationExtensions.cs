using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Reelix.Api.Authentication;

public static class SupabaseAuthenticationExtensions
{
    /// <summary>
    /// Validates the Supabase JWT (signature, issuer, audience and lifetime) and registers the
    /// authorization services. Returns the configured Supabase URL.
    /// </summary>
    public static string AddSupabaseAuthentication(this WebApplicationBuilder builder)
    {
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
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();

        return supabaseUrl;
    }
}

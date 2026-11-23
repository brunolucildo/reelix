namespace Reelix.Api.Hosting;

public static class ApiDocumentationExtensions
{
    /// <summary>Registers OpenAPI and Swagger; the endpoints are only exposed in Development.</summary>
    public static IServiceCollection AddReelixApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddSwaggerGen(options =>
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
        return services;
    }
}

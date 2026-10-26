using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reelix.Application.Authentication;
using Reelix.Infrastructure.Persistence;
using Reelix.Infrastructure.Persistence.Users;

namespace Reelix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Reelix");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Configure ConnectionStrings:Reelix with the PostgreSQL connection string.");
        }

        services.AddDbContext<ReelixDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();

        return services;
    }
}

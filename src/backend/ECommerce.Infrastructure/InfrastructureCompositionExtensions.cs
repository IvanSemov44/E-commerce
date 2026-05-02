using ECommerce.Infrastructure.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure;

public static class InfrastructureCompositionExtensions
{
    public static IServiceCollection AddInfrastructurePersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var integrationConnectionString = configuration.GetConnectionString("IntegrationConnection")
            ?? throw new InvalidOperationException("Connection string 'IntegrationConnection' is not configured.");

        services.AddDbContext<IntegrationPersistenceDbContext>(options =>
        {
            options.UseNpgsql(integrationConnectionString);

            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        return services;
    }

    public static IServiceCollection AddInfrastructureSeeders(this IServiceCollection services)
    {
        // PR5 narrow slice: shared AppDb seeders are retired in favor of context-owned seed paths.
        return services;
    }
}

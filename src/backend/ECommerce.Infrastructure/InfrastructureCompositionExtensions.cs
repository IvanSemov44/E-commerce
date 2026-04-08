using ECommerce.Infrastructure.Data;
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
        var appDbConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var integrationConnectionString = configuration.GetConnectionString("IntegrationConnection")
            ?? throw new InvalidOperationException("Connection string 'IntegrationConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(appDbConnectionString, npgsqlOptions =>
            {
                // SplitQuery avoids cartesian explosion on multi-include read paths.
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddDbContext<IntegrationPersistenceDbContext>(options =>
        {
            options.UseNpgsql(integrationConnectionString);

            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IAppDbInitializationService, AppDbInitializationService>();

        return services;
    }

    public static IServiceCollection AddInfrastructureSeeders(this IServiceCollection services)
    {
        // PR5 narrow slice: shared AppDb seeders are retired in favor of context-owned seed paths.
        return services;
    }
}

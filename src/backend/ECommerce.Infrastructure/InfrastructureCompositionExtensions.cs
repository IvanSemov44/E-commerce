using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Data.Seeders;
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
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // SplitQuery avoids cartesian explosion on multi-include read paths.
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

            services.AddDbContext<IntegrationPersistenceDbContext>(options =>
            {
                options.UseNpgsql(connectionString);

                options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });

        return services;
    }

    public static IServiceCollection AddInfrastructureSeeders(this IServiceCollection services)
    {
        services.AddScoped<IUserSeeder, UserSeeder>();
        services.AddScoped<ICategorySeeder, CategorySeeder>();
        services.AddScoped<IProductSeeder, ProductSeeder>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}

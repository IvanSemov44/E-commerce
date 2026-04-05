using Microsoft.Extensions.DependencyInjection;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Catalog.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Catalog.Infrastructure;

public static class CatalogInfrastructureServiceExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<CatalogDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        return services;
    }
}

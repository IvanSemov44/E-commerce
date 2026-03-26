using Microsoft.Extensions.DependencyInjection;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Infrastructure.Repositories;

namespace ECommerce.Catalog.Infrastructure;

public static class CatalogInfrastructureServiceExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        return services;
    }
}

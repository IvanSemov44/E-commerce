using ECommerce.Infrastructure.Data;
using ECommerce.Promotions.Application;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Promotions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPromotionsInfrastructure(this IServiceCollection services)
    {
        // Register configuration assembly with AppDbContext
        AppDbContext.RegisterConfigurationAssembly(typeof(DependencyInjection).Assembly);

        // Register repositories
        services.AddScoped<IPromoCodeRepository, PromoCodeRepository>();

        // Register application layer
        services.AddPromotionsApplication();

        return services;
    }
}

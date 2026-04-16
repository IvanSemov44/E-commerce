using ECommerce.Promotions.Application;
using ECommerce.Promotions.Application.Interfaces;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Infrastructure.Persistence;
using ECommerce.Promotions.Infrastructure.Persistence.Repositories;
using ECommerce.Promotions.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Promotions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPromotionsInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<PromotionsDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("PromotionsConnection")
                ?? throw new InvalidOperationException("Connection string 'PromotionsConnection' is not configured.");

            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        // Register repositories
        services.AddScoped<IPromoCodeRepository, PromoCodeRepository>();
        services.AddScoped<IPromoProjectionEventPublisher, PromoProjectionEventPublisher>();

        // Register application layer
        services.AddPromotionsApplication();

        return services;
    }
}

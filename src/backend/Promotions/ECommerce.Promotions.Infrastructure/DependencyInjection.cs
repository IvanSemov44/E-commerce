using ECommerce.Infrastructure.Data;
using ECommerce.Promotions.Application;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Infrastructure.Persistence;
using ECommerce.Promotions.Infrastructure.Persistence.Repositories;
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
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        // Register configuration assembly with AppDbContext
        AppDbContext.RegisterConfigurationAssembly(typeof(DependencyInjection).Assembly);

        // Register repositories
        services.AddScoped<IPromoCodeRepository, PromoCodeRepository>();

        // Register application layer
        services.AddPromotionsApplication();

        return services;
    }
}

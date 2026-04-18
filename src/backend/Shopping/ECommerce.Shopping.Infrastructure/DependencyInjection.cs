using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Domain.Interfaces;
using ECommerce.Shopping.Infrastructure.Persistence.Repositories;
using ECommerce.Shopping.Infrastructure.Persistence;
using ECommerce.Shopping.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Shopping.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShoppingInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<ShoppingDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            string connectionString = configuration.GetConnectionString("ShoppingConnection")
                ?? throw new InvalidOperationException("Connection string 'ShoppingConnection' is not configured.");

            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IShoppingProductReader, ShoppingDbReader>();
        services.AddScoped<IStockAvailabilityReader, ShoppingDbReader>();

        return services;
    }
}

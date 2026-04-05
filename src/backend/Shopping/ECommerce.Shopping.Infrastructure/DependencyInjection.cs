using ECommerce.Infrastructure.Data;
using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Domain.Interfaces;
using ECommerce.Shopping.Infrastructure.Persistence.Repositories;
using ECommerce.Shopping.Infrastructure.Persistence;
using ECommerce.Shopping.Infrastructure.Services;
using MediatR;
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
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        // Skip configuration registration - AppDbContext already has Cart/Wishlist from Core.Entities
        // The new DDD aggregates use raw SQL queries in repositories

        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IShoppingDbReader, ShoppingDbReader>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(ECommerce.Shopping.Application.Commands.AddToCart.AddToCartCommand).Assembly));

        return services;
    }
}

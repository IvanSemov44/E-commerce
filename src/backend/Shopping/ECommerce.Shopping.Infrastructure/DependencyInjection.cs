using ECommerce.Infrastructure.Data;
using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Domain.Interfaces;
using ECommerce.Shopping.Infrastructure.Persistence.Repositories;
using ECommerce.Shopping.Infrastructure.Persistence.Configurations;
using ECommerce.Shopping.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Shopping.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShoppingInfrastructure(this IServiceCollection services)
    {
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
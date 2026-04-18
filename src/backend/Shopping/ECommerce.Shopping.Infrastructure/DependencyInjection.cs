using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Domain.Interfaces;
using ECommerce.Shopping.Domain.Events;
using ECommerce.Shopping.Infrastructure.EventHandlers;
using ECommerce.Shopping.Infrastructure.IntegrationEvents;
using ECommerce.Shopping.Infrastructure.Integration;
using ECommerce.Shopping.Infrastructure.Persistence.Repositories;
using ECommerce.Shopping.Infrastructure.Persistence;
using ECommerce.Shopping.Infrastructure.Services;
using ECommerce.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        services.AddOptions<ShoppingOutboxDispatcherOptions>();
        services.AddSingleton<IConfigureOptions<ShoppingOutboxDispatcherOptions>, ShoppingOutboxDispatcherOptionsSetup>();

        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IShoppingProductReader, ShoppingDbReader>();
        services.AddScoped<IStockAvailabilityReader, ShoppingDbReader>();
        services.AddScoped<IShoppingOutboxEventWriter, ShoppingOutboxEventWriter>();
        services.AddScoped<ICartIntegrationEventPublisher, CartIntegrationEventPublisher>();
        services.AddScoped<INotificationHandler<ItemAddedToCartEvent>, ItemAddedToCartEventHandler>();
        services.AddScoped<INotificationHandler<CartItemQuantityUpdatedEvent>, CartItemQuantityUpdatedEventHandler>();
        services.AddScoped<INotificationHandler<CartClearedEvent>, CartClearedEventHandler>();
        services.AddScoped<INotificationHandler<ProductProjectionUpdatedIntegrationEvent>, CatalogProductProjectionUpdatedIntegrationEventHandler>();
        services.AddScoped<INotificationHandler<InventoryStockProjectionUpdatedIntegrationEvent>, InventoryStockProjectionUpdatedIntegrationEventHandler>();

        // Shopping-owned outbox dispatcher — polls shopping.outbox_messages
        services.AddHostedService<ShoppingOutboxDispatcherHostedService>();

        return services;
    }
}

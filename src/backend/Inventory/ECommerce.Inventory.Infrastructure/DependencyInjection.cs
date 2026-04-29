using ECommerce.Contracts;
using ECommerce.Inventory.Application.Interfaces;
using ECommerce.Inventory.Domain.Events;
using ECommerce.Inventory.Domain.Interfaces;
using ECommerce.Inventory.Infrastructure.EventHandlers;
using ECommerce.Inventory.Infrastructure.Integration;
using ECommerce.Inventory.Infrastructure.Persistence;
using ECommerce.Inventory.Infrastructure.Persistence.Repositories;
using ECommerce.Inventory.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ECommerce.Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<InventoryDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("InventoryConnection")
                ?? throw new InvalidOperationException("Connection string 'InventoryConnection' is not configured.");

            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddOptions<InventoryOutboxDispatcherOptions>();
        services.AddSingleton<IConfigureOptions<InventoryOutboxDispatcherOptions>, InventoryOutboxDispatcherOptionsSetup>();

        services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IInventoryOutboxEventWriter, InventoryOutboxEventWriter>();
        services.AddScoped<IInventoryProjectionEventPublisher, InventoryProjectionEventPublisher>();
        services.AddScoped<IInventoryReservationEventPublisher, InventoryReservationEventPublisher>();

        services.AddScoped<INotificationHandler<StockReducedEvent>, StockReducedEventHandler>();
        services.AddScoped<INotificationHandler<StockReplenishedEvent>, StockReplenishedEventHandler>();
        services.AddScoped<INotificationHandler<LowStockDetectedEvent>, LowStockDetectedEventHandler>();
        services.AddScoped<INotificationHandler<OrderPlacedIntegrationEvent>, OrderPlacedIntegrationEventHandler>();

        services.AddHostedService<InventoryOutboxDispatcherHostedService>();

        return services;
    }
}

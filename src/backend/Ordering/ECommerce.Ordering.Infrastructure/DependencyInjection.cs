using ECommerce.Contracts;
using ECommerce.Ordering.Application;
using ECommerce.Ordering.Application.Interfaces;
using ECommerce.Ordering.Domain.Events;
using ECommerce.Ordering.Domain.Interfaces;
using ECommerce.Ordering.Infrastructure.EventHandlers;
using ECommerce.Ordering.Infrastructure.Integration;
using ECommerce.Ordering.Infrastructure.IntegrationEvents;
using ECommerce.Ordering.Infrastructure.Persistence;
using ECommerce.Ordering.Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ECommerce.Ordering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<SoftDeleteInterceptor>();

        services.AddDbContext<OrderingDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("OrderingConnection")
                ?? throw new InvalidOperationException("Connection string 'OrderingConnection' is not configured.");

            options.UseNpgsql(connectionString);
            options.AddInterceptors(serviceProvider.GetRequiredService<SoftDeleteInterceptor>());
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddOptions<OrderingOutboxDispatcherOptions>();
        services.AddSingleton<IConfigureOptions<OrderingOutboxDispatcherOptions>, OrderingOutboxDispatcherOptionsSetup>();
        services.AddOptions<OrderFulfillmentSagaOptions>();
        services.AddSingleton<IConfigureOptions<OrderFulfillmentSagaOptions>, OrderFulfillmentSagaOptionsSetup>();

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IDbReader, DbReader>();
        services.AddScoped<IProductCatalogReader, DbReader>();
        services.AddScoped<IPromoCodeLookup, DbReader>();
        services.AddScoped<IShippingAddressReader, DbReader>();
        services.AddScoped<IOrderingOutboxEventWriter, OrderingOutboxEventWriter>();

        services.AddScoped<IOrderFulfillmentSagaService, OrderFulfillmentSagaService>();
        services.AddScoped<IInboxProcessor, InboxIdempotencyProcessor>();
        services.AddScoped<InboxIdempotencyProcessor>();

        services.AddScoped<INotificationHandler<OrderPlacedEvent>, OrderPlacedEventHandler>();
        services.AddScoped<INotificationHandler<OrderDeliveredEvent>, OrderDeliveredEventHandler>();
        services.AddScoped<INotificationHandler<OrderPlacedIntegrationEvent>, OrderPlacedIntegrationEventHandler>();
        services.AddScoped<INotificationHandler<InventoryReservedIntegrationEvent>, InventoryReservedIntegrationEventHandler>();
        services.AddScoped<INotificationHandler<InventoryReservationFailedIntegrationEvent>, InventoryReservationFailedIntegrationEventHandler>();
        services.AddScoped<INotificationHandler<ProductProjectionUpdatedIntegrationEvent>, ProductProjectionUpdatedIntegrationEventHandler>();
        services.AddScoped<INotificationHandler<ProductImageProjectionUpdatedIntegrationEvent>, ProductImageProjectionUpdatedIntegrationEventHandler>();
        services.AddScoped<INotificationHandler<PromoCodeProjectionUpdatedIntegrationEvent>, PromoCodeProjectionUpdatedIntegrationEventHandler>();
        services.AddScoped<INotificationHandler<AddressProjectionUpdatedIntegrationEvent>, AddressProjectionUpdatedIntegrationEventHandler>();
        services.AddScoped<INotificationHandler<PaymentProcessedIntegrationEvent>, PaymentProcessedIntegrationEventHandler>();

        services.AddHostedService<OrderingOutboxDispatcherHostedService>();
        services.AddHostedService<OrderFulfillmentSagaTimeoutHostedService>();

        services.AddOrderingApplication();

        return services;
    }
}

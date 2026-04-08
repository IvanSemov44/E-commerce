using ECommerce.Ordering.Application;
using ECommerce.Ordering.Application.Interfaces;
using ECommerce.Ordering.Domain.Interfaces;
using ECommerce.Ordering.Infrastructure.IntegrationEvents;
using ECommerce.Ordering.Infrastructure.Persistence;
using ECommerce.Ordering.Infrastructure.Persistence.Repositories;
using ECommerce.Ordering.Infrastructure.Services;
using ECommerce.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Ordering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<OrderingDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("OrderingConnection")
                ?? throw new InvalidOperationException("Connection string 'OrderingConnection' is not configured.");

            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICurrentUserService, OrderingCurrentUserService>();
        services.AddScoped<IDbReader, DbReader>();
        services.AddScoped<IProductCatalogReader, DbReader>();
        services.AddScoped<IPromoCodeLookup, DbReader>();
        services.AddScoped<IShippingAddressReader, DbReader>();
        services.AddScoped<IOrderIntegrationEventPublisher, OrderIntegrationEventPublisher>();
        services.AddScoped<INotificationHandler<ProductProjectionUpdatedIntegrationEvent>, ProductProjectionUpdatedIntegrationEventHandler>();
        services.AddScoped<INotificationHandler<ProductImageProjectionUpdatedIntegrationEvent>, ProductImageProjectionUpdatedIntegrationEventHandler>();
        services.AddScoped<INotificationHandler<PromoCodeProjectionUpdatedIntegrationEvent>, PromoCodeProjectionUpdatedIntegrationEventHandler>();
        services.AddScoped<INotificationHandler<AddressProjectionUpdatedIntegrationEvent>, AddressProjectionUpdatedIntegrationEventHandler>();
        services.AddOrderingApplication();

        return services;
    }
}

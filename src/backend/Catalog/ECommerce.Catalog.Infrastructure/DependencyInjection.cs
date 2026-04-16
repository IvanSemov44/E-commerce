using Microsoft.Extensions.DependencyInjection;
using ECommerce.Catalog.Application.Interfaces;
using ECommerce.Catalog.Infrastructure.Services;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Catalog.Infrastructure.Repositories;
using ECommerce.Catalog.Infrastructure.EventHandlers;
using ECommerce.Catalog.Infrastructure.IntegrationEvents;
using ECommerce.Catalog.Infrastructure.Data.Seeders;
using ECommerce.Catalog.Infrastructure.Integration;
using ECommerce.Catalog.Domain.Aggregates.Product.Events;
using ECommerce.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Catalog.Infrastructure;

public static class CatalogInfrastructureServiceExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<CatalogDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("CatalogConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'CatalogConnection' is required but not configured.");
            }

            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductProjectionEventPublisher, ProductProjectionEventPublisher>();

        // Domain event → outbox handlers
        services.AddScoped<INotificationHandler<ProductCreatedEvent>, ProductCreatedEventHandler>();
        services.AddScoped<INotificationHandler<ProductUpdatedEvent>, ProductUpdatedEventHandler>();
        services.AddScoped<INotificationHandler<ProductPriceChangedEvent>, ProductPriceChangedEventHandler>();
        services.AddScoped<INotificationHandler<ProductDeletedEvent>, ProductDeletedEventHandler>();
        services.AddScoped<INotificationHandler<ProductImageAddedEvent>, ProductImageAddedEventHandler>();
        services.AddScoped<INotificationHandler<ProductPrimaryImageSetEvent>, ProductPrimaryImageSetEventHandler>();

        // Integration event → read model handlers
        services.AddScoped<INotificationHandler<ProductRatingProjectionUpdatedIntegrationEvent>, ProductRatingProjectionUpdatedIntegrationEventHandler>();

        services.AddScoped<CatalogDataSeeder>();

        // Catalog-owned outbox dispatcher — polls catalog.outbox_messages
        services.AddHostedService<CatalogOutboxDispatcherHostedService>();

        return services;
    }
}

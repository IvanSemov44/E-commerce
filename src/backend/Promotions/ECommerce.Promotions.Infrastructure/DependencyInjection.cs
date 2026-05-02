using ECommerce.Contracts;
using ECommerce.Promotions.Application;
using ECommerce.Promotions.Domain.Events;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Infrastructure.EventHandlers;
using ECommerce.Promotions.Infrastructure.Integration;
using ECommerce.Promotions.Infrastructure.Persistence;
using ECommerce.Promotions.Infrastructure.Persistence.Repositories;
using MediatR;
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

        // Outbox: write integration events into promotions.outbox_messages
        services.AddScoped<IIntegrationEventOutbox, EfPromotionsIntegrationEventOutbox>();

        // Outbox dispatcher: poll promotions.outbox_messages and publish to the bus
        services.AddOptions<PromotionsOutboxOptions>();
        services.AddHostedService<PromotionsOutboxDispatcherHostedService>();

        // Register repositories
        services.AddScoped<IPromoCodeRepository, PromoCodeRepository>();

        // Register domain event handlers
        services.AddScoped<INotificationHandler<PromoCodeChangedEvent>, PromoCodeChangedEventHandler>();
        services.AddScoped<INotificationHandler<PromoCodeAppliedEvent>, PromoCodeAppliedEventHandler>();
        services.AddScoped<INotificationHandler<PromoCodeExhaustedEvent>, PromoCodeExhaustedEventHandler>();

        // Register application layer
        services.AddPromotionsApplication();

        return services;
    }
}

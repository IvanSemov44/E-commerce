using ECommerce.Contracts;
using ECommerce.Payments.Application;
using ECommerce.Payments.Application.Interfaces;
using ECommerce.Payments.Domain.Events;
using ECommerce.Payments.Infrastructure.EventHandlers;
using ECommerce.Payments.Infrastructure.Integration;
using ECommerce.Payments.Infrastructure.IntegrationEvents;
using ECommerce.Payments.Infrastructure.Persistence;
using ECommerce.Payments.Infrastructure.Persistence.Repositories;
using ECommerce.Payments.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ECommerce.Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<PaymentsDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("PaymentsConnection")
                ?? throw new InvalidOperationException("Connection string 'PaymentsConnection' is not configured.");

            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddOptions<PaymentsOutboxDispatcherOptions>();
        services.AddSingleton<IConfigureOptions<PaymentsOutboxDispatcherOptions>, PaymentsOutboxDispatcherOptionsSetup>();

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentOrderQuery, PaymentOrderQuery>();
        services.AddScoped<IPaymentGateway, MockPaymentGateway>();
        services.AddSingleton<IPaymentStore, InMemoryPaymentStore>();
        services.AddScoped<IWebhookVerificationService, WebhookVerificationService>();
        services.AddScoped<IPaymentsOutboxEventWriter, PaymentsOutboxEventWriter>();

        services.AddScoped<INotificationHandler<PaymentProcessedEvent>, PaymentProcessedEventHandler>();
        services.AddScoped<INotificationHandler<PaymentFailedEvent>, PaymentFailedEventHandler>();
        services.AddScoped<INotificationHandler<PaymentRefundedEvent>, PaymentRefundedEventHandler>();
        services.AddScoped<INotificationHandler<OrderPlacedIntegrationEvent>, OrderPlacedIntegrationEventHandler>();

        services.AddHostedService<PaymentsOutboxDispatcherHostedService>();

        services.AddPaymentsApplication();
        return services;
    }
}

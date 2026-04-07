using ECommerce.Payments.Application;
using ECommerce.Payments.Application.Interfaces;
using ECommerce.Payments.Infrastructure.Persistence.Repositories;
using ECommerce.Payments.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IPaymentOrderRepository, PaymentOrderRepository>();
        services.AddSingleton<IPaymentStore, InMemoryPaymentStore>();
        services.AddScoped<IWebhookVerificationService, WebhookVerificationService>();

        services.AddPaymentsApplication();
        return services;
    }
}

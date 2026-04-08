using ECommerce.Payments.Application;
using ECommerce.Payments.Application.Interfaces;
using ECommerce.Payments.Infrastructure.Persistence;
using ECommerce.Payments.Infrastructure.Persistence.Repositories;
using ECommerce.Payments.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<PaymentsDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var connectionString = configuration["ConnectionStrings:DefaultConnection"]
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IPaymentOrderRepository, PaymentOrderRepository>();
        services.AddSingleton<IPaymentStore, InMemoryPaymentStore>();
        services.AddScoped<IWebhookVerificationService, WebhookVerificationService>();

        services.AddPaymentsApplication();
        return services;
    }
}

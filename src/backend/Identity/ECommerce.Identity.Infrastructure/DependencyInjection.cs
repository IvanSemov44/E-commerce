using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Events;
using ECommerce.Identity.Infrastructure.EventHandlers;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Infrastructure.Integration;
using ECommerce.Identity.Infrastructure.Persistence;
using ECommerce.Identity.Infrastructure.Repositories;
using ECommerce.Identity.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ECommerce.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<IdentityDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("IdentityConnection")
                ?? throw new InvalidOperationException("Connection string 'IdentityConnection' is not configured.");

            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddOptions<IdentityOutboxDispatcherOptions>();
        services.AddSingleton<IConfigureOptions<IdentityOutboxDispatcherOptions>, IdentityOutboxDispatcherOptionsSetup>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAddressProjectionEventPublisher, AddressProjectionEventPublisher>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IIdentityOutboxEventWriter, IdentityOutboxEventWriter>();
        services.AddScoped<IIdentityIntegrationEventPublisher, IdentityIntegrationEventPublisher>();
        services.AddScoped<INotificationHandler<UserRegisteredEvent>, UserRegisteredEventHandler>();
        services.AddScoped<INotificationHandler<EmailVerifiedEvent>, EmailVerifiedEventHandler>();
        services.AddScoped<INotificationHandler<PasswordChangedEvent>, PasswordChangedEventHandler>();
        services.AddScoped<INotificationHandler<AddressAddedEvent>, AddressAddedEventHandler>();
        services.AddScoped<INotificationHandler<AddressDeletedEvent>, AddressDeletedEventHandler>();
        services.AddScoped<INotificationHandler<AddressDefaultShippingChangedEvent>, AddressDefaultShippingChangedEventHandler>();

        services.AddHostedService<IdentityOutboxDispatcherHostedService>();

        return services;
    }
}

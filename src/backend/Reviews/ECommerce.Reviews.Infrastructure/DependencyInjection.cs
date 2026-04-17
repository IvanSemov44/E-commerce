using ECommerce.Reviews.Infrastructure.EventHandlers;
using ECommerce.Reviews.Infrastructure.IntegrationEvents;
using ECommerce.Reviews.Infrastructure.Integration;
using ECommerce.Reviews.Domain.Events;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.Reviews.Infrastructure.Persistence;
using ECommerce.Reviews.Infrastructure.Persistence.Repositories;
using ECommerce.Reviews.Infrastructure.Services;
using ECommerce.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Reviews.Application.Interfaces;

namespace ECommerce.Reviews.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReviewsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("ReviewsConnection")
            ?? throw new InvalidOperationException("Connection string 'ReviewsConnection' is not configured.");

        services.Configure<ReviewsOutboxDispatcherOptions>(
            configuration.GetSection("IntegrationMessaging:Outbox"));

        services.AddDbContext<ReviewsDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IProductProjectionService, ProductProjectionService>();
        services.AddScoped<IReviewRatingProjectionEventPublisher, ReviewRatingProjectionEventPublisher>();
        services.AddScoped<INotificationHandler<ProductProjectionUpdatedIntegrationEvent>, ProductProjectionUpdatedIntegrationEventHandler>();
        services.AddScoped<INotificationHandler<ReviewRatingProjectionChangedDomainEvent>, ReviewRatingProjectionChangedDomainEventHandler>();

        // Reviews-owned outbox dispatcher — polls reviews.outbox_messages
        services.AddHostedService<ReviewsOutboxDispatcherHostedService>();

        return services;
    }
}

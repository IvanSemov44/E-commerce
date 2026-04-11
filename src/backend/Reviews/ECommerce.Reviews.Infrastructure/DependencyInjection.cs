using ECommerce.Reviews.Application;
using ECommerce.Reviews.Infrastructure.EventHandlers;
using ECommerce.Reviews.Infrastructure.IntegrationEvents;
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

namespace ECommerce.Reviews.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReviewsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("ReviewsConnection")
            ?? throw new InvalidOperationException("Connection string 'ReviewsConnection' is not configured.");

        services.AddDbContext<ReviewsDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<ECommerce.Reviews.Application.Interfaces.ICatalogService, CatalogService>();
        services.AddScoped<ECommerce.Reviews.Application.Interfaces.IReviewRatingProjectionEventPublisher, ReviewRatingProjectionEventPublisher>();
        services.AddScoped<INotificationHandler<ProductProjectionUpdatedIntegrationEvent>, ProductProjectionUpdatedIntegrationEventHandler>();
        services.AddScoped<INotificationHandler<ReviewRatingProjectionChangedDomainEvent>, ReviewRatingProjectionChangedDomainEventHandler>();
        services.AddReviewsApplication();

        return services;
    }
}

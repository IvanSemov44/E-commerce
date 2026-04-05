using ECommerce.Ordering.Application;
using ECommerce.Ordering.Application.Interfaces;
using ECommerce.Ordering.Domain.Interfaces;
using ECommerce.Ordering.Infrastructure.Persistence;
using ECommerce.Ordering.Infrastructure.Persistence.Repositories;
using ECommerce.Ordering.Infrastructure.Services;
using ECommerce.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Ordering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICurrentUserService, OrderingCurrentUserService>();
        services.AddScoped<IDbReader, DbReader>();
        services.AddOrderingApplication();

        return services;
    }
}
using ECommerce.Ordering.Application;
using ECommerce.Ordering.Application.Interfaces;
using ECommerce.Ordering.Domain.Interfaces;
using ECommerce.Ordering.Infrastructure.Persistence;
using ECommerce.Ordering.Infrastructure.Persistence.Repositories;
using ECommerce.Ordering.Infrastructure.Services;
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
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICurrentUserService, OrderingCurrentUserService>();
        services.AddScoped<IDbReader, DbReader>();
        services.AddOrderingApplication();

        return services;
    }
}

using ECommerce.Inventory.Application.Interfaces;
using ECommerce.Inventory.Domain.Interfaces;
using ECommerce.Inventory.Infrastructure.Persistence;
using ECommerce.Inventory.Infrastructure.Persistence.Repositories;
using ECommerce.Inventory.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<InventoryDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("InventoryConnection")
                ?? throw new InvalidOperationException("Connection string 'InventoryConnection' is not configured.");

            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IInventoryProjectionEventPublisher, InventoryProjectionEventPublisher>();
        services.AddScoped<IInventoryReservationEventPublisher, InventoryReservationEventPublisher>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(ECommerce.Inventory.Application.Commands.IncreaseStock.IncreaseStockCommand).Assembly));

        return services;
    }
}

using ECommerce.Infrastructure.Data;
using ECommerce.Inventory.Application.Interfaces;
using ECommerce.Inventory.Domain.Interfaces;
using ECommerce.Inventory.Infrastructure.Persistence.Configurations;
using ECommerce.Inventory.Infrastructure.Persistence.Repositories;
using ECommerce.Inventory.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services)
    {
        // Register EF configurations so AppDbContext picks them up without a direct project reference
        AppDbContext.RegisterConfigurationAssembly(typeof(InventoryItemConfiguration).Assembly);

        services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
        services.AddScoped<IEmailService, EmailService>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(ECommerce.Inventory.Application.Commands.IncreaseStock.IncreaseStockCommand).Assembly));

        return services;
    }
}
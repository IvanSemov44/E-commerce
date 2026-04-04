using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Promotions.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPromotionsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}
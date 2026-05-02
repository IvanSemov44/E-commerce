using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure;

public static class InfrastructureCompositionExtensions
{
    public static IServiceCollection AddInfrastructurePersistence(
        this IServiceCollection services)
    {
        return services;
    }

    public static IServiceCollection AddInfrastructureSeeders(this IServiceCollection services)
    {
        // PR5 narrow slice: shared AppDb seeders are retired in favor of context-owned seed paths.
        return services;
    }
}

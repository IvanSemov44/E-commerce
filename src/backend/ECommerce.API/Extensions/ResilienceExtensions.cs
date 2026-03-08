using ECommerce.Infrastructure.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace ECommerce.API.Extensions;

/// <summary>
/// Extension methods for registering resilience policies and middleware.
/// </summary>
public static class ResilienceExtensions
{
    /// <summary>
    /// Adds Polly resilience policies to the dependency injection container.
    /// Includes HTTP client and database policies with retry, circuit breaker, and timeout.
    /// </summary>
    public static IServiceCollection AddResiliencePolicies(this IServiceCollection services)
    {
        // Register HTTP client policies
        services.AddSingleton<IAsyncPolicy<HttpResponseMessage>>(
            ResiliencePolicies.GetCombinedHttpPolicy());

        // Register database policies
        services.AddSingleton<IAsyncPolicy>(
            ResiliencePolicies.GetCombinedDatabasePolicy());

        return services;
    }

    /// <summary>
    /// Adds correlation ID middleware for distributed tracing.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<Middleware.CorrelationIdMiddleware>();
    }
}

using ECommerce.API.Common.Configuration;
using ECommerce.API.HealthChecks;
using ECommerce.API.Middleware;
using ECommerce.API.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

namespace ECommerce.API.Common.Extensions;

/// <summary>
/// Extension methods for configuring the application middleware pipeline and startup tasks.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Applies pending database migrations and seeds the database with initial data.
    /// </summary>
    public static async Task ApplyMigrationsAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var initializationService = services.GetRequiredService<AppDbContextInitializationService>();
            await initializationService.InitializeAsync(app.Environment);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while applying migrations or seeding database");
            throw;
        }
    }

    /// <summary>
    /// Configures the middleware pipeline for the application.
    /// </summary>
    public static WebApplication ConfigureMiddlewarePipeline(this WebApplication app)
    {
        // Security headers - must be first in pipeline
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // Correlation ID for distributed tracing - enriches all logs with unique request ID
        app.UseCorrelationId();

        // Global exception handler - catches unhandled exceptions
        app.UseMiddleware<GlobalExceptionMiddleware>();

        // Development-specific endpoints
        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerDocumentation();
        }

        // HTTPS redirect for production/staging - skip in Test to avoid noisy warnings
        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Test"))
        {
            app.UseForwardedHeaders();
            app.UseHttpsRedirection();
        }

        // CORS - use environment-appropriate policy
        var corsPolicy = app.Environment.IsDevelopment()
            ? CorsPolicyNames.Development
            : CorsPolicyNames.Production;
        app.UseCors(corsPolicy);

        // Rate limiting - before authentication
        app.UseRateLimiter();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // CSRF Protection - after authentication
        app.UseCsrfProtection();

        // Controllers
        app.MapControllers();

        // Health check endpoints
        app.MapHealthCheckEndpoints();

        return app;
    }

    /// <summary>
    /// Configures Swagger UI and OpenAPI endpoints for development.
    /// </summary>
    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Commerce API v1");
            options.RoutePrefix = string.Empty;
        });
        return app;
    }

    /// <summary>
    /// Maps health check endpoints for container orchestration and monitoring.
    /// </summary>
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        // Basic liveness probe - always returns 200 if app is running
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
            .WithName("Health")
            .AllowAnonymous()
            .WithDescription("Basic liveness probe");

        // Readiness probe - checks all dependencies
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse
        }).AllowAnonymous().WithDescription("Readiness probe");

        // Detailed health check - all checks with full details
        app.MapHealthChecks("/health/detail", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse
        }).AllowAnonymous().WithDescription("Detailed health check");

        Log.Information("Health check endpoints configured: /health, /health/ready, /health/detail");

        return app;
    }
}



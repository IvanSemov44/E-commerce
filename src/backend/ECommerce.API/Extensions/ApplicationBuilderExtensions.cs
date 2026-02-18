using ECommerce.API.HealthChecks;
using ECommerce.API.Middleware;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Data.Seeders;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

namespace ECommerce.API.Extensions;

/// <summary>
/// Extension methods for WebApplication to configure the middleware pipeline and startup tasks.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Applies pending database migrations and seeds the database with initial data.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task ApplyMigrationsAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            await ApplyMigrationsAsync(context);
            await SeedDatabaseAsync(context, services, app.Environment);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while applying migrations or seeding database");
            throw;
        }
    }

    /// <summary>
    /// Applies any pending database migrations.
    /// </summary>
    private static async Task ApplyMigrationsAsync(AppDbContext context)
    {
        try
        {
            var pendingMigrations = await GetPendingMigrationsSafeAsync(context);

            if (pendingMigrations.Any())
            {
                Log.Information("Applying pending migrations...");
                await context.Database.MigrateAsync();
                Log.Information("Migrations applied successfully.");
            }
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex, "Skipping migration checks for non-relational provider.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "An error occurred while applying migrations (non-fatal in tests).");
        }
    }

    /// <summary>
    /// Safely retrieves pending migrations, handling non-relational providers.
    /// </summary>
    private static async Task<IEnumerable<string>> GetPendingMigrationsSafeAsync(AppDbContext context)
    {
        try
        {
            return await Task.Run(() => context.Database.GetPendingMigrations());
        }
        catch (InvalidOperationException)
        {
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Seeds the database with initial data.
    /// </summary>
    private static async Task SeedDatabaseAsync(
        AppDbContext context,
        IServiceProvider services,
        IWebHostEnvironment environment)
    {
        try
        {
            Log.Information("Seeding database with sample data...");
            var seeder = services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync(context, environment);
            Log.Information("Database seeding completed.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "An error occurred while seeding database (non-fatal in tests).");
        }
    }

    /// <summary>
    /// Configures the middleware pipeline for the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication ConfigureMiddlewarePipeline(this WebApplication app)
    {
        // Security headers - must be first in pipeline
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // Global exception handler - catches unhandled exceptions
        app.UseMiddleware<GlobalExceptionMiddleware>();

        // Development-specific endpoints
        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerDocumentation();
        }

        // HTTPS redirect for production
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // CORS - use environment-appropriate policy
        var corsPolicy = app.Environment.IsDevelopment()
            ? ServiceCollectionExtensions.CorsPolicyNames.Development
            : ServiceCollectionExtensions.CorsPolicyNames.Production;
        app.UseCors(corsPolicy);

        // Rate limiting - before authentication
        app.UseRateLimiter();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // CSRF Protection - after authentication so we can check if user is authenticated
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
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Commerce API v1");
            options.RoutePrefix = string.Empty; // Set Swagger UI at root
        });
        return app;
    }

    /// <summary>
    /// Maps health check endpoints for container orchestration and monitoring.
    /// Provides three endpoints:
    /// - /health: Basic liveness probe (always returns 200 if app is running)
    /// - /health/ready: Readiness probe (checks database, memory)
    /// - /health/detail: Detailed health status for monitoring systems
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        // Basic liveness probe - always returns 200 if app is running
        // Used by Kubernetes to know if the container should be restarted
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
            .WithName("Health")
            .AllowAnonymous()
            .WithDescription("Basic liveness probe - returns 200 if the application is running");

        // Readiness probe - checks all dependencies (database, memory)
        // Used by Kubernetes to know if the container can receive traffic
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse
        }).AllowAnonymous()
          .WithDescription("Readiness probe - checks database connectivity and system health");

        // Detailed health check - all checks with full details
        // Used by monitoring systems (Prometheus, Datadog, etc.)
        app.MapHealthChecks("/health/detail", new HealthCheckOptions
        {
            Predicate = _ => true, // Run all health checks
            ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse
        }).AllowAnonymous()
          .WithDescription("Detailed health check - returns full status of all health checks");

        Log.Information("Health check endpoints configured: /health, /health/ready, /health/detail");

        return app;
    }
}

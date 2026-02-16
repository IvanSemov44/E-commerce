using ECommerce.API.Middleware;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Data.Seeders;
using Microsoft.EntityFrameworkCore;
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

        // CORS
        app.UseCors("AllowAll");

        // Rate limiting - before authentication
        app.UseRateLimiter();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Controllers
        app.MapControllers();

        // Health check endpoint
        app.MapHealthCheckEndpoint();

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
    /// Maps the health check endpoint for container orchestration.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapHealthCheckEndpoint(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
            .WithName("Health")
            .AllowAnonymous();
        return app;
    }
}

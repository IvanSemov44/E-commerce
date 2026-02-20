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
                Log.Information("Applying pending migrations... Count: {Count}", pendingMigrations.Count());
                foreach (var migration in pendingMigrations)
                {
                    Log.Information("Pending migration: {Migration}", migration);
                }
                await context.Database.MigrateAsync();
                Log.Information("Migrations applied successfully.");
            }
            else
            {
                Log.Information("No pending migrations found.");
            }

            // Validate that the database schema matches EF Core's model
            // This catches cases where migration history is out of sync with actual schema
            await ValidateDatabaseSchemaAsync(context);
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex, "Skipping migration checks for non-relational provider.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply database migrations. This is fatal in production.");
            throw; // Re-throw to fail startup in production
        }
    }

    /// <summary>
    /// Validates that the database schema matches EF Core's model.
    /// This catches issues where the migration history is out of sync with the actual schema.
    /// </summary>
    private static async Task ValidateDatabaseSchemaAsync(AppDbContext context)
    {
        try
        {
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            
            // First, check if critical tables exist
            var requiredTables = new List<string>
            {
                "Users", "Products", "Orders", "RefreshTokens", "Categories"
            };

            var missingTables = new List<string>();
            
            foreach (var tableName in requiredTables)
            {
                command.CommandText = $@"
                    SELECT EXISTS (
                        SELECT 1 FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                        AND table_name = '{tableName}'
                    )";
                
                var result = await command.ExecuteScalarAsync();
                var tableExists = result != null && (bool)result;
                
                if (!tableExists)
                {
                    missingTables.Add(tableName);
                }
            }

            if (missingTables.Any())
            {
                var errorMessage = $"Database schema validation failed. Missing required tables: {string.Join(", ", missingTables)}. " +
                                   "The database may not be properly initialized or migrations may have failed.";
                Log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Check critical columns that have caused issues in production
            // Format: (TableName, ColumnName, ShouldExist, Reason)
            var columnChecks = new List<(string TableName, string ColumnName, bool ShouldExist, string Reason)>
            {
                // RefreshToken ignores RowVersion from BaseEntity - column should NOT exist
                ("RefreshTokens", "RowVersion", false, "RefreshToken entity ignores RowVersion from BaseEntity"),
                // These tables should have RowVersion for optimistic concurrency
                ("Products", "RowVersion", true, "Product entity uses RowVersion for optimistic concurrency"),
                ("Orders", "RowVersion", true, "Order entity uses RowVersion for optimistic concurrency"),
                ("PromoCodes", "RowVersion", true, "PromoCode entity uses RowVersion for optimistic concurrency"),
                // Critical columns that must exist
                ("RefreshTokens", "Token", true, "RefreshToken requires Token column for authentication"),
                ("RefreshTokens", "UserId", true, "RefreshToken requires UserId column for user association"),
                ("RefreshTokens", "ExpiresAt", true, "RefreshToken requires ExpiresAt column for validity"),
            };

            var schemaIssues = new List<string>();
            
            foreach (var (tableName, columnName, shouldExist, reason) in columnChecks)
            {
                command.CommandText = $@"
                    SELECT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_schema = 'public'
                        AND table_name = '{tableName}' 
                        AND column_name = '{columnName}'
                    )";
                
                var result = await command.ExecuteScalarAsync();
                var columnExists = result != null && (bool)result;
                
                if (shouldExist && !columnExists)
                {
                    schemaIssues.Add($"Table '{tableName}' is missing required column '{columnName}'. {reason}. " +
                                    "Database schema is out of sync with migrations.");
                }
                else if (!shouldExist && columnExists)
                {
                    schemaIssues.Add($"Table '{tableName}' has column '{columnName}' which should not exist. {reason}. " +
                                    "Run migration to drop it or manually remove the column.");
                }
            }

            // Check for migration history consistency
            command.CommandText = @"
                SELECT COUNT(*) FROM ""__EFMigrationsHistory"" 
                WHERE ""MigrationId"" LIKE '%AddRowVersionToAllTables%'";
            var rowVersionMigrationResult = await command.ExecuteScalarAsync();
            var rowVersionMigrationApplied = rowVersionMigrationResult != null && Convert.ToInt64(rowVersionMigrationResult) > 0;

            // If the migration was applied, verify RefreshTokens has RowVersion (which we now need to drop)
            if (rowVersionMigrationApplied)
            {
                Log.Information("AddRowVersionToAllTables migration was previously applied. " +
                               "The IgnoreRefreshTokenRowVersion migration will handle the RefreshTokens.RowVersion column.");
            }

            await connection.CloseAsync();

            if (schemaIssues.Any())
            {
                var errorMessage = $"Database schema validation failed. Issues found:\n{string.Join("\n", schemaIssues)}";
                Log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            Log.Information("Database schema validation passed. All critical tables and columns verified.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            Log.Warning(ex, "Could not validate database schema (non-fatal). This may indicate a connection issue.");
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

        // HTTPS redirect for production - configured for reverse proxy (Render, Azure, AWS, etc.)
        if (!app.Environment.IsDevelopment())
        {
            // FIX: Configure HTTPS redirection to work behind reverse proxy
            // Render terminates SSL at the load balancer, so we need to trust forwarded headers
            app.UseForwardedHeaders();
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

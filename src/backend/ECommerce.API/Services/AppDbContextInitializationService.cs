using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Data.Seeders;
using ECommerce.API.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ECommerce.API.Services;

/// <summary>
/// Encapsulates AppDbContext migration + seed startup flow so API composition stays thin.
/// </summary>
public sealed class AppDbContextInitializationService(
    AppDbContext context,
    DatabaseSeeder seeder,
    ReviewsProductProjectionBackfillService reviewsBackfillService)
{
    public async Task InitializeAsync(IWebHostEnvironment environment)
    {
        // Keep startup data lifecycle in one place: schema first, then data seed, then projection backfill.
        // This ordering avoids backfill running against stale schema/data during app boot.
        await ApplyMigrationsAsync();

        // Integration tests seed their own deterministic dataset in TestWebApplicationFactory.
        // Skipping app-level seed avoids duplicate work and startup overhead.
        if (!environment.IsEnvironment("Test"))
        {
            await SeedDatabaseAsync(environment);
            await BackfillReviewsProductProjectionsAsync();
        }
    }

    private async Task ApplyMigrationsAsync()
    {
        try
        {
            var providerName = context.Database.ProviderName ?? string.Empty;
            if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase) ||
                !context.Database.IsRelational())
            {
                Log.Information("Skipping migration/schema validation for non-relational provider: {Provider}", providerName);
                return;
            }

            var pendingMigrations = await GetPendingMigrationsSafeAsync();

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

            await DatabaseSchemaValidator.ValidateAsync(context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply database migrations.");
            throw;
        }
    }

    private async Task<IEnumerable<string>> GetPendingMigrationsSafeAsync()
    {
        try
        {
            return await Task.Run(() => context.Database.GetPendingMigrations());
        }
        catch (InvalidOperationException)
        {
            return Enumerable.Empty<string>();
        }
        catch (Exception ex) when (ex.Message.Contains("__EFMigrationsHistory") ||
                                   ex.InnerException?.Message.Contains("__EFMigrationsHistory") == true)
        {
            Log.Information("Migration history table does not exist, all migrations will be applied.");
            return await Task.Run(() => context.Database.GetMigrations());
        }
    }

    private async Task SeedDatabaseAsync(IWebHostEnvironment environment)
    {
        try
        {
            Log.Information("Seeding database with sample data...");
            await seeder.SeedAsync(context, environment);
            Log.Information("Database seeding completed.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "An error occurred while seeding database.");
        }
    }

    private async Task BackfillReviewsProductProjectionsAsync()
    {
        try
        {
            await reviewsBackfillService.BackfillAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "An error occurred while backfilling Reviews product projections.");
        }
    }
}

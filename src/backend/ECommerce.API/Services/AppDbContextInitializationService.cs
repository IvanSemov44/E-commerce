using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Integration;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Catalog.Infrastructure.Data.Seeders;
using ECommerce.Reviews.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ECommerce.API.Services;

/// <summary>
/// Encapsulates AppDbContext migration + seed startup flow so API composition stays thin.
/// </summary>
public sealed class AppDbContextInitializationService(
    IAppDbInitializationService appDbInitializationService,
    CatalogDbContext catalogDbContext,
    CatalogDataSeeder catalogDataSeeder,
    ReviewsDbContext reviewsDbContext,
    IntegrationPersistenceDbContext integrationPersistenceDbContext,
    ReviewsProductProjectionBackfillService reviewsBackfillService)
{
    public async Task InitializeAsync(IWebHostEnvironment environment)
    {
        // Keep startup data lifecycle in one place: schema first, then data seed, then projection backfill.
        // This ordering avoids backfill running against stale schema/data during app boot.
        await appDbInitializationService.InitializeAsync(environment);
        await MigrateCatalogContextAsync();
        await MigrateReviewsContextAsync();

        if (!environment.IsEnvironment("Test"))
        {
            await SeedCatalogContextAsync(environment);
            await EnsureIntegrationSchemaAsync();
            await BackfillReviewsProductProjectionsAsync();
        }
    }

    private async Task MigrateCatalogContextAsync(CancellationToken cancellationToken = default)
    {
        var providerName = catalogDbContext.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase) || !catalogDbContext.Database.IsRelational())
        {
            Log.Information("Skipping Catalog migration for non-relational provider: {Provider}", providerName);
            return;
        }

        var pending = await catalogDbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (!pending.Any())
            return;

        Log.Information("Applying {Count} pending Catalog migration(s)...", pending.Count());
        await catalogDbContext.Database.MigrateAsync(cancellationToken);
        Log.Information("Catalog migrations applied successfully.");
    }

    private async Task MigrateReviewsContextAsync(CancellationToken cancellationToken = default)
    {
        var providerName = reviewsDbContext.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase) || !reviewsDbContext.Database.IsRelational())
        {
            Log.Information("Skipping Reviews migration for non-relational provider: {Provider}", providerName);
            return;
        }

        var pending = await reviewsDbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (!pending.Any())
            return;

        Log.Information("Applying {Count} pending Reviews migration(s)...", pending.Count());
        await reviewsDbContext.Database.MigrateAsync(cancellationToken);
        Log.Information("Reviews migrations applied successfully.");
    }

    private async Task SeedCatalogContextAsync(IWebHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        try
        {
            if (environment.IsProduction() &&
                !string.Equals(Environment.GetEnvironmentVariable("ENABLE_PRODUCTION_SEEDING"), "true", StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("Skipping Catalog context seeding in Production (set ENABLE_PRODUCTION_SEEDING=true to enable).");
                return;
            }

            await catalogDataSeeder.SeedAsync(catalogDbContext, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "An error occurred while seeding Catalog context data.");
        }
    }

    private async Task EnsureIntegrationSchemaAsync(CancellationToken cancellationToken = default)
    {
        var providerName = integrationPersistenceDbContext.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase) ||
            !integrationPersistenceDbContext.Database.IsRelational())
            return;

        // IntegrationPersistenceDbContext currently does not own migration files.
        // EnsureCreated keeps outbox/inbox/saga/dead-letter tables present in dedicated integration DBs.
        await integrationPersistenceDbContext.Database.EnsureCreatedAsync(cancellationToken);
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

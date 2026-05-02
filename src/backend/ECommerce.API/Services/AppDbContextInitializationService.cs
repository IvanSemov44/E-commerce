using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Catalog.Infrastructure.Data.Seeders;
using ECommerce.Payments.Infrastructure.Persistence;
using ECommerce.Promotions.Infrastructure.Persistence;
using ECommerce.Reviews.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ECommerce.API.Services;

/// <summary>
/// Runs schema migration and seed startup flow for all bounded contexts.
/// </summary>
public sealed class AppDbContextInitializationService(
    DataProtectionKeysContext dataProtectionKeysContext,
    CatalogDbContext catalogDbContext,
    CatalogDataSeeder catalogDataSeeder,
    PaymentsDbContext paymentsDbContext,
    PromotionsDbContext promotionsDbContext,
    ReviewsDbContext reviewsDbContext,
    ReviewsProductProjectionBackfillService reviewsBackfillService)
{
    public async Task InitializeAsync(IWebHostEnvironment environment)
    {
        await EnsureDataProtectionSchemaAsync();

        if (environment.IsEnvironment("Test"))
            return;

        await MigrateCatalogContextAsync();
        await MigratePaymentsContextAsync();
        await MigratePromotionsContextAsync();
        await MigrateReviewsContextAsync();

        await SeedCatalogContextAsync(environment);
        await BackfillReviewsProductProjectionsAsync();
    }

    private async Task EnsureDataProtectionSchemaAsync(CancellationToken cancellationToken = default)
    {
        var providerName = dataProtectionKeysContext.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase) || !dataProtectionKeysContext.Database.IsRelational())
            return;

        await dataProtectionKeysContext.Database.EnsureCreatedAsync(cancellationToken);
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

    private async Task MigratePaymentsContextAsync(CancellationToken cancellationToken = default)
    {
        var providerName = paymentsDbContext.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase) || !paymentsDbContext.Database.IsRelational())
        {
            Log.Information("Skipping Payments migration for non-relational provider: {Provider}", providerName);
            return;
        }

        var pending = await paymentsDbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (!pending.Any())
            return;

        Log.Information("Applying {Count} pending Payments migration(s)...", pending.Count());
        await paymentsDbContext.Database.MigrateAsync(cancellationToken);
        Log.Information("Payments migrations applied successfully.");
    }

    private async Task MigratePromotionsContextAsync(CancellationToken cancellationToken = default)
    {
        var providerName = promotionsDbContext.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase) || !promotionsDbContext.Database.IsRelational())
        {
            Log.Information("Skipping Promotions migration for non-relational provider: {Provider}", providerName);
            return;
        }

        var pending = await promotionsDbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (!pending.Any())
            return;

        Log.Information("Applying {Count} pending Promotions migration(s)...", pending.Count());
        await promotionsDbContext.Database.MigrateAsync(cancellationToken);
        Log.Information("Promotions migrations applied successfully.");
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

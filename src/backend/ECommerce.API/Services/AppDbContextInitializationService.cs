using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Integration;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ECommerce.API.Services;

/// <summary>
/// Encapsulates AppDbContext migration + seed startup flow so API composition stays thin.
/// </summary>
public sealed class AppDbContextInitializationService(
    IAppDbInitializationService appDbInitializationService,
    IntegrationPersistenceDbContext integrationPersistenceDbContext,
    ReviewsProductProjectionBackfillService reviewsBackfillService)
{
    public async Task InitializeAsync(IWebHostEnvironment environment)
    {
        // Keep startup data lifecycle in one place: schema first, then data seed, then projection backfill.
        // This ordering avoids backfill running against stale schema/data during app boot.
        await appDbInitializationService.InitializeAsync(environment);

        if (!environment.IsEnvironment("Test"))
        {
            await EnsureIntegrationSchemaAsync();
            await BackfillReviewsProductProjectionsAsync();
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

using ECommerce.Infrastructure;
using Serilog;

namespace ECommerce.API.Services;

/// <summary>
/// Encapsulates AppDbContext migration + seed startup flow so API composition stays thin.
/// </summary>
public sealed class AppDbContextInitializationService(
    IAppDbInitializationService appDbInitializationService,
    ReviewsProductProjectionBackfillService reviewsBackfillService)
{
    public async Task InitializeAsync(IWebHostEnvironment environment)
    {
        // Keep startup data lifecycle in one place: schema first, then data seed, then projection backfill.
        // This ordering avoids backfill running against stale schema/data during app boot.
        await appDbInitializationService.InitializeAsync(environment);

        if (!environment.IsEnvironment("Test"))
            await BackfillReviewsProductProjectionsAsync();
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

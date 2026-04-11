using ECommerce.Catalog.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace ECommerce.Catalog.Infrastructure.Data.Seeders;

public sealed class CatalogDataSeeder(
    ILogger<CatalogDataSeeder> logger)
{
    public async Task SeedAsync(CatalogDbContext context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting Catalog context seed...");

        await CatalogCategorySeeder.SeedAsync(context, cancellationToken);
        await CatalogProductSeeder.SeedAsync(context, cancellationToken);

        logger.LogInformation("Catalog context seed completed.");
    }
}

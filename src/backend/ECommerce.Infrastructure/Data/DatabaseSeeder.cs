using ECommerce.Infrastructure.Data.Seeders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Data;

/// <summary>
/// Orchestrates the seeding of all entities into the database.
/// Calls individual seeders in the correct sequence to maintain referential integrity.
/// Seeds in production only when explicitly enabled via ENABLE_PRODUCTION_SEEDING environment variable.
/// </summary>
public class DatabaseSeeder(
    IUserSeeder userSeeder,
    ICategorySeeder categorySeeder,
    IProductSeeder productSeeder,
    ILogger<DatabaseSeeder> logger)
{
    /// <summary>
    /// Seed all data into the database in the correct order.
    /// In production, requires ENABLE_PRODUCTION_SEEDING=true to seed.
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="environment">Hosting environment (to check if production)</param>
    public async Task SeedAsync(AppDbContext context, IHostEnvironment environment)
    {
        // Safety guard: Only seed in production if explicitly enabled
        if (environment.IsProduction())
        {
            var enableSeeding = Environment.GetEnvironmentVariable("ENABLE_PRODUCTION_SEEDING");
            if (!string.Equals(enableSeeding, "true", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Skipping database seeding in Production environment (set ENABLE_PRODUCTION_SEEDING=true to enable)");
                return;
            }
            logger.LogWarning("Production seeding is ENABLED. This should only be used for initial deployment!");
        }

        try
        {
            logger.LogInformation("Starting database seeding in {Environment} environment...", environment.EnvironmentName);

            // Seed users first (no dependencies)
            logger.LogInformation("Seeding users...");
            await userSeeder.SeedAsync(context);

            // Seed categories (no dependencies)
            logger.LogInformation("Seeding categories...");
            await categorySeeder.SeedAsync(context);

            // Seed products and product images (depends on categories)
            logger.LogInformation("Seeding products...");
            await productSeeder.SeedAsync(context);

            logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }
}

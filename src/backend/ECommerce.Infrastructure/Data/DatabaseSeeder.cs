using ECommerce.Infrastructure.Data.Seeders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Data;

/// <summary>
/// Orchestrates the seeding of all entities into the database.
/// Calls individual seeders in the correct sequence to maintain referential integrity.
/// Never seeds in production environments for safety.
/// </summary>
public class DatabaseSeeder
{
    private readonly IUserSeeder _userSeeder;
    private readonly ICategorySeeder _categorySeeder;
    private readonly IProductSeeder _productSeeder;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IUserSeeder userSeeder,
        ICategorySeeder categorySeeder,
        IProductSeeder productSeeder,
        ILogger<DatabaseSeeder> logger)
    {
        _userSeeder = userSeeder;
        _categorySeeder = categorySeeder;
        _productSeeder = productSeeder;
        _logger = logger;
    }

    /// <summary>
    /// Seed all data into the database in the correct order.
    /// Skips seeding in production environments to prevent accidental data corruption.
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="environment">Hosting environment (to check if production)</param>
    public async Task SeedAsync(AppDbContext context, IHostEnvironment environment)
    {
        // Safety guard: Never seed in production
        if (environment.IsProduction())
        {
            _logger.LogInformation("Skipping database seeding in Production environment");
            return;
        }

        try
        {
            _logger.LogInformation("Starting database seeding in {Environment} environment...", environment.EnvironmentName);

            // Seed users first (no dependencies)
            _logger.LogInformation("Seeding users...");
            await _userSeeder.SeedAsync(context);

            // Seed categories (no dependencies)
            _logger.LogInformation("Seeding categories...");
            await _categorySeeder.SeedAsync(context);

            // Seed products and product images (depends on categories)
            _logger.LogInformation("Seeding products...");
            await _productSeeder.SeedAsync(context);

            _logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }
}

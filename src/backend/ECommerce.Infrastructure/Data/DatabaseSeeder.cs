using ECommerce.Infrastructure.Data.Seeders;

namespace ECommerce.Infrastructure.Data;

/// <summary>
/// Orchestrates the seeding of all entities into the database.
/// Calls individual seeders in the correct sequence to maintain referential integrity.
/// </summary>
public class DatabaseSeeder
{
    private readonly IUserSeeder _userSeeder;
    private readonly ICategorySeeder _categorySeeder;
    private readonly IProductSeeder _productSeeder;

    public DatabaseSeeder(
        IUserSeeder userSeeder,
        ICategorySeeder categorySeeder,
        IProductSeeder productSeeder)
    {
        _userSeeder = userSeeder;
        _categorySeeder = categorySeeder;
        _productSeeder = productSeeder;
    }

    /// <summary>
    /// Seed all data into the database in the correct order.
    /// </summary>
    public async Task SeedAsync(AppDbContext context)
    {
        try
        {
            Console.WriteLine("Starting database seeding...");

            // Seed users first (no dependencies)
            Console.WriteLine("Seeding users...");
            await _userSeeder.SeedAsync(context);

            // Seed categories (no dependencies)
            Console.WriteLine("Seeding categories...");
            await _categorySeeder.SeedAsync(context);

            // Seed products and product images (depends on categories)
            Console.WriteLine("Seeding products...");
            await _productSeeder.SeedAsync(context);

            Console.WriteLine("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during database seeding: {ex.Message}");
            throw;
        }
    }
}

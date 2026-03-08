namespace ECommerce.Infrastructure.Data.Seeders;

/// <summary>
/// Interface for seeding product data into the database.
/// </summary>
public interface IProductSeeder
{
    /// <summary>
    /// Seed products and product images asynchronously.
    /// </summary>
    Task SeedAsync(AppDbContext context);
}

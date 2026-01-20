namespace ECommerce.Infrastructure.Data.Seeders;

/// <summary>
/// Interface for seeding category data into the database.
/// </summary>
public interface ICategorySeeder
{
    /// <summary>
    /// Seed categories asynchronously.
    /// </summary>
    Task SeedAsync(AppDbContext context);
}

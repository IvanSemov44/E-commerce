namespace ECommerce.Infrastructure.Data.Seeders;

/// <summary>
/// Interface for seeding user data into the database.
/// </summary>
public interface IUserSeeder
{
    /// <summary>
    /// Seed users asynchronously.
    /// </summary>
    Task SeedAsync(AppDbContext context);
}

using ECommerce.Catalog.Domain.Aggregates.Category;
using ECommerce.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds category data into the database.
/// </summary>
public sealed class CatalogCategorySeeder
{
    public static async Task SeedAsync(CatalogDbContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if categories already exist
            if (await context.Categories.AnyAsync(cancellationToken))
            {
                return; // Database already seeded
            }

            var categories = new List<Category>
            {
                Category.Create("Electronics", slugRaw: "electronics").GetDataOrThrow(),
                Category.Create("Fashion", slugRaw: "fashion").GetDataOrThrow(),
                Category.Create("Home & Garden", slugRaw: "home-garden").GetDataOrThrow()
            };

            await context.Categories.AddRangeAsync(categories, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding categories: {ex.Message}");
            throw;
        }
    }
}

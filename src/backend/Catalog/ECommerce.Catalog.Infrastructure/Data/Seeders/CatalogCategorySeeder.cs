using ECommerce.SharedKernel.Entities;
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
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Electronics",
                    Slug = "electronics",
                    Description = "Electronic devices and gadgets",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Fashion",
                    Slug = "fashion",
                    Description = "Clothing and apparel",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Home & Garden",
                    Slug = "home-garden",
                    Description = "Home and garden products",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
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

using ECommerce.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Data;

public class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        try
        {
            // Check if data already exists
            if (await context.Products.AnyAsync())
            {
                return; // Database already seeded
            }

            // Create categories
            var electronics = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Electronics",
                Slug = "electronics",
                Description = "Electronic devices and gadgets",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var fashion = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Fashion",
                Slug = "fashion",
                Description = "Clothing and apparel",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var home = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Home & Garden",
                Slug = "home-garden",
                Description = "Home and garden products",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await context.Categories.AddRangeAsync(electronics, fashion, home);
            await context.SaveChangesAsync();

            // Create sample products
            var products = new List<Product>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Wireless Headphones Pro",
                    Slug = "wireless-headphones-pro",
                    Description = "Premium wireless headphones with noise cancellation and 30-hour battery life",
                    ShortDescription = "Premium wireless headphones",
                    Price = 299.99m,
                    CompareAtPrice = 399.99m,
                    CostPrice = 150.00m,
                    Sku = "WHP-001",
                    StockQuantity = 50,
                    LowStockThreshold = 10,
                    Weight = 0.25m,
                    IsActive = true,
                    IsFeatured = true,
                    CategoryId = electronics.Id,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "4K Webcam Ultra",
                    Slug = "4k-webcam-ultra",
                    Description = "4K resolution webcam with auto-focus and built-in microphone",
                    ShortDescription = "Professional 4K webcam",
                    Price = 149.99m,
                    CompareAtPrice = 199.99m,
                    CostPrice = 75.00m,
                    Sku = "WC-001",
                    StockQuantity = 35,
                    LowStockThreshold = 5,
                    Weight = 0.15m,
                    IsActive = true,
                    IsFeatured = true,
                    CategoryId = electronics.Id,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Mechanical Keyboard RGB",
                    Slug = "mechanical-keyboard-rgb",
                    Description = "Gaming mechanical keyboard with RGB lighting and custom switches",
                    ShortDescription = "RGB gaming keyboard",
                    Price = 129.99m,
                    CompareAtPrice = 179.99m,
                    CostPrice = 60.00m,
                    Sku = "KB-001",
                    StockQuantity = 42,
                    LowStockThreshold = 8,
                    Weight = 0.80m,
                    IsActive = true,
                    IsFeatured = true,
                    CategoryId = electronics.Id,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Premium Cotton T-Shirt",
                    Slug = "premium-cotton-tshirt",
                    Description = "100% organic cotton t-shirt, soft and durable",
                    ShortDescription = "Comfortable cotton t-shirt",
                    Price = 29.99m,
                    CompareAtPrice = 39.99m,
                    CostPrice = 10.00m,
                    Sku = "TSH-001",
                    StockQuantity = 200,
                    LowStockThreshold = 20,
                    Weight = 0.20m,
                    IsActive = true,
                    IsFeatured = false,
                    CategoryId = fashion.Id,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Classic Denim Jeans",
                    Slug = "classic-denim-jeans",
                    Description = "Timeless blue denim jeans with perfect fit",
                    ShortDescription = "Classic blue jeans",
                    Price = 59.99m,
                    CompareAtPrice = 79.99m,
                    CostPrice = 25.00m,
                    Sku = "JNS-001",
                    StockQuantity = 150,
                    LowStockThreshold = 15,
                    Weight = 0.50m,
                    IsActive = true,
                    IsFeatured = false,
                    CategoryId = fashion.Id,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "LED Smart Bulb Set",
                    Slug = "led-smart-bulb-set",
                    Description = "Set of 4 smart LED bulbs with WiFi control and 16 million colors",
                    ShortDescription = "Smart LED bulbs",
                    Price = 79.99m,
                    CompareAtPrice = 119.99m,
                    CostPrice = 35.00m,
                    Sku = "LED-001",
                    StockQuantity = 80,
                    LowStockThreshold = 10,
                    Weight = 0.30m,
                    IsActive = true,
                    IsFeatured = true,
                    CategoryId = home.Id,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Stainless Steel Coffee Maker",
                    Slug = "stainless-steel-coffee-maker",
                    Description = "12-cup programmable coffee maker with thermal carafe",
                    ShortDescription = "Programmable coffee maker",
                    Price = 89.99m,
                    CompareAtPrice = 129.99m,
                    CostPrice = 45.00m,
                    Sku = "CF-001",
                    StockQuantity = 60,
                    LowStockThreshold = 8,
                    Weight = 1.50m,
                    IsActive = true,
                    IsFeatured = false,
                    CategoryId = home.Id,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Portable Wireless Speaker",
                    Slug = "portable-wireless-speaker",
                    Description = "Waterproof Bluetooth speaker with 20-hour battery",
                    ShortDescription = "Waterproof speaker",
                    Price = 69.99m,
                    CompareAtPrice = 99.99m,
                    CostPrice = 30.00m,
                    Sku = "SPK-001",
                    StockQuantity = 90,
                    LowStockThreshold = 12,
                    Weight = 0.40m,
                    IsActive = true,
                    IsFeatured = true,
                    CategoryId = electronics.Id,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            // Add product images
            var productImages = new List<ProductImage>();
            foreach (var product in products)
            {
                productImages.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Url = $"https://via.placeholder.com/500x500?text={Uri.EscapeDataString(product.Name)}",
                    AltText = product.Name,
                    IsPrimary = true,
                    SortOrder = 0,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.ProductImages.AddRangeAsync(productImages);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding database: {ex.Message}");
        }
    }
}

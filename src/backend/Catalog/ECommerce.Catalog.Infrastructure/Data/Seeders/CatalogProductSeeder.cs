using ECommerce.SharedKernel.Entities;
using ECommerce.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds product and product image data into the database.
/// </summary>
public sealed class CatalogProductSeeder
{
    public static async Task SeedAsync(CatalogDbContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if products already exist
            if (await context.Products.AnyAsync(cancellationToken))
            {
                return; // Database already seeded
            }

            // Get categories (assuming they've been seeded)
            var electronics = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "electronics", cancellationToken);
            var fashion = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "fashion", cancellationToken);
            var home = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "home-garden", cancellationToken);

            if (electronics == null || fashion == null || home == null)
            {
                throw new InvalidOperationException("Categories must be seeded before products.");
            }

            var products = CreateProducts(electronics, fashion, home);
            await context.Products.AddRangeAsync(products, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            // Seed product images
            await SeedProductImagesAsync(context, products, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding products: {ex.Message}");
            throw;
        }
    }

    private static List<Product> CreateProducts(Category electronics, Category fashion, Category home)
    {
        return new List<Product>
        {
            // Original Electronics (4)
            new() { Id = Guid.NewGuid(), Name = "Wireless Headphones Pro", Slug = "wireless-headphones-pro", Description = "Premium wireless headphones with noise cancellation and 30-hour battery life", ShortDescription = "Premium wireless headphones", Price = 299.99m, CompareAtPrice = 399.99m, CostPrice = 150.00m, Sku = "WHP-001", StockQuantity = 50, LowStockThreshold = 10, Weight = 0.25m, IsActive = true, IsFeatured = true, CategoryId = electronics.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "4K Webcam Ultra", Slug = "4k-webcam-ultra", Description = "4K resolution webcam with auto-focus and built-in microphone", ShortDescription = "Professional 4K webcam", Price = 149.99m, CompareAtPrice = 199.99m, CostPrice = 75.00m, Sku = "WC-001", StockQuantity = 35, LowStockThreshold = 5, Weight = 0.15m, IsActive = true, IsFeatured = true, CategoryId = electronics.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Mechanical Keyboard RGB", Slug = "mechanical-keyboard-rgb", Description = "Gaming mechanical keyboard with RGB lighting and custom switches", ShortDescription = "RGB gaming keyboard", Price = 129.99m, CompareAtPrice = 179.99m, CostPrice = 60.00m, Sku = "KB-001", StockQuantity = 42, LowStockThreshold = 8, Weight = 0.80m, IsActive = true, IsFeatured = true, CategoryId = electronics.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Portable Wireless Speaker", Slug = "portable-wireless-speaker", Description = "Waterproof Bluetooth speaker with 20-hour battery", ShortDescription = "Waterproof speaker", Price = 69.99m, CompareAtPrice = 99.99m, CostPrice = 30.00m, Sku = "SPK-001", StockQuantity = 90, LowStockThreshold = 12, Weight = 0.40m, IsActive = true, IsFeatured = true, CategoryId = electronics.Id, CreatedAt = DateTime.UtcNow },
            // Additional Electronics (8)
            new() { Id = Guid.NewGuid(), Name = "USB-C Hub 7-in-1", Slug = "usb-c-hub-7-in-1", Description = "Multi-port USB-C hub with HDMI, USB 3.0, and SD card reader", ShortDescription = "Multi-port USB-C hub", Price = 49.99m, CompareAtPrice = 69.99m, CostPrice = 20.00m, Sku = "HUB-001", StockQuantity = 100, LowStockThreshold = 10, Weight = 0.15m, IsActive = true, IsFeatured = false, CategoryId = electronics.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "4K Monitor 27-inch", Slug = "4k-monitor-27-inch", Description = "27-inch 4K IPS monitor with USB-C and 60Hz refresh rate", ShortDescription = "Professional 4K monitor", Price = 399.99m, CompareAtPrice = 499.99m, CostPrice = 200.00m, Sku = "MON-001", StockQuantity = 25, LowStockThreshold = 5, Weight = 6.50m, IsActive = true, IsFeatured = true, CategoryId = electronics.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Wireless Ergonomic Mouse", Slug = "wireless-ergonomic-mouse", Description = "Ergonomic wireless mouse with silent clicks and 12-month battery", ShortDescription = "Ergonomic wireless mouse", Price = 39.99m, CompareAtPrice = 59.99m, CostPrice = 15.00m, Sku = "MOU-001", StockQuantity = 120, LowStockThreshold = 15, Weight = 0.10m, IsActive = true, IsFeatured = false, CategoryId = electronics.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Laptop Stand Adjustable", Slug = "laptop-stand-adjustable", Description = "Aluminum adjustable laptop stand compatible with all laptops", ShortDescription = "Adjustable laptop stand", Price = 34.99m, CompareAtPrice = 49.99m, CostPrice = 12.00m, Sku = "STA-001", StockQuantity = 85, LowStockThreshold = 10, Weight = 0.60m, IsActive = true, IsFeatured = false, CategoryId = electronics.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "USB Condenser Microphone", Slug = "usb-condenser-microphone", Description = "Studio-quality USB condenser microphone with pop filter", ShortDescription = "Professional USB microphone", Price = 79.99m, CompareAtPrice = 119.99m, CostPrice = 35.00m, Sku = "MIC-001", StockQuantity = 50, LowStockThreshold = 8, Weight = 0.45m, IsActive = true, IsFeatured = false, CategoryId = electronics.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Monitor Light Bar", Slug = "monitor-light-bar", Description = "Auto-dimming monitor light bar with USB power and no glare", ShortDescription = "Monitor light bar", Price = 59.99m, CompareAtPrice = 89.99m, CostPrice = 25.00m, Sku = "LIT-001", StockQuantity = 65, LowStockThreshold = 8, Weight = 0.55m, IsActive = true, IsFeatured = false, CategoryId = electronics.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "External SSD 2TB", Slug = "external-ssd-2tb", Description = "Fast 2TB external SSD with USB-C and 550MB/s speed", ShortDescription = "2TB external SSD", Price = 249.99m, CompareAtPrice = 299.99m, CostPrice = 130.00m, Sku = "SSD-001", StockQuantity = 40, LowStockThreshold = 5, Weight = 0.20m, IsActive = true, IsFeatured = true, CategoryId = electronics.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Graphics Drawing Tablet", Slug = "graphics-drawing-tablet", Description = "10x6 inch graphics tablet with 8192 pressure levels", ShortDescription = "Graphics drawing tablet", Price = 89.99m, CompareAtPrice = 129.99m, CostPrice = 40.00m, Sku = "TAB-001", StockQuantity = 35, LowStockThreshold = 5, Weight = 0.50m, IsActive = true, IsFeatured = false, CategoryId = electronics.Id, CreatedAt = DateTime.UtcNow },
            // Original Fashion (2)
            new() { Id = Guid.NewGuid(), Name = "Premium Cotton T-Shirt", Slug = "premium-cotton-tshirt", Description = "100% organic cotton t-shirt, soft and durable", ShortDescription = "Comfortable cotton t-shirt", Price = 29.99m, CompareAtPrice = 39.99m, CostPrice = 10.00m, Sku = "TSH-001", StockQuantity = 200, LowStockThreshold = 20, Weight = 0.20m, IsActive = true, IsFeatured = false, CategoryId = fashion.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Classic Denim Jeans", Slug = "classic-denim-jeans", Description = "Timeless blue denim jeans with perfect fit", ShortDescription = "Classic blue jeans", Price = 59.99m, CompareAtPrice = 79.99m, CostPrice = 25.00m, Sku = "JNS-001", StockQuantity = 150, LowStockThreshold = 15, Weight = 0.50m, IsActive = true, IsFeatured = false, CategoryId = fashion.Id, CreatedAt = DateTime.UtcNow },
            // Additional Fashion (6)
            new() { Id = Guid.NewGuid(), Name = "Premium Leather Belt", Slug = "premium-leather-belt", Description = "Genuine leather belt with stainless steel buckle", ShortDescription = "Quality leather belt", Price = 44.99m, CompareAtPrice = 64.99m, CostPrice = 18.00m, Sku = "BLT-001", StockQuantity = 110, LowStockThreshold = 15, Weight = 0.30m, IsActive = true, IsFeatured = false, CategoryId = fashion.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Merino Wool Sweater", Slug = "merino-wool-sweater", Description = "Soft merino wool sweater, perfect for all seasons", ShortDescription = "Merino wool sweater", Price = 79.99m, CompareAtPrice = 109.99m, CostPrice = 35.00m, Sku = "SWE-001", StockQuantity = 75, LowStockThreshold = 10, Weight = 0.40m, IsActive = true, IsFeatured = false, CategoryId = fashion.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Running Shoes Pro", Slug = "running-shoes-pro", Description = "High-performance running shoes with memory foam", ShortDescription = "Professional running shoes", Price = 119.99m, CompareAtPrice = 159.99m, CostPrice = 55.00m, Sku = "SHO-001", StockQuantity = 95, LowStockThreshold = 12, Weight = 0.35m, IsActive = true, IsFeatured = true, CategoryId = fashion.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Athletic Performance Shorts", Slug = "athletic-performance-shorts", Description = "Moisture-wicking athletic shorts for training and sports", ShortDescription = "Athletic shorts", Price = 39.99m, CompareAtPrice = 54.99m, CostPrice = 16.00m, Sku = "SHT-001", StockQuantity = 140, LowStockThreshold = 20, Weight = 0.15m, IsActive = true, IsFeatured = false, CategoryId = fashion.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Winter Puffer Jacket", Slug = "winter-puffer-jacket", Description = "Insulated winter puffer jacket with water-resistant shell", ShortDescription = "Winter puffer jacket", Price = 149.99m, CompareAtPrice = 199.99m, CostPrice = 70.00m, Sku = "JAC-001", StockQuantity = 60, LowStockThreshold = 8, Weight = 0.75m, IsActive = true, IsFeatured = true, CategoryId = fashion.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Casual Button-Up Shirt", Slug = "casual-button-up-shirt", Description = "100% cotton casual button-up shirt in multiple colors", ShortDescription = "Casual button-up shirt", Price = 49.99m, CompareAtPrice = 69.99m, CostPrice = 20.00m, Sku = "BSH-001", StockQuantity = 130, LowStockThreshold = 15, Weight = 0.25m, IsActive = true, IsFeatured = false, CategoryId = fashion.Id, CreatedAt = DateTime.UtcNow },
            // Original Home & Garden (2)
            new() { Id = Guid.NewGuid(), Name = "LED Smart Bulb Set", Slug = "led-smart-bulb-set", Description = "Set of 4 smart LED bulbs with WiFi control and 16 million colors", ShortDescription = "Smart LED bulbs", Price = 79.99m, CompareAtPrice = 119.99m, CostPrice = 35.00m, Sku = "LED-001", StockQuantity = 80, LowStockThreshold = 10, Weight = 0.30m, IsActive = true, IsFeatured = true, CategoryId = home.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Stainless Steel Coffee Maker", Slug = "stainless-steel-coffee-maker", Description = "12-cup programmable coffee maker with thermal carafe", ShortDescription = "Programmable coffee maker", Price = 89.99m, CompareAtPrice = 129.99m, CostPrice = 45.00m, Sku = "CF-001", StockQuantity = 60, LowStockThreshold = 8, Weight = 1.50m, IsActive = true, IsFeatured = false, CategoryId = home.Id, CreatedAt = DateTime.UtcNow },
            // Additional Home & Garden (6)
            new() { Id = Guid.NewGuid(), Name = "Modern Desk Lamp LED", Slug = "modern-desk-lamp-led", Description = "Touch-control LED desk lamp with brightness adjustment", ShortDescription = "Modern LED desk lamp", Price = 54.99m, CompareAtPrice = 79.99m, CostPrice = 22.00m, Sku = "LAM-001", StockQuantity = 75, LowStockThreshold = 10, Weight = 0.70m, IsActive = true, IsFeatured = false, CategoryId = home.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Ceramic Plant Pot Set", Slug = "ceramic-plant-pot-set", Description = "Set of 3 decorative ceramic plant pots with drainage holes", ShortDescription = "Ceramic plant pots", Price = 44.99m, CompareAtPrice = 64.99m, CostPrice = 18.00m, Sku = "POT-001", StockQuantity = 85, LowStockThreshold = 12, Weight = 1.20m, IsActive = true, IsFeatured = false, CategoryId = home.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Modern Area Rug 8x10", Slug = "modern-area-rug-8x10", Description = "Soft wool blend area rug with modern geometric pattern", ShortDescription = "Area rug 8x10", Price = 199.99m, CompareAtPrice = 279.99m, CostPrice = 95.00m, Sku = "RUG-001", StockQuantity = 30, LowStockThreshold = 5, Weight = 3.50m, IsActive = true, IsFeatured = true, CategoryId = home.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Wooden Frame Wall Art", Slug = "wooden-frame-wall-art", Description = "Set of 3 wooden frame wall art pieces with botanical prints", ShortDescription = "Wall art frame set", Price = 69.99m, CompareAtPrice = 99.99m, CostPrice = 28.00m, Sku = "ART-001", StockQuantity = 50, LowStockThreshold = 8, Weight = 1.10m, IsActive = true, IsFeatured = false, CategoryId = home.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Fabric Storage Basket", Slug = "fabric-storage-basket", Description = "Durable fabric storage basket with handles for organizing", ShortDescription = "Storage basket", Price = 34.99m, CompareAtPrice = 49.99m, CostPrice = 14.00m, Sku = "BAS-001", StockQuantity = 105, LowStockThreshold = 15, Weight = 0.45m, IsActive = true, IsFeatured = false, CategoryId = home.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Smart Air Purifier", Slug = "smart-air-purifier", Description = "Smart air purifier with WiFi control and HEPA filter", ShortDescription = "Smart air purifier", Price = 179.99m, CompareAtPrice = 249.99m, CostPrice = 85.00m, Sku = "PUR-001", StockQuantity = 45, LowStockThreshold = 6, Weight = 4.20m, IsActive = true, IsFeatured = true, CategoryId = home.Id, CreatedAt = DateTime.UtcNow }
        };
    }

    private static async Task SeedProductImagesAsync(CatalogDbContext context, List<Product> products, CancellationToken cancellationToken)
    {
        var productImageMap = CreateProductImageMap();
        var productImages = new List<ProductImage>();

        foreach (var product in products)
        {
            if (productImageMap.TryGetValue(product.Slug, out var images))
            {
                int sortOrder = 0;
                foreach (var (imageUrl, altText) in images)
                {
                    productImages.Add(new ProductImage
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        Url = imageUrl,
                        AltText = altText,
                        IsPrimary = sortOrder == 0,
                        SortOrder = sortOrder,
                        CreatedAt = DateTime.UtcNow
                    });
                    sortOrder++;
                }
            }
            else
            {
                // Fallback for any products not in the map
                productImages.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Url = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=500&h=500&fit=crop",
                    AltText = product.Name,
                    IsPrimary = true,
                    SortOrder = 0,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await context.ProductImages.AddRangeAsync(productImages, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<string, List<(string Url, string Alt)>> CreateProductImageMap()
    {
        return new Dictionary<string, List<(string Url, string Alt)>>
        {
            { "wireless-headphones-pro", new List<(string, string)> { ("https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=500&h=500&fit=crop", "Wireless Headphones Pro - Front View"), ("https://images.unsplash.com/photo-1487215078519-e21cc028cb29?w=500&h=500&fit=crop", "Wireless Headphones Pro - Side View"), ("https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=500&h=500&fit=crop", "Wireless Headphones Pro - In Use") } },
            { "4k-webcam-ultra", new List<(string, string)> { ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "4K Webcam Ultra - Front View"), ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "4K Webcam Ultra - Top View"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "4K Webcam Ultra - Detail") } },
            { "mechanical-keyboard-rgb", new List<(string, string)> { ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "Mechanical Keyboard RGB - Full View"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Mechanical Keyboard RGB - RGB Lights"), ("https://images.unsplash.com/photo-1521017432531-fbd92d768814?w=500&h=500&fit=crop", "Mechanical Keyboard RGB - Detail") } },
            { "portable-wireless-speaker", new List<(string, string)> { ("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "Wireless Speaker - Blue"), ("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "Wireless Speaker - Detail"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Wireless Speaker - Portable") } },
            { "usb-c-hub-7-in-1", new List<(string, string)> { ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "USB-C Hub - Full View"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "USB-C Hub - Ports"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "USB-C Hub - Detail") } },
            { "4k-monitor-27-inch", new List<(string, string)> { ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "4K Monitor - Front"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "4K Monitor - Side View"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "4K Monitor - Stand") } },
            { "wireless-ergonomic-mouse", new List<(string, string)> { ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Ergonomic Mouse - Top"), ("https://images.unsplash.com/photo-1521017432531-fbd92d768814?w=500&h=500&fit=crop", "Ergonomic Mouse - Side"), ("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "Ergonomic Mouse - Bottom") } },
            { "laptop-stand-adjustable", new List<(string, string)> { ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Laptop Stand - Folded"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Laptop Stand - Extended"), ("https://images.unsplash.com/photo-1521017432531-fbd92d768814?w=500&h=500&fit=crop", "Laptop Stand - With Laptop") } },
            { "usb-condenser-microphone", new List<(string, string)> { ("https://images.unsplash.com/photo-1487215078519-e21cc028cb29?w=500&h=500&fit=crop", "USB Microphone - Front"), ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "USB Microphone - Stand"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "USB Microphone - Detail") } },
            { "monitor-light-bar", new List<(string, string)> { ("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "Light Bar - On Monitor"), ("https://images.unsplash.com/photo-1487215078519-e21cc028cb29?w=500&h=500&fit=crop", "Light Bar - Side View"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Light Bar - In Use") } },
            { "external-ssd-2tb", new List<(string, string)> { ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "External SSD - Front"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "External SSD - Cable"), ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "External SSD - Size") } },
            { "graphics-drawing-tablet", new List<(string, string)> { ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "Drawing Tablet - Front"), ("https://images.unsplash.com/photo-1521017432531-fbd92d768814?w=500&h=500&fit=crop", "Drawing Tablet - Pen"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Drawing Tablet - With Stylus") } },
            { "premium-cotton-tshirt", new List<(string, string)> { ("https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=500&h=500&fit=crop", "Premium Cotton T-Shirt - White"), ("https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=500&h=500&fit=crop", "Premium Cotton T-Shirt - Folded"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Premium Cotton T-Shirt - Worn") } },
            { "classic-denim-jeans", new List<(string, string)> { ("https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=500&h=500&fit=crop", "Classic Denim Jeans - Front"), ("https://images.unsplash.com/photo-1487215078519-e21cc028cb29?w=500&h=500&fit=crop", "Classic Denim Jeans - Back"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Classic Denim Jeans - Detail") } },
            { "premium-leather-belt", new List<(string, string)> { ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Leather Belt - Laid"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Leather Belt - Buckle"), ("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "Leather Belt - Worn") } },
            { "merino-wool-sweater", new List<(string, string)> { ("https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=500&h=500&fit=crop", "Wool Sweater - Front"), ("https://images.unsplash.com/photo-1487215078519-e21cc028cb29?w=500&h=500&fit=crop", "Wool Sweater - Back"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Wool Sweater - Folded") } },
            { "running-shoes-pro", new List<(string, string)> { ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "Running Shoes - Side"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Running Shoes - Top"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Running Shoes - Front") } },
            { "athletic-performance-shorts", new List<(string, string)> { ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Athletic Shorts - Front"), ("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "Athletic Shorts - Back"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Athletic Shorts - Waist") } },
            { "winter-puffer-jacket", new List<(string, string)> { ("https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=500&h=500&fit=crop", "Puffer Jacket - Front"), ("https://images.unsplash.com/photo-1487215078519-e21cc028cb29?w=500&h=500&fit=crop", "Puffer Jacket - Back"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Puffer Jacket - Detail") } },
            { "casual-button-up-shirt", new List<(string, string)> { ("https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=500&h=500&fit=crop", "Button-Up - Front"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Button-Up - Back"), ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "Button-Up - Buttons") } },
            { "led-smart-bulb-set", new List<(string, string)> { ("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "LED Smart Bulb Set - All Colors"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "LED Smart Bulb Set - Warm Light"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "LED Smart Bulb Set - Cool Light") } },
            { "stainless-steel-coffee-maker", new List<(string, string)> { ("https://images.unsplash.com/photo-1521017432531-fbd92d768814?w=500&h=500&fit=crop", "Coffee Maker - Stainless Steel"), ("https://images.unsplash.com/photo-1521017432531-fbd92d768814?w=500&h=500&fit=crop", "Coffee Maker - Side View"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Coffee Maker - In Use") } },
            { "modern-desk-lamp-led", new List<(string, string)> { ("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "LED Lamp - On"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "LED Lamp - Off"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "LED Lamp - Control") } },
            { "ceramic-plant-pot-set", new List<(string, string)> { ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Plant Pots - Set"), ("https://images.unsplash.com/photo-1521017432531-fbd92d768814?w=500&h=500&fit=crop", "Plant Pots - Close"), ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "Plant Pots - Arrangement") } },
            { "modern-area-rug-8x10", new List<(string, string)> { ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Area Rug - Full"), ("https://images.unsplash.com/photo-1487215078519-e21cc028cb29?w=500&h=500&fit=crop", "Area Rug - Pattern"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Area Rug - Texture") } },
            { "wooden-frame-wall-art", new List<(string, string)> { ("https://images.unsplash.com/photo-1595225476933-18e409d64f0d?w=500&h=500&fit=crop", "Wall Art - Display"), ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "Wall Art - Frame"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Wall Art - Detail") } },
            { "fabric-storage-basket", new List<(string, string)> { ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Storage Basket - Empty"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Storage Basket - Handles"), ("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "Storage Basket - Filled") } },
            { "smart-air-purifier", new List<(string, string)> { ("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "Air Purifier - Front"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Air Purifier - Side"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Air Purifier - Running") } }
        };
    }
}

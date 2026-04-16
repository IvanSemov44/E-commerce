using ECommerce.Catalog.Domain.Aggregates.Product;
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
        if (await context.Products.AnyAsync(cancellationToken))
            return;

        var electronics = await context.Categories.FirstOrDefaultAsync(c => EF.Property<string>(c, "Slug") == "electronics", cancellationToken);
        var fashion = await context.Categories.FirstOrDefaultAsync(c => EF.Property<string>(c, "Slug") == "fashion", cancellationToken);
        var home = await context.Categories.FirstOrDefaultAsync(c => EF.Property<string>(c, "Slug") == "home-garden", cancellationToken);

        if (electronics is null || fashion is null || home is null)
            throw new InvalidOperationException("Categories must be seeded before products.");

        var imageMap = CreateProductImageMap();

        foreach (var product in CreateProducts(electronics.Id, fashion.Id, home.Id))
        {
            if (imageMap.TryGetValue(product.Slug.Value, out var images))
            {
                foreach (var (url, alt) in images)
                    product.AddImage(url, alt);
            }
            else
            {
                product.AddImage(
                    "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=500&h=500&fit=crop",
                    product.Name.Value);
            }

            context.Products.Add(product);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static List<Product> CreateProducts(Guid electronicsId, Guid fashionId, Guid homeId)
    {
        return
        [
            Make("Wireless Headphones Pro",      299.99m, electronicsId, "WHP-001", "wireless-headphones-pro",       "Premium wireless headphones with noise cancellation and 30-hour battery life", compareAt: 399.99m, stock: 50,  featured: true),
            Make("4K Webcam Ultra",              149.99m, electronicsId, "WC-001",  "4k-webcam-ultra",               "4K resolution webcam with auto-focus and built-in microphone",                compareAt: 199.99m, stock: 35,  featured: true),
            Make("Mechanical Keyboard RGB",      129.99m, electronicsId, "KB-001",  "mechanical-keyboard-rgb",       "Gaming mechanical keyboard with RGB lighting and custom switches",            compareAt: 179.99m, stock: 42,  featured: true),
            Make("Portable Wireless Speaker",    69.99m,  electronicsId, "SPK-001", "portable-wireless-speaker",     "Waterproof Bluetooth speaker with 20-hour battery",                           compareAt: 99.99m,  stock: 90,  featured: true),
            Make("USB-C Hub 7-in-1",             49.99m,  electronicsId, "HUB-001", "usb-c-hub-7-in-1",             "Multi-port USB-C hub with HDMI, USB 3.0, and SD card reader",                compareAt: 69.99m,  stock: 100, featured: false),
            Make("4K Monitor 27-inch",           399.99m, electronicsId, "MON-001", "4k-monitor-27-inch",            "27-inch 4K IPS monitor with USB-C and 60Hz refresh rate",                    compareAt: 499.99m, stock: 25,  featured: true),
            Make("Wireless Ergonomic Mouse",     39.99m,  electronicsId, "MOU-001", "wireless-ergonomic-mouse",      "Ergonomic wireless mouse with silent clicks and 12-month battery",           compareAt: 59.99m,  stock: 120, featured: false),
            Make("Laptop Stand Adjustable",      34.99m,  electronicsId, "STA-001", "laptop-stand-adjustable",       "Aluminum adjustable laptop stand compatible with all laptops",               compareAt: 49.99m,  stock: 85,  featured: false),
            Make("USB Condenser Microphone",     79.99m,  electronicsId, "MIC-001", "usb-condenser-microphone",      "Studio-quality USB condenser microphone with pop filter",                    compareAt: 119.99m, stock: 50,  featured: false),
            Make("Monitor Light Bar",            59.99m,  electronicsId, "LIT-001", "monitor-light-bar",             "Auto-dimming monitor light bar with USB power and no glare",                 compareAt: 89.99m,  stock: 65,  featured: false),
            Make("External SSD 2TB",             249.99m, electronicsId, "SSD-001", "external-ssd-2tb",              "Fast 2TB external SSD with USB-C and 550MB/s speed",                        compareAt: 299.99m, stock: 40,  featured: true),
            Make("Graphics Drawing Tablet",      89.99m,  electronicsId, "TAB-001", "graphics-drawing-tablet",       "10x6 inch graphics tablet with 8192 pressure levels",                       compareAt: 129.99m, stock: 35,  featured: false),
            Make("Premium Cotton T-Shirt",       29.99m,  fashionId,     "TSH-001", "premium-cotton-tshirt",         "100% organic cotton t-shirt, soft and durable",                              compareAt: 39.99m,  stock: 200, featured: false),
            Make("Classic Denim Jeans",          59.99m,  fashionId,     "JNS-001", "classic-denim-jeans",           "Timeless blue denim jeans with perfect fit",                                 compareAt: 79.99m,  stock: 150, featured: false),
            Make("Premium Leather Belt",         44.99m,  fashionId,     "BLT-001", "premium-leather-belt",          "Genuine leather belt with stainless steel buckle",                           compareAt: 64.99m,  stock: 110, featured: false),
            Make("Merino Wool Sweater",          79.99m,  fashionId,     "SWE-001", "merino-wool-sweater",           "Soft merino wool sweater, perfect for all seasons",                          compareAt: 109.99m, stock: 75,  featured: false),
            Make("Running Shoes Pro",            119.99m, fashionId,     "SHO-001", "running-shoes-pro",             "High-performance running shoes with memory foam",                            compareAt: 159.99m, stock: 95,  featured: true),
            Make("Athletic Performance Shorts",  39.99m,  fashionId,     "SHT-001", "athletic-performance-shorts",   "Moisture-wicking athletic shorts for training and sports",                   compareAt: 54.99m,  stock: 140, featured: false),
            Make("Winter Puffer Jacket",         149.99m, fashionId,     "JAC-001", "winter-puffer-jacket",          "Insulated winter puffer jacket with water-resistant shell",                  compareAt: 199.99m, stock: 60,  featured: true),
            Make("Casual Button-Up Shirt",       49.99m,  fashionId,     "BSH-001", "casual-button-up-shirt",        "100% cotton casual button-up shirt in multiple colors",                      compareAt: 69.99m,  stock: 130, featured: false),
            Make("LED Smart Bulb Set",           79.99m,  homeId,        "LED-001", "led-smart-bulb-set",            "Set of 4 smart LED bulbs with WiFi control and 16 million colors",           compareAt: 119.99m, stock: 80,  featured: true),
            Make("Stainless Steel Coffee Maker", 89.99m,  homeId,        "CF-001",  "stainless-steel-coffee-maker",  "12-cup programmable coffee maker with thermal carafe",                       compareAt: 129.99m, stock: 60,  featured: false),
            Make("Modern Desk Lamp LED",         54.99m,  homeId,        "LAM-001", "modern-desk-lamp-led",          "Touch-control LED desk lamp with brightness adjustment",                     compareAt: 79.99m,  stock: 75,  featured: false),
            Make("Ceramic Plant Pot Set",        44.99m,  homeId,        "POT-001", "ceramic-plant-pot-set",         "Set of 3 decorative ceramic plant pots with drainage holes",                 compareAt: 64.99m,  stock: 85,  featured: false),
            Make("Modern Area Rug 8x10",         199.99m, homeId,        "RUG-001", "modern-area-rug-8x10",          "Soft wool blend area rug with modern geometric pattern",                     compareAt: 279.99m, stock: 30,  featured: true),
            Make("Wooden Frame Wall Art",        69.99m,  homeId,        "ART-001", "wooden-frame-wall-art",         "Set of 3 wooden frame wall art pieces with botanical prints",                 compareAt: 99.99m,  stock: 50,  featured: false),
            Make("Fabric Storage Basket",        34.99m,  homeId,        "BAS-001", "fabric-storage-basket",         "Durable fabric storage basket with handles for organizing",                  compareAt: 49.99m,  stock: 105, featured: false),
            Make("Smart Air Purifier",           179.99m, homeId,        "PUR-001", "smart-air-purifier",            "Smart air purifier with WiFi control and HEPA filter",                       compareAt: 249.99m, stock: 45,  featured: true),
        ];
    }

    private static Product Make(
        string name, decimal price, Guid categoryId, string sku, string slug,
        string description, decimal compareAt, int stock, bool featured)
    {
        var product = Product.Create(name, price, "USD", categoryId, sku, slug, description, compareAt)
            .GetDataOrThrow();

        product.Activate();
        product.SetStock(stock);
        if (featured) product.Feature();

        return product;
    }

    private static Dictionary<string, List<(string Url, string Alt)>> CreateProductImageMap()
    {
        return new Dictionary<string, List<(string Url, string Alt)>>
        {
            { "wireless-headphones-pro",      [("https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=500&h=500&fit=crop", "Wireless Headphones Pro - Front View"), ("https://images.unsplash.com/photo-1487215078519-e21cc028cb29?w=500&h=500&fit=crop", "Wireless Headphones Pro - Side View"), ("https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=500&h=500&fit=crop", "Wireless Headphones Pro - In Use")] },
            { "4k-webcam-ultra",              [("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "4K Webcam Ultra - Front View"), ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "4K Webcam Ultra - Top View"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "4K Webcam Ultra - Detail")] },
            { "mechanical-keyboard-rgb",      [("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "Mechanical Keyboard RGB - Full View"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Mechanical Keyboard RGB - RGB Lights"), ("https://images.unsplash.com/photo-1521017432531-fbd92d768814?w=500&h=500&fit=crop", "Mechanical Keyboard RGB - Detail")] },
            { "portable-wireless-speaker",    [("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "Wireless Speaker - Blue"), ("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "Wireless Speaker - Detail"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Wireless Speaker - Portable")] },
            { "usb-c-hub-7-in-1",             [("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "USB-C Hub - Full View"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "USB-C Hub - Ports"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "USB-C Hub - Detail")] },
            { "4k-monitor-27-inch",           [("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "4K Monitor - Front"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "4K Monitor - Side View"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "4K Monitor - Stand")] },
            { "wireless-ergonomic-mouse",     [("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Ergonomic Mouse - Top"), ("https://images.unsplash.com/photo-1521017432531-fbd92d768814?w=500&h=500&fit=crop", "Ergonomic Mouse - Side"), ("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "Ergonomic Mouse - Bottom")] },
            { "laptop-stand-adjustable",      [("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Laptop Stand - Folded"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Laptop Stand - Extended"), ("https://images.unsplash.com/photo-1521017432531-fbd92d768814?w=500&h=500&fit=crop", "Laptop Stand - With Laptop")] },
            { "usb-condenser-microphone",     [("https://images.unsplash.com/photo-1487215078519-e21cc028cb29?w=500&h=500&fit=crop", "USB Microphone - Front"), ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "USB Microphone - Stand"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "USB Microphone - Detail")] },
            { "monitor-light-bar",            [("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "Light Bar - On Monitor"), ("https://images.unsplash.com/photo-1487215078519-e21cc028cb29?w=500&h=500&fit=crop", "Light Bar - Side View"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Light Bar - In Use")] },
            { "external-ssd-2tb",             [("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "External SSD - Front"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "External SSD - Cable"), ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "External SSD - Size")] },
            { "graphics-drawing-tablet",      [("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "Drawing Tablet - Front"), ("https://images.unsplash.com/photo-1521017432531-fbd92d768814?w=500&h=500&fit=crop", "Drawing Tablet - Pen"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Drawing Tablet - With Stylus")] },
            { "premium-cotton-tshirt",        [("https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=500&h=500&fit=crop", "Premium Cotton T-Shirt - White"), ("https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=500&h=500&fit=crop", "Premium Cotton T-Shirt - Folded"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Premium Cotton T-Shirt - Worn")] },
            { "classic-denim-jeans",          [("https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=500&h=500&fit=crop", "Classic Denim Jeans - Front"), ("https://images.unsplash.com/photo-1487215078519-e21cc028cb29?w=500&h=500&fit=crop", "Classic Denim Jeans - Back"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Classic Denim Jeans - Detail")] },
            { "premium-leather-belt",         [("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Leather Belt - Laid"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Leather Belt - Buckle"), ("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "Leather Belt - Worn")] },
            { "merino-wool-sweater",          [("https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=500&h=500&fit=crop", "Wool Sweater - Front"), ("https://images.unsplash.com/photo-1487215078519-e21cc028cb29?w=500&h=500&fit=crop", "Wool Sweater - Back"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Wool Sweater - Folded")] },
            { "running-shoes-pro",            [("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "Running Shoes - Side"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Running Shoes - Top"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Running Shoes - Front")] },
            { "athletic-performance-shorts",  [("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Athletic Shorts - Front"), ("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "Athletic Shorts - Back"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Athletic Shorts - Waist")] },
            { "winter-puffer-jacket",         [("https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=500&h=500&fit=crop", "Puffer Jacket - Front"), ("https://images.unsplash.com/photo-1487215078519-e21cc028cb29?w=500&h=500&fit=crop", "Puffer Jacket - Back"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Puffer Jacket - Detail")] },
            { "casual-button-up-shirt",       [("https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=500&h=500&fit=crop", "Button-Up - Front"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Button-Up - Back"), ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "Button-Up - Buttons")] },
            { "led-smart-bulb-set",           [("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "LED Smart Bulb Set - All Colors"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "LED Smart Bulb Set - Warm Light"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "LED Smart Bulb Set - Cool Light")] },
            { "stainless-steel-coffee-maker", [("https://images.unsplash.com/photo-1521017432531-fbd92d768814?w=500&h=500&fit=crop", "Coffee Maker - Stainless Steel"), ("https://images.unsplash.com/photo-1521017432531-fbd92d768814?w=500&h=500&fit=crop", "Coffee Maker - Side View"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Coffee Maker - In Use")] },
            { "modern-desk-lamp-led",         [("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "LED Lamp - On"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "LED Lamp - Off"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "LED Lamp - Control")] },
            { "ceramic-plant-pot-set",        [("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Plant Pots - Set"), ("https://images.unsplash.com/photo-1521017432531-fbd92d768814?w=500&h=500&fit=crop", "Plant Pots - Close"), ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "Plant Pots - Arrangement")] },
            { "modern-area-rug-8x10",         [("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Area Rug - Full"), ("https://images.unsplash.com/photo-1487215078519-e21cc028cb29?w=500&h=500&fit=crop", "Area Rug - Pattern"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Area Rug - Texture")] },
            { "wooden-frame-wall-art",        [("https://images.unsplash.com/photo-1595225476933-18e409d64f0d?w=500&h=500&fit=crop", "Wall Art - Display"), ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=500&h=500&fit=crop", "Wall Art - Frame"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Wall Art - Detail")] },
            { "fabric-storage-basket",        [("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Storage Basket - Empty"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Storage Basket - Handles"), ("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "Storage Basket - Filled")] },
            { "smart-air-purifier",           [("https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop", "Air Purifier - Front"), ("https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&h=500&fit=crop", "Air Purifier - Side"), ("https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=500&h=500&fit=crop", "Air Purifier - Running")] },
        };
    }
}

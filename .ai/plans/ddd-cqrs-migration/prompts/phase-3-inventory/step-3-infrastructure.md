# Phase 3, Step 3: Inventory Infrastructure Project

**Prerequisite**: Step 2 (`ECommerce.Inventory.Application`) is complete and `dotnet build` passes.

---

## Task: Create ECommerce.Inventory.Infrastructure Project

### 1. Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Inventory.Infrastructure -f net10.0 -o Inventory/ECommerce.Inventory.Infrastructure
dotnet sln ../../ECommerce.sln add Inventory/ECommerce.Inventory.Infrastructure/ECommerce.Inventory.Infrastructure.csproj

dotnet add Inventory/ECommerce.Inventory.Infrastructure/ECommerce.Inventory.Infrastructure.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add Inventory/ECommerce.Inventory.Infrastructure/ECommerce.Inventory.Infrastructure.csproj \
    reference Inventory/ECommerce.Inventory.Domain/ECommerce.Inventory.Domain.csproj
dotnet add Inventory/ECommerce.Inventory.Infrastructure/ECommerce.Inventory.Infrastructure.csproj \
    reference Inventory/ECommerce.Inventory.Application/ECommerce.Inventory.Application.csproj
dotnet add Inventory/ECommerce.Inventory.Infrastructure/ECommerce.Inventory.Infrastructure.csproj \
    reference ECommerce.Infrastructure/ECommerce.Infrastructure.csproj  # for AppDbContext

dotnet add Inventory/ECommerce.Inventory.Infrastructure/ECommerce.Inventory.Infrastructure.csproj \
    package Microsoft.EntityFrameworkCore

rm Inventory/ECommerce.Inventory.Infrastructure/Class1.cs
```

### 2. Create EF Core configurations

**File: `Inventory/ECommerce.Inventory.Infrastructure/Persistence/Configurations/InventoryItemConfiguration.cs`**

```csharp
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Inventory.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");
        builder.HasKey(i => i.Id);

        // StockLevel value object — owned entity, stored as columns in the same table
        builder.OwnsOne(i => i.Stock, stock =>
        {
            stock.Property(s => s.Quantity)
                 .HasColumnName("Quantity")
                 .IsRequired();
        });

        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.LowStockThreshold).IsRequired();
        builder.Property(i => i.TrackInventory).IsRequired();

        // Index for the most common lookup: by ProductId
        builder.HasIndex(i => i.ProductId).IsUnique();

        // InventoryLog child collection — mapped via backing field name.
        // Use Include("_log") in queries (not Include(i => i.Log)) to match this registration.
        builder.HasMany<InventoryLog>("_log")
               .WithOne()
               .HasForeignKey(l => l.InventoryItemId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**File: `Inventory/ECommerce.Inventory.Infrastructure/Persistence/Configurations/InventoryLogConfiguration.cs`**

```csharp
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Inventory.Infrastructure.Persistence.Configurations;

public class InventoryLogConfiguration : IEntityTypeConfiguration<InventoryLog>
{
    public void Configure(EntityTypeBuilder<InventoryLog> builder)
    {
        builder.ToTable("InventoryLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.InventoryItemId).IsRequired();
        builder.Property(l => l.Delta).IsRequired();
        builder.Property(l => l.Reason).IsRequired().HasMaxLength(500);
        builder.Property(l => l.StockAfter).IsRequired();
        builder.Property(l => l.OccurredAt).IsRequired();
    }
}
```

> **Note**: `InventoryLog` is `public sealed class` in Domain — the type must be public because Application reads `InventoryItem.Log` entries. The `internal static Create` factory ensures only `InventoryItem` can create log entries. EF Core materializes via the `private` parameterless constructor using reflection (no `InternalsVisibleTo` required for this).
> The navigation is registered via the backing field name `"_log"` — use `Include("_log")` in all repository queries (not the lambda form `Include(i => i.Log)`).

### 3. Add configurations to AppDbContext

In the existing `AppDbContext.cs` (in `ECommerce.Infrastructure`), add the two DbSets and apply configurations.

Look at how `CatalogItemConfiguration` and `UserConfiguration` were added in Phases 1–2 and follow the same pattern:

```csharp
// In AppDbContext.cs — add DbSets:
public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

// In OnModelCreating — apply configuration:
modelBuilder.ApplyConfiguration(new InventoryItemConfiguration());
modelBuilder.ApplyConfiguration(new InventoryLogConfiguration());
```

### 4. Implement IInventoryItemRepository

**File: `Inventory/ECommerce.Inventory.Infrastructure/Persistence/Repositories/InventoryItemRepository.cs`**

```csharp
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Domain.Interfaces;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Infrastructure.Persistence.Repositories;

public class InventoryItemRepository(AppDbContext _db) : IInventoryItemRepository
{
    public async Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.InventoryItems
            .Include("_log")
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => await _db.InventoryItems
            .Include("_log")
            .FirstOrDefaultAsync(i => i.ProductId == productId, ct);

    public async Task<List<InventoryItem>> GetAllAsync(CancellationToken ct = default)
        => await _db.InventoryItems
            .Include("_log")
            .ToListAsync(ct);

    public async Task<List<InventoryItem>> GetLowStockAsync(int? thresholdOverride = null, CancellationToken ct = default)
        => await _db.InventoryItems
            .Include("_log")
            .Where(i => i.Stock.Quantity <= (thresholdOverride ?? i.LowStockThreshold))
            .ToListAsync(ct);

    public async Task AddAsync(InventoryItem item, CancellationToken ct = default)
        => await _db.InventoryItems.AddAsync(item, ct);
}
```

> **Note**: Use `Include("_log")` with the backing field name (string). EF Core requires this when the navigation property is private/internal.

### 5. Implement IEmailService (stub)

For now, log a warning instead of sending real email. Replace with SendGrid/SMTP in production.

**File: `Inventory/ECommerce.Inventory.Infrastructure/Services/EmailService.cs`**

```csharp
using ECommerce.Inventory.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Inventory.Infrastructure.Services;

public class EmailService(ILogger<EmailService> _logger) : IEmailService
{
    public Task SendLowStockAlertAsync(Guid productId, int currentStock, int threshold, CancellationToken ct)
    {
        // TODO: Replace with real email provider (SendGrid, SMTP) when ready
        _logger.LogWarning(
            "LOW STOCK ALERT: ProductId={ProductId} has {CurrentStock} units (threshold={Threshold})",
            productId, currentStock, threshold);

        return Task.CompletedTask;
    }
}
```

### 6. Create DI registration

**File: `Inventory/ECommerce.Inventory.Infrastructure/DependencyInjection.cs`**

```csharp
using ECommerce.Inventory.Application.Interfaces;
using ECommerce.Inventory.Domain.Interfaces;
using ECommerce.Inventory.Infrastructure.Persistence.Repositories;
using ECommerce.Inventory.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
        services.AddScoped<IEmailService, EmailService>();

        // Register MediatR handlers from Application project
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(ECommerce.Inventory.Application.Commands.IncreaseStock.IncreaseStockCommand).Assembly));

        return services;
    }
}
```

### 7. Wire up in Program.cs / Startup

Add to `ECommerce.API` the reference to Infrastructure and call the DI extension:

```bash
dotnet add ECommerce.API/ECommerce.API.csproj \
    reference Inventory/ECommerce.Inventory.Infrastructure/ECommerce.Inventory.Infrastructure.csproj
```

In `Program.cs`:
```csharp
builder.Services.AddInventoryInfrastructure();
```

### 8. Create EF Core data migration

**This is the critical migration.** It:
1. Creates the `InventoryItems` and `InventoryLogs` tables
2. Seeds `InventoryItem` for every existing `Product` (copies `StockQuantity` and `LowStockThreshold`)
3. Drops `StockQuantity` and `LowStockThreshold` from `Products`

```bash
cd src/backend
dotnet ef migrations add Phase3_ExtractInventoryItem \
    --project ECommerce.Infrastructure/ECommerce.Infrastructure.csproj \
    --startup-project ECommerce.API/ECommerce.API.csproj
```

After EF generates the migration scaffold, **manually add the data seed** inside the `Up` method:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // EF auto-generates: CreateTable("InventoryItems"), CreateTable("InventoryLogs")

    // Seed InventoryItem for every existing Product (migrate stock data)
    migrationBuilder.Sql(@"
        INSERT INTO ""InventoryItems"" (""Id"", ""ProductId"", ""Quantity"", ""LowStockThreshold"", ""TrackInventory"")
        SELECT
            gen_random_uuid(),
            ""Id"",
            COALESCE(""StockQuantity"", 0),
            COALESCE(""LowStockThreshold"", 10),
            true
        FROM ""Products""
    ");

    // Remove stock fields from Products — they now live in InventoryItems
    migrationBuilder.DropColumn("StockQuantity", "Products");
    migrationBuilder.DropColumn("LowStockThreshold", "Products");
}
```

> **Warning**: This migration is irreversible in production. Run it on staging first. Verify the row count: `SELECT COUNT(*) FROM "InventoryItems"` must equal `SELECT COUNT(*) FROM "Products"`.

### 9. Update Catalog GetProductsQuery for real stock status

The Catalog `GetProductsQuery` from Phase 1 hardcoded `InStock = true`. Now that stock lives in `InventoryItems`, join to get real availability.

Find `GetProductsQueryHandler.cs` in `ECommerce.Catalog.Application` and update the projection:

```csharp
// Before (Phase 1 placeholder):
InStock = true,

// After (Phase 3 — join InventoryItems):
InStock = context.InventoryItems.Any(i => i.ProductId == p.Id && i.Stock.Quantity > 0),
```

If the Catalog Application project doesn't directly reference `AppDbContext`, pass an `IInventoryStockReader` interface instead (define it in SharedKernel or a contracts project). The simplest approach is a direct `DbContext` join if both are in the same database.

### 10. Update TestWebApplicationFactory to seed InventoryItems

**This is required for integration tests (InMemory DB) to pass after cutover.** Without it, every endpoint that calls `GetByProductIdAsync` will return 404 in the InMemory test environment.

Find `TestWebApplicationFactory.cs` (in `ECommerce.Tests/`) and add `InventoryItem` seeding alongside the existing product seed:

```csharp
// In the seed method — add after seeding Products:
var inventoryItem = InventoryItem.Create(
    productId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
    initialQuantity: 100,
    lowStockThreshold: 10).GetDataOrThrow();

context.InventoryItems.Add(inventoryItem);
await context.SaveChangesAsync();
```

> The `InventoryItems` DbSet must exist on `AppDbContext` (added in step 3 above) before this compiles.

### 11. Verify

```bash
cd src/backend
dotnet build  # Entire solution builds
dotnet test   # All existing tests still pass
```

---

## Acceptance Criteria

- [ ] `ECommerce.Inventory.Infrastructure` project created and added to solution
- [ ] `InventoryItemConfiguration` maps `StockLevel` as owned entity with `Quantity` column
- [ ] `InventoryLogConfiguration` maps `InventoryLog` with all required fields
- [ ] EF configurations applied in `AppDbContext.OnModelCreating`
- [ ] `InventoryItemRepository` implements all 5 methods from `IInventoryItemRepository`
- [ ] `Include("_log")` used (string-based) for backing field navigation
- [ ] `EmailService` stub logs warning — does NOT throw
- [ ] `DependencyInjection.cs` registers repo, email service, and MediatR handlers
- [ ] EF migration created: `Phase3_ExtractInventoryItem`
- [ ] Migration data seed copies `Products.StockQuantity` → `InventoryItems.Quantity`
- [ ] Migration drops `StockQuantity` and `LowStockThreshold` from `Products`
- [ ] Catalog `GetProductsQuery` updated to join real stock data from `InventoryItems`
- [ ] **`TestWebApplicationFactory` updated to seed `InventoryItem` for the seeded product** — integration tests must not 404
- [ ] `dotnet build` and `dotnet test` both pass after infrastructure is wired up

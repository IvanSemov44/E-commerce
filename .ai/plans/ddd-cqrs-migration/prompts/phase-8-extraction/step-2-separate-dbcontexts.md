# Phase 8, Step 2: Separate DbContexts (Logical Separation)

**Prerequisite**: Step 1 contracts complete.

Split the shared `AppDbContext` into one `DbContext` per bounded context. All contexts use the **same PostgreSQL database** but **separate schemas** as an intermediate step before full database separation.

---

## Architecture

Instead of:
```
AppDbContext (owns all tables: Products, Orders, Inventory, PromoCodes, etc.)
```

Create:
```
CatalogDbContext (owns: Products, Categories)
InventoryDbContext (owns: InventoryItems)
OrderingDbContext (owns: Orders, OrderLineItems)
PromotionsDbContext (owns: PromoCodes)
ReviewsDbContext (owns: Reviews)
ShoppingDbContext (owns: Carts, WishlistItems)
```

---

## Task 1: Use PostgreSQL Schemas

Add schema prefix to each DbContext table mappings. In PostgreSQL, this separates tables logically without splitting databases:

```csharp
// CatalogDbContext
modelBuilder.Entity<Product>().ToTable("products", schema: "catalog");
modelBuilder.Entity<Category>().ToTable("categories", schema: "catalog");

// OrderingDbContext
modelBuilder.Entity<Order>().ToTable("orders", schema: "ordering");
modelBuilder.Entity<OrderLineItem>().ToTable("order_line_items", schema: "ordering");

// InventoryDbContext
modelBuilder.Entity<InventoryItem>().ToTable("inventory_items", schema: "inventory");
```

---

## Task 2: Remove Cross-Context Navigation Properties

**Remove** ForeignKey relationships that cross contexts:

```csharp
// REMOVE THIS (OrderingDbContext should not ForeignKey Inventory)
public class Order
{
    public Guid InventoryItemId { get; set; }
    // public InventoryItem InventoryItem { get; set; } ← DELETE THIS
}
```

Store only **IDs** (not references):
```csharp
public class Order
{
    public Guid[] ProductIds { get; set; } // Just IDs, no FK or navigation
    public List<OrderLineItem> Items { get; set; }
}
```

**Denormalize** product data at the time of ordering:
```csharp
// In OrderLineItem, store a copy of product info
public class OrderLineItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } ← Copied from Catalog at order time
    public decimal ProductPrice { get; set; } ← Copied from Catalog at order time
    public int Quantity { get; set; }
}
```

---

## Task 3: Create Schema-Specific DbContexts

**File: `src/backend/ECommerce.Infrastructure/Data/CatalogDbContext.cs`**

```csharp
using ECommerce.Catalog.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Data;

public class CatalogDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
```

**File: `src/backend/ECommerce.Infrastructure/Data/OrderingDbContext.cs`**

```csharp
using ECommerce.Orders.Domain.Aggregates.Order;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Data;

public class OrderingDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }

    public OrderingDbContext(DbContextOptions<OrderingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ordering");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
    }
}
```

Repeat for: `InventoryDbContext`, `PromotionsDbContext`, `ReviewsDbContext`, `ShoppingDbContext`

---

## Task 4: Register DbContexts in DI

**In `Program.cs`:**

```csharp
// Register all DbContexts with same connection string
var connString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<CatalogDbContext>(o => o.UseNpgsql(connString));
builder.Services.AddDbContext<InventoryDbContext>(o => o.UseNpgsql(connString));
builder.Services.AddDbContext<OrderingDbContext>(o => o.UseNpgsql(connString));
builder.Services.AddDbContext<PromotionsDbContext>(o => o.UseNpgsql(connString));
builder.Services.AddDbContext<ReviewsDbContext>(o => o.UseNpgsql(connString));
builder.Services.AddDbContext<ShoppingDbContext>(o => o.UseNpgsql(connString));

// Remove old: builder.Services.AddDbContext<AppDbContext>(...);
```

---

## Task 5: Create EF Migrations

```bash
cd src/backend

# Create migrations for each schema
dotnet ef migrations add "Split_Catalog_Schema" -p ECommerce.Infrastructure -s ECommerce.API -c CatalogDbContext
dotnet ef migrations add "Split_Inventory_Schema" -p ECommerce.Infrastructure -s ECommerce.API -c InventoryDbContext
dotnet ef migrations add "Split_Ordering_Schema" -p ECommerce.Infrastructure -s ECommerce.API -c OrderingDbContext
dotnet ef migrations add "Split_Promotions_Schema" -p ECommerce.Infrastructure -s ECommerce.API -c PromotionsDbContext
dotnet ef migrations add "Split_Reviews_Schema" -p ECommerce.Infrastructure -s ECommerce.API -c ReviewsDbContext
dotnet ef migrations add "Split_Shopping_Schema" -p ECommerce.Infrastructure -s ECommerce.API -c ShoppingDbContext

# Apply all migrations
for context in CatalogDbContext InventoryDbContext OrderingDbContext PromotionsDbContext ReviewsDbContext ShoppingDbContext; do
  dotnet ef database update -p ECommerce.Infrastructure -s ECommerce.API -c $context
done
```

Verify in PostgreSQL:
```sql
SELECT schema_name FROM information_schema.schemata WHERE schema_name IN ('catalog', 'inventory', 'ordering', 'promotions', 'reviews', 'shopping');
-- Should return 6 rows
```

---

## Breaking Changes to Fix

1. **Repositories must use correct DbContext**:
   ```csharp
   // OLD
   public class ProductRepository : IProductRepository
   {
       private readonly AppDbContext _db; // ← Wrong
   }

   // NEW
   public class ProductRepository : IProductRepository
   {
       private readonly CatalogDbContext _db; // ← Correct
   }
   ```

2. **UnitOfWork must coordinate multiple DbContextes** (see step 3)

3. **Cross-context queries fail** (e.g., Order.Include(o => o.Product) will not compile) — must denormalize

---

## Acceptance Criteria

- [ ] `CatalogDbContext`, `InventoryDbContext`, `OrderingDbContext`, `PromotionsDbContext`, `ReviewsDbContext`, `ShoppingDbContext` created
- [ ] Each context maps only its own tables
- [ ] PostgreSQL schemas created: `catalog.`, `inventory.`, `ordering.`, etc.
- [ ] All cross-context foreign keys and navigation properties removed
- [ ] Denormalized data (ProductName, Price) added to OrderLineItem
- [ ] All migrations created and applied successfully
- [ ] `dotnet build` clean
- [ ] Existing tests still pass (may need schema prefixes adjusted)

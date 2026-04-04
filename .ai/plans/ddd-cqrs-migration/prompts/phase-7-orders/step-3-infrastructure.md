# Phase 7, Step 3: Infrastructure Project

**Prerequisite**: Steps 1 and 2 complete and building.

Create `ECommerce.Orders.Infrastructure` — EF Core configuration, repository implementation, and DI wiring.

---

## Task 1: Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Orders.Infrastructure -o ECommerce.Orders.Infrastructure
dotnet sln ECommerce.sln add ECommerce.Orders.Infrastructure/ECommerce.Orders.Infrastructure.csproj
dotnet add ECommerce.Orders.Infrastructure/ECommerce.Orders.Infrastructure.csproj reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add ECommerce.Orders.Infrastructure/ECommerce.Orders.Infrastructure.csproj reference ECommerce.Orders.Domain/ECommerce.Orders.Domain.csproj
dotnet add ECommerce.Orders.Infrastructure/ECommerce.Orders.Infrastructure.csproj reference ECommerce.Orders.Application/ECommerce.Orders.Application.csproj
dotnet add ECommerce.Orders.Infrastructure/ECommerce.Orders.Infrastructure.csproj reference ECommerce.Infrastructure/ECommerce.Infrastructure.csproj
dotnet add ECommerce.Orders.Infrastructure/ECommerce.Orders.Infrastructure.csproj package Microsoft.EntityFrameworkCore
rm ECommerce.Orders.Infrastructure/Class1.cs
```

---

## Task 2: EF Configuration

**File: `ECommerce.Orders.Infrastructure/Configurations/OrderConfiguration.cs`**

```csharp
using ECommerce.Orders.Domain.Aggregates.Order;
using ECommerce.Orders.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Orders.Infrastructure.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerId).IsRequired();

        builder.Property(o => o.OrderNumber)
            .HasColumnName("OrderNumber")
            .HasConversion(v => v.Value, v => OrderNumber.Reconstitute(v))
            .IsRequired();

        builder.Property(o => o.Status)
            .HasColumnName("Status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(o => o.Subtotal)
            .HasColumnName("Subtotal")
            .HasConversion(v => v.Amount, v => Money.Reconstitute(v))
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(o => o.Tax)
            .HasColumnName("Tax")
            .HasConversion(v => v.Amount, v => Money.Reconstitute(v))
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(o => o.ShippingCost)
            .HasColumnName("ShippingCost")
            .HasConversion(v => v.Amount, v => Money.Reconstitute(v))
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(o => o.Total)
            .HasColumnName("Total")
            .HasConversion(v => v.Amount, v => Money.Reconstitute(v))
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(o => o.TrackingNumber)
            .HasColumnName("TrackingNumber")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(o => o.CancellationReason)
            .HasColumnName("CancellationReason")
            .HasMaxLength(500)
            .IsRequired(false);

        // Concurrency token
        builder.Property(o => o.RowVersion)
            .HasColumnName("RowVersion")
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired();

        // Line items as owned collection
        builder.OwnsMany(o => o.Items, itemBuilder =>
        {
            itemBuilder.ToTable("OrderLineItems");
            itemBuilder.HasKey(i => i.Id);
            itemBuilder.Property(i => i.ProductId).IsRequired();
            itemBuilder.Property(i => i.Quantity)
                .HasColumnName("Quantity")
                .HasConversion(v => v.Value, v => Quantity.Reconstitute(v))
                .IsRequired();
            itemBuilder.Property(i => i.UnitPrice)
                .HasColumnName("UnitPrice")
                .HasConversion(v => v.Amount, v => Money.Reconstitute(v))
                .HasPrecision(18, 2)
                .IsRequired();
        });

        // Indexes
        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => new { o.CustomerId, o.CreatedAt });
    }
}
```

---

## Task 3: Register DbSet

Open `ECommerce.Infrastructure/Data/AppDbContext.cs` and add:

```csharp
// Phase 7 — Orders bounded context
public DbSet<ECommerce.Orders.Domain.Aggregates.Order.Order> Orders { get; set; }
```

Also register the EF configuration in `OnModelCreating`:
```csharp
modelBuilder.ApplyConfiguration(new ECommerce.Orders.Infrastructure.Configurations.OrderConfiguration());
```

Add the reference:
```bash
dotnet add src/backend/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj reference src/backend/ECommerce.Orders.Infrastructure/ECommerce.Orders.Infrastructure.csproj
```

---

## Task 4: Repository

**File: `ECommerce.Orders.Infrastructure/Repositories/OrderRepository.cs`**

```csharp
using ECommerce.Infrastructure.Data;
using ECommerce.Orders.Domain.Aggregates.Order;
using ECommerce.Orders.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Orders.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db) => _db = db;

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
        => _db.Orders.FirstOrDefaultAsync(o => o.OrderNumber.Value == orderNumber, ct);

    public async Task<(List<Order> Items, int TotalCount)> GetByCustomerAsync(
        Guid customerId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(List<Order> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? status, CancellationToken ct = default)
    {
        var query = _db.Orders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status.ToString() == status);
        }

        query = query.OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(List<Order> Items, int TotalCount)> GetPendingAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Orders
            .Where(o => o.Status.ToString() == "Pending")
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task UpsertAsync(Order order, CancellationToken ct = default)
    {
        var existing = await _db.Orders.FindAsync(new object[] { order.Id }, ct);
        if (existing is null)
            await _db.Orders.AddAsync(order, ct);
    }

    public async Task DeleteAsync(Order order, CancellationToken ct = default)
    {
        var existing = await _db.Orders.FindAsync(new object[] { order.Id }, ct);
        if (existing is not null)
            _db.Orders.Remove(existing);
    }
}
```

---

## Task 5: DependencyInjection

**File: `ECommerce.Orders.Infrastructure/DependencyInjection.cs`**

```csharp
using ECommerce.Orders.Application;
using ECommerce.Orders.Domain.Interfaces;
using ECommerce.Orders.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Orders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddOrdersApplication();
        return services;
    }
}
```

---

## Task 6: Wire up in API

1. Add project reference:
```bash
dotnet add src/backend/ECommerce.API/ECommerce.API.csproj reference src/backend/ECommerce.Orders.Infrastructure/ECommerce.Orders.Infrastructure.csproj
```

2. In `src/backend/ECommerce.API/Program.cs`, add before `builder.Build()`:
```csharp
builder.Services.AddOrdersInfrastructure();
```

---

## Task 7: EF Migration

```bash
cd src/backend
dotnet ef migrations add "Add_Orders_Tables" -p ECommerce.Infrastructure -s ECommerce.API
dotnet ef database update -p ECommerce.Infrastructure -s ECommerce.API
```

Verify tables created:
```sql
SELECT * FROM sys.tables WHERE name IN ('Orders', 'OrderLineItems');
-- Expected: two rows
```

---

## Acceptance Criteria

- [ ] Project builds with zero errors
- [ ] `AppDbContext` has `Orders` DbSet
- [ ] `OrderConfiguration` applied in `OnModelCreating`
- [ ] EF migration creates `Orders` and `OrderLineItems` tables
- [ ] Value converters working: OrderNumber, Money, Quantity
- [ ] Owned collection `Items` mapped to separate table
- [ ] Indexes on CustomerId, OrderNumber (unique), Status, (CustomerId, CreatedAt)
- [ ] `RowVersion` column added as concurrency token
- [ ] App boots and `GET /api/orders/{id}` returns 404 for unknown ID
- [ ] `IOrderRepository` → `OrderRepository` registered as Scoped

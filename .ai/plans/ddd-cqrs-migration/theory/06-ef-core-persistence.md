# EF Core Persistence of Value Objects

**Read this after `04-value-types-and-dtos.md`.**

The domain model uses private constructors, read-only properties, and factory methods. EF Core wants public constructors and settable properties. This document shows how to bridge that gap without compromising the domain model.

---

## The Core Tension

EF Core was designed around anemic models:

```csharp
// EF Core wants this:
public class Product
{
    public Guid Id { get; set; }          // public setter
    public string Name { get; set; }      // public setter
    public decimal Price { get; set; }    // public setter
}
```

DDD requires this:

```csharp
// DDD requires this:
public class Product : AggregateRoot
{
    public Guid Id { get; private set; }
    public ProductName Name { get; private set; }
    public Money Price { get; private set; }

    private Product() { }  // required by EF, but private
    public static Product Create(...) { ... }
}
```

EF Core supports this. You just need to know the right configurations.

---

## Rule: Record Value Objects Use Value Converters

A `record` value object is a **single-value wrapper** around a primitive. Email wraps a string. Slug wraps a string. Quantity wraps an int. Sku wraps a string.

For these, use **value converters** (`HasConversion`). EF Core stores the primitive directly in the parent table's column. Clean, flat, no extra tables.

```csharp
// Email record value object
public record Email
{
    public string Value { get; }
    private Email(string value) => Value = value;
    public static Email Create(string raw) { ... }
}
```

```csharp
// EF Core configuration — store as varchar, not as owned sub-object
builder.Property(u => u.Email)
    .HasConversion(
        email => email.Value,               // domain → DB: extract the string
        value => Email.Create(value))       // DB → domain: reconstruct via factory
    .HasMaxLength(256)
    .IsRequired();
```

**Why not `OwnsOne()` for records?**

`OwnsOne()` creates shadow columns named `Product_Email_Value` or a separate table. For a single-property wrapper this is:
- Ugly column names
- Extra JOIN or shadow properties
- EF quirks when the value object is nullable (owned entities cannot be null in EF Core 6-)
- Private constructors fight with EF's owned entity materialization

`HasConversion` is clean: one column, one value, no navigation.

---

## Rule: Multi-Property Value Objects Use Owned Entities

A `class : ValueObject` with multiple properties cannot be flattened to a single column. Use `OwnsOne()` and name the columns explicitly.

```csharp
// Money value object — two properties
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency) { ... }
    public static Money Create(decimal amount, string currency) { ... }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

```csharp
// EF Core configuration — own the two columns inline in Product table
builder.OwnsOne(p => p.Price, money =>
{
    money.Property(m => m.Amount)
        .HasColumnName("Price")        // ← explicit column name, not "Price_Amount"
        .HasPrecision(18, 4)
        .IsRequired();

    money.Property(m => m.Currency)
        .HasColumnName("PriceCurrency")  // ← explicit, not "Price_Currency"
        .HasMaxLength(3)
        .IsRequired();
});

// CompareAtPrice is optional — Money can be null here
builder.OwnsOne(p => p.CompareAtPrice, money =>
{
    money.Property(m => m.Amount).HasColumnName("CompareAtPrice").HasPrecision(18, 4);
    money.Property(m => m.Currency).HasColumnName("CompareAtPriceCurrency").HasMaxLength(3);
});
```

**Always name columns explicitly.** EF Core's default generates `Price_Amount` and `Price_Currency`. Those underscores leak EF's internal navigation model into your database schema. Own the naming.

---

## EF Core's Private Constructor Requirement

EF Core needs to instantiate entities. It uses the parameterless constructor. Make it `private` — EF Core can still call it via reflection:

```csharp
public class Product : AggregateRoot
{
    private Product() { }  // EF Core uses this. Your code uses Create().

    public static Product Create(...) { ... }
}
```

For value objects used via `OwnsOne`, EF Core also needs a way to construct them. Solutions:

**Option A**: Private parameterless constructor (same pattern)
```csharp
public class Money : ValueObject
{
    private Money() { }  // EF needs this for OwnsOne materialization
    private Money(decimal amount, string currency) { Amount = amount; Currency = currency; }
    public static Money Create(decimal amount, string currency) { ... }
}
```

**Option B**: Use value converters (sidesteps the issue entirely for single-property objects)

---

## Private Setters for EF Core

EF Core can set `private set` properties directly. Use this for aggregate properties:

```csharp
public class Product : AggregateRoot
{
    public ProductName Name { get; private set; }  // ← private set, not init
    public Money Price { get; private set; }
    public ProductStatus Status { get; private set; }
}
```

Why `private set` not `init`? Because EF Core needs to set properties when materializing from the database (after the object is constructed). `init`-only properties can only be set during object initialization, which breaks EF's materialization.

**Exception**: Value objects configured via `HasConversion` don't need `private set` on the value object itself — EF sets the property on the *owning* entity, not on the value object.

---

## Collections: Child Entities

Child entities (ProductImage, CartItem, OrderItem) are not value objects — they have identity. Configure them as normal owned or related entities.

```csharp
// Child entities owned by aggregate root
builder.OwnsMany(p => p.Images, image =>
{
    image.WithOwner().HasForeignKey("ProductId");
    image.HasKey("Id");
    image.Property(i => i.Url).HasMaxLength(2000).IsRequired();
    image.Property(i => i.AltText).HasMaxLength(500);
    image.Property(i => i.IsPrimary).IsRequired();
    image.Property(i => i.DisplayOrder).IsRequired();
    image.ToTable("ProductImages");
});
```

EF Core's `OwnsMany` creates a separate table. Always call `.ToTable(...)` to name it explicitly.

**How to back the private collection:**

```csharp
public class Product : AggregateRoot
{
    private readonly List<ProductImage> _images = new();
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();
}
```

EF Core needs to know which field backs the collection:

```csharp
builder.OwnsMany(p => p.Images, image =>
{
    image.UsePropertyAccessMode(PropertyAccessMode.Field);  // ← use the _images field
    // ... rest of config
});
```

Or configure globally in `DbContext.OnModelCreating`:
```csharp
modelBuilder.UsePropertyAccessMode(PropertyAccessMode.PreferFieldDuringConstruction);
```

---

## Enums: Always Store as String

```csharp
builder.Property(p => p.Status)
    .HasConversion<string>()  // store "Active" not 2
    .HasMaxLength(50)
    .IsRequired();
```

Never store enums as integers. See `theory/04-value-types-and-dtos.md` §Enums for why.

---

## Soft Deletes: Global Query Filter

```csharp
// In AppDbContext.OnModelCreating
modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
modelBuilder.Entity<Category>().HasQueryFilter(c => !c.IsDeleted);
// ... for every entity that supports soft delete
```

Once configured, every query against `Products` automatically adds `WHERE IsDeleted = 0`. To intentionally query deleted records:

```csharp
_db.Products.IgnoreQueryFilters().Where(p => p.IsDeleted)
```

---

## EF Core Cannot Call Your Factory Method

EF Core does NOT call `Product.Create(...)` when loading from the database. It calls the private parameterless constructor and then sets properties via reflection. This means:

- The private constructor **must not** raise domain events or run business logic
- All validation happens in `Create(...)` which is only called by your application code
- Aggregates loaded from the database bypass all factory validation — the data is assumed valid because it was validated when first saved

```csharp
private Product()
{
    // EMPTY. No defaults. No validation. No events.
    // EF Core uses this to materialize. Keep it clean.
}

public static Product Create(...)
{
    // ALL validation here. This is called by your command handlers only.
}
```

---

## Full Configuration Example: Product

```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        // Single-property record VOs → HasConversion
        builder.Property(p => p.Name)
            .HasConversion(n => n.Value, v => ProductName.Create(v))
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasConversion(s => s.Value, v => Slug.Create(v))
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Sku)
            .HasConversion(s => s.Value, v => Sku.Create(v))
            .HasMaxLength(100)
            .IsRequired();

        // Multi-property class VO → OwnsOne with explicit column names
        builder.OwnsOne(p => p.Price, m =>
        {
            m.Property(x => x.Amount).HasColumnName("Price").HasPrecision(18, 4).IsRequired();
            m.Property(x => x.Currency).HasColumnName("PriceCurrency").HasMaxLength(3).IsRequired();
        });

        builder.OwnsOne(p => p.CompareAtPrice, m =>
        {
            m.Property(x => x.Amount).HasColumnName("CompareAtPrice").HasPrecision(18, 4);
            m.Property(x => x.Currency).HasColumnName("CompareAtPriceCurrency").HasMaxLength(3);
        });

        // Enum → string
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Child entity collection
        builder.OwnsMany(p => p.Images, image =>
        {
            image.ToTable("ProductImages");
            image.HasKey("Id");
            image.WithOwner().HasForeignKey("ProductId");
            image.Property(i => i.Url).HasMaxLength(2000).IsRequired();
            image.Property(i => i.AltText).HasMaxLength(500);
            image.Property(i => i.IsPrimary).IsRequired();
            image.Property(i => i.DisplayOrder).IsRequired();
        });

        // Foreign key (no navigation property — just the ID)
        builder.Property(p => p.CategoryId).IsRequired();

        // Soft delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);

        // Indexes
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.HasIndex(p => p.CategoryId);
    }
}
```

---

## Summary

| Value Object Type | C# Type | EF Core Strategy | Column Result |
|------------------|---------|------------------|---------------|
| Single-property wrapper | `record` | `HasConversion(to, from)` | One clean column |
| Multi-property (all required) | `class : ValueObject` | `OwnsOne(...)` with named columns | Inline columns, no join |
| Multi-property (optional) | `class : ValueObject` | `OwnsOne(...)` — all nullable | Same, nullable columns |
| Enum | `enum` | `HasConversion<string>()` | Varchar column |
| Child entities | `class : Entity` | `OwnsMany(...)` with `.ToTable(...)` | Separate table |

# Value Objects, DTOs, Enums, Structs, and AutoMapper

**Read this after `03-query-side.md`.**

---

## Value Objects: Record vs Abstract Class

You already know value objects are equal by value, not by identity. The question is: *which C# type do you use to write them?*

You have three choices. Here is when to use each.

---

### Option A: `record` (simple value objects)

C# records automatically generate value equality based on all their properties. For simple value objects where equality means "all properties are equal," a record is the cleanest choice:

```csharp
// Email is equal if the address string is equal — that's all
public record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new CatalogDomainException("EMAIL_EMPTY", "Email cannot be empty.");

        var normalized = raw.Trim().ToLowerInvariant();

        if (!normalized.Contains('@'))
            throw new CatalogDomainException("EMAIL_INVALID", "Email is not valid.");

        return new Email(normalized);
    }
}
```

**Why `record` works here:**
- `Email("a@b.com") == Email("a@b.com")` is `true` — records compare properties automatically
- Immutable by default (`init`-only or private constructor with no setters)
- Concise syntax

**When to use `record`**: when "equal" means "all properties are equal" and the value object is simple.

**How EF Core persists record value objects**: Use a **value converter** (`HasConversion`), NOT `OwnsOne`. A value converter stores the single property directly in the parent table's column — clean and flat. `OwnsOne` on a single-property record creates ugly shadow columns and fights with private constructors. See `theory/06-ef-core-persistence.md` for full details.

```csharp
// EF configuration for Email record — one clean column, no shadow properties
builder.Property(u => u.Email)
    .HasConversion(e => e.Value, v => Email.Create(v))
    .HasMaxLength(256);
```

---

### Option B: `class` inheriting `ValueObject` (complex value objects)

Sometimes you need custom equality — for example, `Address` where equality might mean only street + city + country match (not the optional `Line2`). The `ValueObject` base class from SharedKernel lets you define exactly which components count:

```csharp
public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string Country { get; }
    public string? PostalCode { get; }  // ← optional, NOT part of equality

    private Address(string street, string city, string country, string? postalCode)
    {
        Street = street;
        City = city;
        Country = country;
        PostalCode = postalCode;
    }

    public static Address Create(string street, string city, string country, string? postalCode = null)
    {
        if (string.IsNullOrWhiteSpace(street)) throw new IdentityDomainException("Street required.");
        if (string.IsNullOrWhiteSpace(city)) throw new IdentityDomainException("City required.");
        if (string.IsNullOrWhiteSpace(country)) throw new IdentityDomainException("Country required.");
        return new Address(street, city, country, postalCode);
    }

    // PostalCode is NOT included — two addresses are equal even with different postal codes
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street.ToLowerInvariant();
        yield return City.ToLowerInvariant();
        yield return Country.ToLowerInvariant();
    }
}
```

**When to use `class : ValueObject`**: when you need to pick and choose which properties define equality.

**How EF Core persists `class : ValueObject`**: Use `OwnsOne()` with explicit column names:
```csharp
builder.OwnsOne(p => p.Price, m => {
    m.Property(x => x.Amount).HasColumnName("Price").HasPrecision(18, 4).IsRequired();
    m.Property(x => x.Currency).HasColumnName("PriceCurrency").HasMaxLength(3).IsRequired();
});
```
Always call `.HasColumnName(...)` — EF's default generates `Price_Amount` with underscores that leak internal navigation model names into your database schema.

---

### Can you combine both? (`record : ValueObject`)

You might think: "use `record` AND inherit from `ValueObject` base class to get both."

**Don't.** Records generate their own `Equals()` and `GetHashCode()` based on all properties. The ValueObject base class overrides `Equals()` based on `GetEqualityComponents()`. They fight each other. Pick one.

**Rule of thumb for this project:**
- Simple VOs (Email, Slug, Money, Quantity, ProductName): use `record`
- VOs with custom partial equality (Address, DateRange): use `class : ValueObject`

---

## Structs — When and Why

A `struct` in C# is a **value type** that lives on the stack, not the heap. Copying a struct copies its data directly. Copying a class copies a reference (pointer) to heap data.

### Why you'd want a struct

- No heap allocation means no garbage collector pressure
- Copying is cheap for tiny types (8–16 bytes)

### Why you usually DON'T use structs for value objects

| Problem | Detail |
|---------|--------|
| Can't be null | `Money? price` works but is awkward |
| EF Core support is limited | EF Core doesn't support struct-valued owned entities well |
| Can't inherit | Structs can't inherit from `ValueObject` base class |
| Size matters | If your struct is > 16 bytes, copying it becomes more expensive than a heap reference |
| Mutable by accident | The default struct behavior is mutable — requires discipline to keep immutable |

**Practical rule**: Don't use structs for domain value objects. Use `record` or `class`. The performance benefit is real but irrelevant at the scale of an e-commerce app until proven by profiling.

**The one place structs appear in this codebase**: primitive result types inside tight loops. Not in the domain.

---

## Enums — How and Where

### Basic C# Enums

Use enums for any property that has a fixed set of possible values:

```csharp
// In Catalog.Domain (or SharedKernel if shared across contexts)
public enum ProductStatus
{
    Draft,
    Active,
    Discontinued
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}
```

### How EF Core Stores Enums

By default, EF Core stores enums as their underlying integer (`Draft = 0`, `Active = 1`, etc.). This is bad:

- Reordering enum values silently corrupts data
- Reading the database requires decoding numbers
- DB migrations become misleading (`WHERE status = 1` instead of `WHERE status = 'Active'`)

**Always store enums as strings** in EF Core configurations:

```csharp
// In CatalogDbContext or Product configuration
builder.Property(p => p.Status)
    .HasConversion<string>()   // ← store "Active" not 1
    .HasMaxLength(50);
```

### When an Enum Is Not Enough: The Enumeration Class

Enums can't have behavior. If `OrderStatus.Cancelled` needs to know which statuses it can transition FROM, you need the **Enumeration class** pattern:

```csharp
public abstract class Enumeration
{
    public int Id { get; }
    public string Name { get; }

    protected Enumeration(int id, string name) { Id = id; Name = name; }

    public override string ToString() => Name;
}

public class OrderStatus : Enumeration
{
    public static readonly OrderStatus Pending   = new(1, "Pending");
    public static readonly OrderStatus Confirmed = new(2, "Confirmed");
    public static readonly OrderStatus Shipped   = new(3, "Shipped");
    public static readonly OrderStatus Cancelled = new(4, "Cancelled");

    private OrderStatus(int id, string name) : base(id, name) { }

    // Behavior lives HERE, not in the Order aggregate or in the handler
    public bool CanTransitionTo(OrderStatus next)
    {
        return (this, next) switch
        {
            (_, _) when this == next => false,
            ({ Id: 1 }, { Id: 2 }) => true,   // Pending → Confirmed
            ({ Id: 2 }, { Id: 3 }) => true,   // Confirmed → Shipped
            ({ Id: 1 }, { Id: 4 }) => true,   // Pending → Cancelled
            ({ Id: 2 }, { Id: 4 }) => true,   // Confirmed → Cancelled
            _ => false
        };
    }
}
```

**Use regular enums for Phases 1–6. Introduce Enumeration class in Phase 7 (Ordering) where status transitions have business rules.**

---

## DTOs — How Many, Where, Are They Records?

### The CQRS Shift: Commands ARE the Write DTOs

In the old architecture:
```
Client → HTTP body: CreateProductDto → ProductService.CreateAsync(dto) → new Product(dto)
```

In CQRS, **the command is the DTO**. There is no separate `CreateProductDto`:
```
Client → HTTP body: CreateProductCommand → MediatR → CreateProductCommandHandler → Product.Create(...)
```

This means:
- No separate "request DTO" layer for writes
- The command carries the input data directly
- Validators are registered for commands, not for separate DTOs

### How Many Read DTOs per Entity?

There is no universal rule, but a reliable pattern is **one DTO per read shape needed by the UI**:

```
ProductSummaryDto   → dropdowns, search results, related product lists
                      (Id, Name, Slug, Price, ThumbnailUrl)

ProductDto          → product list page
                      (Id, Name, Slug, Price, CategoryName, InStock, ThumbnailUrl)

ProductDetailDto    → product detail page
                      (all above + Description, Images[], Tags[], FullStock, ...)
```

**Start with two**: list DTO and detail DTO. Add more only when the UI demands a genuinely different shape. Don't create DTOs speculatively.

### Where Do DTOs Live?

```
ECommerce.Catalog.Application/
└── DTOs/
    ├── ProductSummaryDto.cs
    ├── ProductDto.cs
    └── ProductDetailDto.cs
```

Read DTOs live in **Application**, not Domain. The Domain has no concept of "what the UI needs." Commands (write DTOs) also live in Application.

### Are DTOs Records? Yes.

```csharp
// List DTO — flat, minimal
public record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    decimal Price,
    string CategoryName,
    bool InStock
);

// Detail DTO — richer, includes related data
public record ProductDetailDto(
    Guid Id,
    string Name,
    string Description,
    string Slug,
    decimal Price,
    string CategoryName,
    IReadOnlyList<string> ImageUrls,
    bool InStock,
    int StockQuantity
);
```

**Why records?**
- DTOs carry data, they don't have behavior — records are perfect for this
- Immutability: once you create a DTO, nobody should mutate it
- Value equality: `new ProductDto(...) == new ProductDto(...)` → useful in tests
- Concise syntax: primary constructor parameters become properties automatically

---

## AutoMapper — Where It Lives and When to Use It

### The honest answer: in CQRS with a thin read stack, you rarely need AutoMapper

Here is why, depending on which side of CQRS you're on:

**Write side (commands):**
You map a command to a domain object by calling the domain factory:
```csharp
var product = Product.Create(
    ProductName.Create(command.Name),
    Money.Create(command.Price, "USD"),
    Sku.Create(command.Sku),
    command.CategoryId
);
```
AutoMapper cannot do this. The factory method validates, creates value objects, raises domain events. Mapping is domain logic, not property copying. **Never use AutoMapper on the write side.**

**Read side (queries):**
You map entities to DTOs by projecting in SQL:
```csharp
.Select(p => new ProductDto(p.Id, p.Name, p.Slug, p.Price, p.Category.Name, p.Stock > 0))
```
EF Core translates this `.Select()` into a SQL projection. AutoMapper cannot participate in this — it runs in-memory after the data is already loaded. Using AutoMapper here means loading full entities just to map them. **Prefer `.Select()` projection over AutoMapper for queries.**

### When AutoMapper IS useful

The one case where AutoMapper earns its place: **mapping a loaded aggregate to a response DTO at the end of a command handler** (the "return the created item" pattern):

```csharp
// Command returns the full detail DTO of what was created
public async Task<Result<ProductDetailDto>> Handle(CreateProductCommand command, ...)
{
    var product = Product.Create(...);
    await _repository.AddAsync(product, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    // Map to DTO — reasonable use of AutoMapper
    return Result<ProductDetailDto>.Ok(_mapper.Map<ProductDetailDto>(product));
}
```

### Where AutoMapper Lives (When You Use It)

```
ECommerce.Catalog.Application/
└── Mappings/
    └── CatalogMappingProfile.cs
```

```csharp
public class CatalogMappingProfile : Profile
{
    public CatalogMappingProfile()
    {
        // Aggregate → DTO (after a command, to return result)
        CreateMap<Product, ProductDetailDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src => src.Images.Select(i => i.Url)));
    }
}
```

Register in Program.cs (or in each context's DI extension):
```csharp
builder.Services.AddAutoMapper(typeof(CatalogMappingProfile).Assembly);
```

### Recommendation for This Migration

1. Start **without AutoMapper** — it adds a dependency and a layer of magic
2. Use `.Select()` for all queries (fast, SQL-level projection)
3. Use manual `ToDetailDto()` extension methods on aggregates for command responses
4. Add AutoMapper only if the manual mapping boilerplate becomes genuinely painful (many properties, many mappings)

---

## Summary Table

| Type | C# Type | Where | Why |
|------|---------|-------|-----|
| Simple value object | `record` | Domain/ValueObjects/ | Built-in value equality, immutable |
| Complex value object | `class : ValueObject` | Domain/ValueObjects/ | Custom equality via GetEqualityComponents |
| Struct | Avoid | — | EF Core issues, can't inherit, mutable traps |
| Simple status | `enum` with `.HasConversion<string>()` | Domain or SharedKernel | Simple, store as string in DB |
| Status with behavior | Enumeration class | Domain | Can hold CanTransitionTo() logic |
| Write DTO | Command record | Application/Commands/ | Command IS the write DTO in CQRS |
| Read DTO (list) | `record` | Application/DTOs/ | Immutable data carrier, concise |
| Read DTO (detail) | `record` | Application/DTOs/ | Same |
| AutoMapper Profile | class : Profile | Application/Mappings/ | Only for aggregate → DTO after commands |

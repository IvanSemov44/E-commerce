# Phase 1: Catalog Bounded Context

**Prerequisite**: Phase 0 complete. SharedKernel exists. MediatR configured. Pipeline behaviors registered.

**Learn**: Aggregate Roots, Child Entities, Value Objects, Commands, Queries, Handlers, EF Core owned entities.

---

## What We're Building

Three new projects:
- `ECommerce.Catalog.Domain` — Product and Category aggregates with domain logic
- `ECommerce.Catalog.Application` — Commands, Queries, Handlers, DTOs
- `ECommerce.Catalog.Infrastructure` — Repositories, EF Core configurations

Replace `ProductService` and `CategoryService` with MediatR handlers.

---

## Old Service → New Handler Mapping

Before writing a single line of DDD code, map every old service method to its replacement. This is your characterization test checklist.

| Old Method | New Handler | HTTP Method |
|-----------|-------------|-------------|
| `ProductService.GetProductsAsync(filter)` | `GetProductsQuery` | GET /api/products |
| `ProductService.GetProductByIdAsync(id)` | `GetProductByIdQuery` | GET /api/products/{id} |
| `ProductService.GetProductBySlugAsync(slug)` | `GetProductBySlugQuery` | GET /api/products/slug/{slug} |
| `ProductService.GetFeaturedProductsAsync()` | `GetFeaturedProductsQuery` | GET /api/products/featured |
| `ProductService.CreateProductAsync(dto)` | `CreateProductCommand` | POST /api/products |
| `ProductService.UpdateProductAsync(id, dto)` | `UpdateProductCommand` | PUT /api/products/{id} |
| `ProductService.UpdatePriceAsync(id, price)` | `UpdateProductPriceCommand` | PATCH /api/products/{id}/price |
| `ProductService.AddImageAsync(id, dto)` | `AddProductImageCommand` | POST /api/products/{id}/images |
| `ProductService.DeleteProductAsync(id)` | `DeleteProductCommand` | DELETE /api/products/{id} |
| `CategoryService.GetCategoriesAsync()` | `GetCategoriesQuery` | GET /api/categories |
| `CategoryService.GetCategoryByIdAsync(id)` | `GetCategoryByIdQuery` | GET /api/categories/{id} |
| `CategoryService.CreateCategoryAsync(dto)` | `CreateCategoryCommand` | POST /api/categories |
| `CategoryService.UpdateCategoryAsync(id, dto)` | `UpdateCategoryCommand` | PUT /api/categories/{id} |
| `CategoryService.DeleteCategoryAsync(id)` | `DeleteCategoryCommand` | DELETE /api/categories/{id} |

**Do this before Step 1. Write characterization tests against the old service before touching any DDD code.**

---

## Step 1: Characterization Tests

Write integration tests that document the current behavior of every endpoint above. Run them. Confirm they pass. These become your regression suite.

See `theory/07-testing-ddd.md` §Characterization Tests for the pattern.

Minimum tests per endpoint:
- Happy path: correct status + response shape
- Not found (GET/PUT/DELETE): 404
- Validation failure (POST/PUT): 400
- Business rule violation (duplicate slug/SKU): 422 with error code
- Authorization: 401 if unauthenticated, 403 if wrong role

---

## Step 2: Domain Project

### Project setup

```bash
cd src/backend
dotnet new classlib -n ECommerce.Catalog.Domain -f net10.0
dotnet sln ../../ECommerce.sln add ECommerce.Catalog.Domain/ECommerce.Catalog.Domain.csproj
dotnet add ECommerce.Catalog.Domain/ECommerce.Catalog.Domain.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
rm ECommerce.Catalog.Domain/Class1.cs
```

**Allowed references**: SharedKernel only. No EF Core. No MediatR. Pure C#.

### Domain exception

```csharp
// Exceptions/CatalogDomainException.cs
public class CatalogDomainException : DomainException
{
    public CatalogDomainException(string message) : base(message) { }
    public CatalogDomainException(string code, string message) : base(code, message) { }
}
```

### Value Objects

All value objects live in `ValueObjects/`. Single-property wrappers use `record`. Multi-property use `class : ValueObject`.

```csharp
// ValueObjects/ProductName.cs
public record ProductName
{
    public string Value { get; }

    private ProductName(string value) => Value = value;

    public static ProductName Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new CatalogDomainException("PRODUCT_NAME_EMPTY", "Product name cannot be empty.");
        if (raw.Trim().Length > 200)
            throw new CatalogDomainException("PRODUCT_NAME_TOO_LONG", "Product name cannot exceed 200 characters.");
        return new ProductName(raw.Trim());
    }
}
```

```csharp
// ValueObjects/Slug.cs
public record Slug
{
    public string Value { get; }

    private Slug(string value) => Value = value;

    public static Slug Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new CatalogDomainException("SLUG_EMPTY", "Slug cannot be empty.");

        var slug = raw.Trim().ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        // Remove all characters that aren't alphanumeric or hyphen
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-").Trim('-');

        if (slug.Length == 0)
            throw new CatalogDomainException("SLUG_INVALID", "Slug produced no valid characters.");

        return new Slug(slug);
    }
}
```

```csharp
// ValueObjects/Money.cs — multi-property, use class : ValueObject
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money() { }  // EF Core
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0)
            throw new CatalogDomainException("MONEY_NEGATIVE", "Amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new CatalogDomainException("MONEY_INVALID_CURRENCY", "Currency must be a 3-letter ISO code.");
        return new Money(amount, currency.ToUpperInvariant());
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new CatalogDomainException("MONEY_CURRENCY_MISMATCH", "Cannot add money with different currencies.");
        return new Money(Amount + other.Amount, Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

Also create: `Sku.cs`, `Barcode.cs`, `Weight.cs`, `CategoryName.cs` — same pattern as `ProductName`.

### Domain events

```csharp
// Aggregates/Product/Events/ProductCreatedEvent.cs
public record ProductCreatedEvent(Guid ProductId, string Name, Guid CategoryId) : DomainEventBase;

// Aggregates/Product/Events/ProductPriceChangedEvent.cs
public record ProductPriceChangedEvent(Guid ProductId, Money OldPrice, Money NewPrice) : DomainEventBase;

// Aggregates/Product/Events/ProductDeactivatedEvent.cs
public record ProductDeactivatedEvent(Guid ProductId) : DomainEventBase;

// Aggregates/Category/Events/CategoryCreatedEvent.cs
public record CategoryCreatedEvent(Guid CategoryId, string Name) : DomainEventBase;
```

### Product aggregate

```csharp
// Aggregates/Product/Product.cs
public class Product : AggregateRoot
{
    public ProductName Name { get; private set; } = null!;
    public Slug Slug { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public Money? CompareAtPrice { get; private set; }
    public Sku Sku { get; private set; } = null!;
    public string? Description { get; private set; }
    public ProductStatus Status { get; private set; }
    public bool IsFeatured { get; private set; }
    public Guid CategoryId { get; private set; }

    private readonly List<ProductImage> _images = new();
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    private Product() { }  // EF Core

    public static Product Create(
        ProductName name,
        Money price,
        Sku sku,
        Guid categoryId,
        string? description = null,
        Money? compareAtPrice = null)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = Slug.Create(name.Value),
            Price = price,
            CompareAtPrice = compareAtPrice,
            Sku = sku,
            Description = description,
            Status = ProductStatus.Draft,
            IsFeatured = false,
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        product.AddDomainEvent(new ProductCreatedEvent(product.Id, name.Value, categoryId));
        return product;
    }

    public void UpdatePrice(Money newPrice)
    {
        var oldPrice = Price;
        Price = newPrice;
        AddDomainEvent(new ProductPriceChangedEvent(Id, oldPrice, newPrice));
    }

    public void UpdateDetails(ProductName name, string? description, Guid categoryId)
    {
        Name = name;
        Slug = Slug.Create(name.Value);
        Description = description;
        CategoryId = categoryId;
    }

    public void Activate()
    {
        if (Status == ProductStatus.Active) return;
        Status = ProductStatus.Active;
    }

    public void Deactivate()
    {
        if (Status == ProductStatus.Discontinued)
            throw new CatalogDomainException("PRODUCT_DISCONTINUED", "Cannot deactivate a discontinued product.");
        Status = ProductStatus.Inactive;
        AddDomainEvent(new ProductDeactivatedEvent(Id));
    }

    public void AddImage(string url, string? altText)
    {
        if (_images.Count >= 10)
            throw new CatalogDomainException("PRODUCT_MAX_IMAGES", "Product cannot have more than 10 images.");

        var isPrimary = _images.Count == 0;  // First image is always primary
        var order = _images.Count;
        _images.Add(new ProductImage(Guid.NewGuid(), Id, url, altText, isPrimary, order));
    }

    public void SetPrimaryImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new CatalogDomainException("IMAGE_NOT_FOUND", "Image not found on this product.");

        foreach (var img in _images) img.SetPrimary(false);
        image.SetPrimary(true);
    }
}
```

### ProductImage child entity

```csharp
// Aggregates/Product/ProductImage.cs
public class ProductImage : Entity
{
    public Guid ProductId { get; private set; }
    public string Url { get; private set; } = null!;
    public string? AltText { get; private set; }
    public bool IsPrimary { get; private set; }
    public int DisplayOrder { get; private set; }

    private ProductImage() { }  // EF Core

    internal ProductImage(Guid id, Guid productId, string url, string? altText, bool isPrimary, int displayOrder)
    {
        Id = id;
        ProductId = productId;
        Url = url;
        AltText = altText;
        IsPrimary = isPrimary;
        DisplayOrder = displayOrder;
    }

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}
```

`internal` constructor: ProductImage can only be created from within the Catalog.Domain assembly (through Product). External code calls `product.AddImage(...)`, not `new ProductImage(...)`.

### Category aggregate

```csharp
// Aggregates/Category/Category.cs
public class Category : AggregateRoot
{
    public CategoryName Name { get; private set; } = null!;
    public Slug Slug { get; private set; } = null!;
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; }

    private Category() { }  // EF Core

    public static Category Create(CategoryName name, Guid? parentId = null)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = Slug.Create(name.Value),
            ParentId = parentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        category.AddDomainEvent(new CategoryCreatedEvent(category.Id, name.Value));
        return category;
    }

    public void Rename(CategoryName newName)
    {
        Name = newName;
        Slug = Slug.Create(newName.Value);
    }

    public void MoveTo(Guid? newParentId)
    {
        if (newParentId == Id)
            throw new CatalogDomainException("CATEGORY_CIRCULAR", "Category cannot be its own parent.");
        ParentId = newParentId;
    }
}
```

### Repository interfaces

```csharp
// Interfaces/IProductRepository.cs
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
}

// Interfaces/ICategoryRepository.cs
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task AddAsync(Category category, CancellationToken ct = default);
    Task UpdateAsync(Category category, CancellationToken ct = default);
}
```

---

## Step 3: Application Project

```bash
dotnet new classlib -n ECommerce.Catalog.Application -f net10.0
dotnet sln ../../ECommerce.sln add ECommerce.Catalog.Application/ECommerce.Catalog.Application.csproj

dotnet add ECommerce.Catalog.Application/ECommerce.Catalog.Application.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add ECommerce.Catalog.Application/ECommerce.Catalog.Application.csproj \
    reference ECommerce.Catalog.Domain/ECommerce.Catalog.Domain.csproj

dotnet add ECommerce.Catalog.Application/ECommerce.Catalog.Application.csproj package MediatR
dotnet add ECommerce.Catalog.Application/ECommerce.Catalog.Application.csproj package FluentValidation
```

### Read DTOs

```csharp
// DTOs/ProductDto.cs — list view
public record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    decimal Price,
    string Currency,
    string CategoryName,
    bool InStock,
    string? ThumbnailUrl
);

// DTOs/ProductDetailDto.cs — detail view
public record ProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    string Currency,
    decimal? CompareAtPrice,
    string Sku,
    string CategoryId,
    string CategoryName,
    IReadOnlyList<ProductImageDto> Images,
    bool IsFeatured,
    string Status
);

public record ProductImageDto(Guid Id, string Url, string? AltText, bool IsPrimary);

// DTOs/CategoryDto.cs
public record CategoryDto(Guid Id, string Name, string Slug, Guid? ParentId, bool IsActive);
```

### Commands

```csharp
// Commands/CreateProduct/CreateProductCommand.cs
public record CreateProductCommand(
    string Name,
    decimal Price,
    string Currency,
    string Sku,
    Guid CategoryId,
    string? Description = null,
    decimal? CompareAtPrice = null
) : IRequest<Result<ProductDetailDto>>, ITransactionalCommand;

// Commands/CreateProduct/CreateProductCommandHandler.cs
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductDetailDto>>
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public async Task<Result<ProductDetailDto>> Handle(
        CreateProductCommand command, CancellationToken cancellationToken)
    {
        // 1. Authorization — fail fast before any domain work
        if (!_currentUser.IsInRole("Admin"))
            return Result<ProductDetailDto>.Unauthorized();

        // 2. Validate category exists
        var category = await _categories.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
            return Result<ProductDetailDto>.Fail(ErrorCodes.Catalog.CategoryNotFound, "Category not found.");

        // 3. Check SKU uniqueness (cross-aggregate, can't be enforced in aggregate alone)
        if (await _products.SkuExistsAsync(command.Sku, cancellationToken))
            return Result<ProductDetailDto>.Fail(ErrorCodes.Catalog.SkuAlreadyExists, "A product with this SKU already exists.");

        // 4. Build domain objects and create aggregate via factory
        var product = Product.Create(
            ProductName.Create(command.Name),
            Money.Create(command.Price, command.Currency),
            Sku.Create(command.Sku),
            command.CategoryId,
            command.Description,
            command.CompareAtPrice.HasValue
                ? Money.Create(command.CompareAtPrice.Value, command.Currency)
                : null);

        // 5. Persist
        await _products.AddAsync(product, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // 6. Return — map aggregate to detail DTO
        return Result<ProductDetailDto>.Ok(product.ToDetailDto(category.Name.Value));
    }
}

// Commands/CreateProduct/CreateProductCommandValidator.cs
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
```

**Why check SKU in the handler, not the aggregate?** The aggregate cannot access the repository — it has no dependencies (Rule 7). Uniqueness requires a database query. The handler fetches external data and passes it to the aggregate or guards before calling it. This is the correct pattern for cross-aggregate invariants.

### Queries

```csharp
// Queries/GetProducts/GetProductsQuery.cs
public record GetProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CategoryId = null,
    bool? InStock = null
) : IRequest<Result<PaginatedResult<ProductDto>>>;

// Queries/GetProducts/GetProductsQueryHandler.cs
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, Result<PaginatedResult<ProductDto>>>
{
    private readonly AppDbContext _db;

    public async Task<Result<PaginatedResult<ProductDto>>> Handle(
        GetProductsQuery query, CancellationToken cancellationToken)
    {
        var q = _db.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.Status == ProductStatus.Active);

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(p => p.Name.Value.Contains(query.Search));

        if (query.CategoryId.HasValue)
            q = q.Where(p => p.CategoryId == query.CategoryId.Value);

        var totalCount = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderBy(p => p.Name.Value)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new ProductDto(
                p.Id,
                p.Name.Value,
                p.Slug.Value,
                p.Price.Amount,
                p.Price.Currency,
                p.Category.Name.Value,
                true,  // stock check via Inventory context (Phase 3)
                p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return Result<PaginatedResult<ProductDto>>.Ok(
            new PaginatedResult<ProductDto>(items, totalCount, query.Page, query.PageSize));
    }
}
```

**Note on stock**: In Phase 1, `InStock` is hardcoded to `true`. In Phase 3 (Inventory), you will either join to the InventoryItem table or resolve via the Inventory context. Document this as a known limitation in a `// TODO Phase 3:` comment.

### DTO mapping extension

Instead of AutoMapper, use a simple extension method for aggregate → DTO after commands:

```csharp
// Extensions/ProductMappingExtensions.cs
public static class ProductMappingExtensions
{
    public static ProductDetailDto ToDetailDto(this Product product, string categoryName) =>
        new(
            product.Id,
            product.Name.Value,
            product.Slug.Value,
            product.Description,
            product.Price.Amount,
            product.Price.Currency,
            product.CompareAtPrice?.Amount,
            product.Sku.Value,
            product.CategoryId.ToString(),
            categoryName,
            product.Images.Select(i => new ProductImageDto(i.Id, i.Url, i.AltText, i.IsPrimary)).ToList(),
            product.IsFeatured,
            product.Status.ToString());
}
```

---

## Step 4: Infrastructure Project

```bash
dotnet new classlib -n ECommerce.Catalog.Infrastructure -f net10.0
dotnet sln ../../ECommerce.sln add ECommerce.Catalog.Infrastructure/ECommerce.Catalog.Infrastructure.csproj

dotnet add ECommerce.Catalog.Infrastructure/ECommerce.Catalog.Infrastructure.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add ECommerce.Catalog.Infrastructure/ECommerce.Catalog.Infrastructure.csproj \
    reference ECommerce.Catalog.Domain/ECommerce.Catalog.Domain.csproj

dotnet add ECommerce.Catalog.Infrastructure/ECommerce.Catalog.Infrastructure.csproj \
    package Microsoft.EntityFrameworkCore
```

### Repository implementation

```csharp
// Repositories/ProductRepository.cs
public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public ProductRepository(AppDbContext db) => _db = db;

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        _db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Slug.Value == slug, ct);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default) =>
        _db.Products.AnyAsync(p => p.Sku.Value == sku, ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        _db.Products.AnyAsync(p => p.Slug.Value == slug, ct);

    public async Task AddAsync(Product product, CancellationToken ct = default) =>
        await _db.Products.AddAsync(product, ct);

    public Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        _db.Products.Update(product);
        return Task.CompletedTask;
    }
}
```

### EF Core configuration

```csharp
// Configurations/ProductConfiguration.cs
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        // Single-property records → HasConversion (clean columns)
        builder.Property(p => p.Name)
            .HasConversion(n => n.Value, v => ProductName.Create(v))
            .HasMaxLength(200).IsRequired();

        builder.Property(p => p.Slug)
            .HasConversion(s => s.Value, v => Slug.Create(v))
            .HasMaxLength(200).IsRequired();

        builder.Property(p => p.Sku)
            .HasConversion(s => s.Value, v => Sku.Create(v))
            .HasMaxLength(100).IsRequired();

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
            .HasConversion<string>().HasMaxLength(50).IsRequired();

        // Child entities
        builder.OwnsMany(p => p.Images, img =>
        {
            img.ToTable("ProductImages");
            img.HasKey("Id");
            img.WithOwner().HasForeignKey("ProductId");
            img.Property(i => i.Url).HasMaxLength(2000).IsRequired();
            img.Property(i => i.AltText).HasMaxLength(500);
            img.Property(i => i.IsPrimary).IsRequired();
            img.Property(i => i.DisplayOrder).IsRequired();
        });

        builder.Property(p => p.CategoryId).IsRequired();
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.HasIndex(p => p.CategoryId);
    }
}
```

### Register infrastructure DI

```csharp
// DependencyInjection.cs (in Infrastructure project)
public static class CatalogInfrastructureDI
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        return services;
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddCatalogInfrastructure();
// In AddMediatR, add:
cfg.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly);
```

---

## Step 5: Cutover

**Do NOT start this step until characterization tests pass against the new handlers.**

1. Update `ProductsController` and `CategoriesController` to inject `IMediator` instead of the old services
2. Replace each service call with `await _mediator.Send(new XxxCommand(...))`
3. Run characterization tests — they must all pass
4. Delete `ProductService.cs`, `CategoryService.cs`, `IProductService.cs`, `ICategoryService.cs`
5. Remove DI registrations for old services from `Program.cs`
6. Run `dotnet build` — no references to old services should remain
7. Run full test suite

---

## Definition of Done

- [ ] Characterization tests written and passing (both old and new handlers)
- [ ] `ECommerce.Catalog.Domain` created — no EF Core references
- [ ] Product aggregate with factory, domain methods, events
- [ ] Category aggregate with factory and domain methods
- [ ] All value objects created with validation
- [ ] `ECommerce.Catalog.Application` created with all commands + queries
- [ ] All handlers follow: auth check → load aggregate → call domain method → save → return DTO
- [ ] `ECommerce.Catalog.Infrastructure` created with repository implementations
- [ ] EF configurations use `HasConversion` for records, `OwnsOne` for multi-property VOs
- [ ] Old `ProductService` and `CategoryService` deleted
- [ ] Controllers dispatch via MediatR
- [ ] `dotnet build` clean, no compiler warnings
- [ ] All characterization tests still passing after cutover

## What You Learned in Phase 1

- How an aggregate root enforces its own invariants without external services
- Why `ProductImage` is a child entity (has identity) not a value object (no identity)
- Why slug uniqueness must be checked in the handler, not the aggregate (Rule 7 + Rule 3)
- The difference between commands (write → aggregate → save) and queries (read → .Select() → DTO)
- How `HasConversion` vs `OwnsOne` differ based on single-property vs multi-property value objects
- The characterization test workflow: write → run → migrate → run → delete old service

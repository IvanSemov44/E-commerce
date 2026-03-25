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

> **Naming conflict starts here.** `ECommerce.Catalog.Domain` will define `Product`, `Category`, `IProductRepository`, `ICategoryRepository` — all names that already exist in `ECommerce.Core`. Both coexist until cutover. See `debt/README.md` items D-01, D-02, D-04 for how to handle this.

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

    public static Result<ProductName> Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<ProductName>.Fail(CatalogErrors.ProductNameEmpty);
        if (raw.Trim().Length > 200)
            return Result<ProductName>.Fail(CatalogErrors.ProductNameTooLong);
        return Result<ProductName>.Ok(new ProductName(raw.Trim()));
    }
}
```

```csharp
// ValueObjects/Slug.cs
public record Slug
{
    public string Value { get; }

    private Slug(string value) => Value = value;

    public static Result<Slug> Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<Slug>.Fail(CatalogErrors.SlugEmpty);

        string slug = raw.Trim().ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        // Remove all characters that aren't alphanumeric or hyphen
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-").Trim('-');

        if (slug.Length == 0)
            return Result<Slug>.Fail(CatalogErrors.SlugInvalid);

        return Result<Slug>.Ok(new Slug(slug));
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

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
            return Result<Money>.Fail(CatalogErrors.MoneyNegative);
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return Result<Money>.Fail(CatalogErrors.MoneyInvalidCurrency);
        return Result<Money>.Ok(new Money(amount, currency.ToUpperInvariant()));
    }

    public Result<Money> Add(Money other)
    {
        if (Currency != other.Currency)
            return Result<Money>.Fail(CatalogErrors.MoneyCurrencyMismatch);
        return Result<Money>.Ok(new Money(Amount + other.Amount, Currency));
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

    // Factory takes raw primitive inputs — validates everything and returns Result<Product>.
    public static Result<Product> Create(
        string nameRaw,
        decimal priceAmount,
        string priceCurrency,
        string skuRaw,
        Guid categoryId,
        string? description = null,
        decimal? compareAtPriceAmount = null)
    {
        var nameResult = ProductName.Create(nameRaw);
        if (!nameResult.IsSuccess) return Result<Product>.Fail(nameResult.GetErrorOrThrow());

        var priceResult = Money.Create(priceAmount, priceCurrency);
        if (!priceResult.IsSuccess) return Result<Product>.Fail(priceResult.GetErrorOrThrow());

        var skuResult = Sku.Create(skuRaw);
        if (!skuResult.IsSuccess) return Result<Product>.Fail(skuResult.GetErrorOrThrow());

        var slugResult = Slug.Create(nameRaw);
        if (!slugResult.IsSuccess) return Result<Product>.Fail(slugResult.GetErrorOrThrow());

        Money? compareAtPrice = null;
        if (compareAtPriceAmount.HasValue)
        {
            var compareResult = Money.Create(compareAtPriceAmount.Value, priceCurrency);
            if (!compareResult.IsSuccess) return Result<Product>.Fail(compareResult.GetErrorOrThrow());
            compareAtPrice = compareResult.GetDataOrThrow();
        }

        var name = nameResult.GetDataOrThrow();
        Product product = new()
        {
            Name = name,
            Slug = slugResult.GetDataOrThrow(),
            Price = priceResult.GetDataOrThrow(),
            CompareAtPrice = compareAtPrice,
            Sku = skuResult.GetDataOrThrow(),
            Description = description,
            Status = ProductStatus.Draft,
            IsFeatured = false,
            IsDeleted = false,
            CategoryId = categoryId,
        };

        product.AddDomainEvent(new ProductCreatedEvent(product.Id, name.Value, categoryId));
        return Result<Product>.Ok(product);
    }

    // Takes pre-validated value objects — callers call ProductName.Create() etc. first.
    // Slug derivation from a valid ProductName.Value cannot fail.
    public void UpdateDetails(ProductName name, string? description, Guid categoryId)
    {
        Name = name;
        Slug = Slug.Create(name.Value).GetDataOrThrow();
        Description = description;
        CategoryId = categoryId;
    }

    public void UpdatePrice(Money newPrice)
    {
        var oldPrice = Price;
        Price = newPrice;
        AddDomainEvent(new ProductPriceChangedEvent(Id, oldPrice, newPrice));
    }

    public void Activate()
    {
        if (Status == ProductStatus.Active) return;
        Status = ProductStatus.Active;
    }

    public Result Deactivate()
    {
        if (Status == ProductStatus.Discontinued)
            return Result.Fail(CatalogErrors.ProductDiscontinued);
        Status = ProductStatus.Inactive;
        AddDomainEvent(new ProductDeactivatedEvent(Id));
        return Result.Ok();
    }

    public Result AddImage(string url, string? altText)
    {
        if (_images.Count >= 10)
            return Result.Fail(CatalogErrors.ProductMaxImages);
        bool isPrimary = _images.Count == 0;
        int order = _images.Count;
        _images.Add(new ProductImage(Guid.NewGuid(), Id, url, altText, isPrimary, order));
        return Result.Ok();
    }

    public Result SetPrimaryImage(Guid imageId)
    {
        ProductImage? image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image is null) return Result.Fail(CatalogErrors.ProductImageNotFound);
        foreach (ProductImage img in _images) img.SetPrimary(false);
        image.SetPrimary(true);
        return Result.Ok();
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

    public static Result<Category> Create(string nameRaw, Guid? parentId = null)
    {
        var nameResult = CategoryName.Create(nameRaw);
        if (!nameResult.IsSuccess) return Result<Category>.Fail(nameResult.GetErrorOrThrow());

        var slugResult = Slug.Create(nameRaw);
        if (!slugResult.IsSuccess) return Result<Category>.Fail(slugResult.GetErrorOrThrow());

        var name = nameResult.GetDataOrThrow();
        Category category = new()
        {
            Name = name,
            Slug = slugResult.GetDataOrThrow(),
            ParentId = parentId,
            IsActive = true,
        };

        category.AddDomainEvent(new CategoryCreatedEvent(category.Id, name.Value));
        return Result<Category>.Ok(category);
    }

    // Takes a pre-validated CategoryName — callers use CategoryName.Create() first.
    public void Rename(CategoryName newName)
    {
        Name = newName;
        Slug = Slug.Create(newName.Value).GetDataOrThrow();
    }

    public Result MoveTo(Guid? newParentId)
    {
        if (newParentId == Id)
            return Result.Fail(CatalogErrors.CategoryCircularParent);
        ParentId = newParentId;
        return Result.Ok();
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

**Tester handoff after Step 2:** Once aggregates and value objects are delivered, the tester writes domain unit tests in `ECommerce.Catalog.Tests/Domain/`. See prompt in `.ai/plans/ddd-cqrs-migration/testing/tester-prompt-template.md` → Prompt 2.

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

**Tester handoff after Step 3:** Once handlers are delivered, the tester writes handler unit tests in `ECommerce.Catalog.Tests/Handlers/`. See prompt in `.ai/plans/ddd-cqrs-migration/testing/tester-prompt-template.md` → Prompt 3.

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

Full testing guide: `.ai/plans/ddd-cqrs-migration/testing/README.md`

**Characterization (integration — slow):**
- [ ] Characterization tests written and PASSING against OLD service (before any migration)
- [ ] Characterization tests still PASSING after cutover to new handlers

**Domain unit tests (fast — written after Step 2):**
- [ ] `ECommerce.Catalog.Tests/Domain/ProductTests.cs` written and PASSING
- [ ] `ECommerce.Catalog.Tests/Domain/CategoryTests.cs` written and PASSING
- Covers: factory methods, invariants, domain events, value object validation

**Handler unit tests (fast — written after Step 3):**
- [ ] `ECommerce.Catalog.Tests/Handlers/` tests written and PASSING for all command + query handlers
- Covers: correct repo called, UoW saved, correct Result returned

**Code:**
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

## What You Learned in Phase 1

- How an aggregate root enforces its own invariants without external services
- Why `ProductImage` is a child entity (has identity) not a value object (no identity)
- Why slug uniqueness must be checked in the handler, not the aggregate (Rule 7 + Rule 3)
- The difference between commands (write → aggregate → save) and queries (read → .Select() → DTO)
- How `HasConversion` vs `OwnsOne` differ based on single-property vs multi-property value objects
- The characterization test workflow: write → run → migrate → run → delete old service

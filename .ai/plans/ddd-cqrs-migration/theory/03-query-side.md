# The Query Side: Thin Read Stack

**Read this after `02-cqrs-and-mediatr.md`.**

---

## The Core Idea

On the write (command) side, you load a full aggregate, call domain methods, enforce invariants, and save.

On the read (query) side, **none of that is needed**. The data already exists. You just need to read it efficiently and return the right shape. Loading a rich aggregate just to map it to a DTO wastes CPU, memory, and DB round trips.

> **Rule 22 from rules.md**: Query handlers bypass aggregates. They query the DB directly and return DTOs.

---

## The Thin Read Stack Pattern

The read stack has two components:

1. **The query object** — defines what you're asking for
2. **The query handler** — fetches directly, returns DTO

```
Controller
    └── sends GetProductsQuery via MediatR
            └── GetProductsQueryHandler
                    └── IProductReadRepository
                            └── EF Core DbContext (SELECT with projection)
                                    └── ProductDto[]
```

No aggregate loaded. No domain methods called. No domain events raised.

---

## What a Query Handler Looks Like

```csharp
// Query — a plain record with filter parameters
public record GetProductsQuery(
    int Page,
    int PageSize,
    string? Search,
    Guid? CategoryId
) : IRequest<Result<PaginatedResult<ProductDto>>>;

// Handler — projects directly to DTO
public class GetProductsQueryHandler
    : IRequestHandler<GetProductsQuery, Result<PaginatedResult<ProductDto>>>
{
    private readonly AppDbContext _db;

    public GetProductsQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PaginatedResult<ProductDto>>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken)
    {
        // Build the query — no Product aggregate instantiated
        var queryable = _db.Products
            .AsNoTracking()                        // ← read-only, no change tracking
            .Where(p => !p.IsDeleted)
            .Where(p => query.CategoryId == null || p.CategoryId == query.CategoryId)
            .Where(p => query.Search == null
                        || p.Name.Contains(query.Search)
                        || p.Description.Contains(query.Search));

        var totalCount = await queryable.CountAsync(cancellationToken);

        // Project in SQL — only the columns the DTO needs come back from the DB
        var items = await queryable
            .OrderBy(p => p.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new ProductDto(         // ← projection happens in SQL
                p.Id,
                p.Name,
                p.Price,
                p.Slug,
                p.Category.Name                  // ← JOIN in SQL, not a navigation load
            ))
            .ToListAsync(cancellationToken);

        return Result<PaginatedResult<ProductDto>>.Ok(
            new PaginatedResult<ProductDto>(items, totalCount, query.Page, query.PageSize)
        );
    }
}
```

### Key details to notice

| Detail | Why |
|--------|-----|
| `AsNoTracking()` | EF doesn't track these objects — they'll never be saved |
| `.Select(p => new ProductDto(...))` | Projection in SQL means only needed columns are fetched |
| `p.Category.Name` inside `.Select()` | EF generates a JOIN; no separate round-trip |
| No `IProductRepository` | Repositories return aggregates. Queries access DbContext directly. |

---

## Two Repository Interfaces

Rules.md (Rule 28) allows a separate read repository:

```csharp
// Write repository — in Catalog.Domain/Interfaces
// Returns full aggregates. Used only by command handlers.
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
}

// Read repository — in Catalog.Domain/Interfaces (or Catalog.Application)
// Returns DTOs. Used only by query handlers. No aggregate instantiation.
public interface IProductReadRepository
{
    Task<PaginatedResult<ProductDto>> GetProductsAsync(GetProductsQuery query, CancellationToken ct = default);
    Task<ProductDetailDto?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<ProductDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
```

**When to use direct DbContext vs IProductReadRepository:**

- Simple projects: inject DbContext directly in query handlers (less abstraction, easier to read)
- When queries get complex or need reuse across handlers: extract into `IProductReadRepository`

Both are valid for this migration. Start with direct DbContext, extract repository when you have more than one handler sharing the same query logic.

---

## What Lives Where

```
Catalog.Application/
├── Queries/
│   └── GetProducts/
│       ├── GetProductsQuery.cs           ← record with parameters
│       └── GetProductsQueryHandler.cs    ← handler, injects DbContext or IProductReadRepository
│   └── GetProductBySlug/
│       ├── GetProductBySlugQuery.cs
│       └── GetProductBySlugQueryHandler.cs
├── DTOs/
│   ├── ProductDto.cs                     ← flat DTO for list views
│   └── ProductDetailDto.cs              ← richer DTO for single-item views

Catalog.Infrastructure/
└── ReadModels/
    └── ProductReadRepository.cs          ← if you extract the read repo
```

---

## What Does NOT Go in Query Handlers

| Don't do this | Why |
|---------------|-----|
| Load a `Product` aggregate and map it | Loading aggregate is for write side only |
| Call `_unitOfWork.SaveChangesAsync()` | Queries never save anything |
| Raise domain events | Queries have no side effects |
| Call `_productRepository.GetByIdAsync()` (write repo) | Write repos return aggregates; use DbContext or read repo |
| Add business logic (discount calculation, etc.) | Business logic belongs in the domain, not query handlers |

---

## Read vs Write DTO Shape

It's normal for read and write DTOs to look different:

```csharp
// Command DTO — what the client sends to CREATE a product
public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string Sku,
    Guid CategoryId
) : IRequest<Result<Guid>>;     // ← just returns the new Id

// Query DTO — what the client gets back when READING a product list
public record ProductDto(
    Guid Id,
    string Name,
    decimal Price,
    string Slug,
    string CategoryName
);

// Query DTO — richer view for a product detail page
public record ProductDetailDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Slug,
    string CategoryName,
    IReadOnlyList<string> ImageUrls,
    bool InStock,
    int StockQuantity
);
```

The detail DTO joins Catalog + Inventory data. That's fine — query handlers can read across
the conceptual bounded context boundaries via SQL. Only the write side (commands) must respect
aggregate and bounded context boundaries strictly.

---

## Summary: Command side vs Query side

| | Command Side | Query Side |
|-|-------------|------------|
| Purpose | Change state | Read state |
| Goes through | Aggregate | DbContext / read repo |
| Returns | `Result<T>` (usually just an Id or confirmation) | `Result<T>` with a DTO |
| Uses transactions | Yes (TransactionBehavior) | No |
| Raises domain events | Yes | No |
| Validates with FluentValidation | Yes | No (or minimal parameter validation) |
| Uses `AsNoTracking()` | Never | Always |
| Uses `.Select()` projection | Never | Always |

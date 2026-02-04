# Pagination Rewrite Plan

> **Purpose:** This document is a self-contained, step-by-step execution plan. Any developer or AI assistant can pick it up mid-way and continue. Every phase has exact file paths, exact code (before/after), and a checkbox at the bottom to track progress.

---

## Decision Summary

| Keep | Reason |
|---|---|
| `PaginatedResult<T>` as response wrapper | Metadata in body. Works natively with RTK Query — zero header config. Same pattern as Stripe, Google. |
| `ApiResponse<T>` envelope | Consistent across all endpoints already. |
| `QueryableExtensions.GetPagedDataAsync()` | Used by repositories. Stays as-is. |

| Adopt (from tutorial pattern) | Reason |
|---|---|
| `RequestParameters` abstract base class | Single source of truth for page/pageSize/search/sort. Eliminates copy-pasted `[FromQuery]` params across every controller. |
| Derived query parameter classes per feature | `ProductQueryParameters`, `PromoCodeQueryParameters`, `InventoryQueryParameters`, `OrderQueryParameters`. Each adds only the filters that endpoint needs. |
| FluentValidation validators per parameter class | Follows existing codebase pattern (`AddressDtoValidator`, `ProductQueryDtoValidator`). |

| Delete | Reason |
|---|---|
| `PagedList.cs` | Never instantiated anywhere. Tutorial scaffolding. |
| `MetaData.cs` | Only referenced by `PagedList.cs`. |
| `PagedRequest` class in `PaginatedResult.cs` | Never referenced anywhere. |
| `ProductQueryDto.cs` | Replaced by `ProductQueryParameters.cs`. |
| `ProductQueryDtoValidator.cs` | Replaced by `ProductQueryParametersValidator.cs`. |

---

## Codebase Conventions

**Every line of code in this plan follows these rules.** Do not deviate.

- **Namespaces:** File-scoped only. `namespace X.Y.Z;` — never block-scoped `namespace X { }`.
- **Using directives:** Only include namespaces that are actually referenced. Do not add `using System;` — it is globally available in .NET 10.
- **DTOs:** `ECommerce.Application/DTOs/{Feature}/`. Shared DTOs in `DTOs/Common/`.
- **Validators:** `ECommerce.Application/Validators/{Feature}/`. One validator class per DTO file.
- **Interfaces:** `ECommerce.Application/Interfaces/`. One interface per file.
- **Services:** `ECommerce.Application/Services/`. One class per file.
- **Naming:** Query parameter classes end in `QueryParameters`. Validators end in `Validator`. Example: `ProductQueryParameters` → `ProductQueryParametersValidator`.
- **Async:** Every async method ends in `Async` and has `CancellationToken cancellationToken = default` as the last parameter.
- **XML docs:** Public interface methods get `/// <summary>`. Implementations do not repeat them.
- **Property defaults:** The base class sets `Page = 1`, `PageSize = 20`. Derived classes only add new properties — they do not re-declare Page or PageSize unless they need a different cap.

---

## Query Parameter Name Contract

These are the exact names used in URL query strings, frontend RTK Query calls, and backend property names. They must all match.

| Query string | Backend property | Used by |
|---|---|---|
| `page` | `Page` | All paginated endpoints |
| `pageSize` | `PageSize` | All paginated endpoints |
| `search` | `Search` | Products, PromoCodes, Inventory |
| `sortBy` | `SortBy` | Products |
| `sortOrder` | `SortOrder` | Products |
| `categoryId` | `CategoryId` | Products |
| `minPrice` | `MinPrice` | Products |
| `maxPrice` | `MaxPrice` | Products |
| `minRating` | `MinRating` | Products |
| `isFeatured` | `IsFeatured` | Products |
| `isActive` | `IsActive` | PromoCodes |
| `lowStockOnly` | `LowStockOnly` | Inventory |

ASP.NET Core's `[FromQuery]` binding is **case-insensitive** by default. `page=1` binds to `Page`. No `[FromQuery("page")]` attributes needed.

---

## Phase 1 — Delete Dead Code

### 1.1 Delete `PagedList.cs`

**File:** `src/backend/ECommerce.Application/DTOs/Common/PagedList.cs`
**Action:** Delete the entire file. Never instantiated anywhere in the codebase.

### 1.2 Delete `MetaData.cs`

**File:** `src/backend/ECommerce.Application/DTOs/Common/MetaData.cs`
**Action:** Delete the entire file. Only referenced by `PagedList.cs` (also deleted).

### 1.3 Remove `PagedRequest` from `PaginatedResult.cs`

**File:** `src/backend/ECommerce.Application/DTOs/Common/PaginatedResult.cs`

Remove this entire class block. The file retains only `PaginatedResult<T>` and `ApiResponse<T>`.

**Remove:**
```csharp
public class PagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool Ascending { get; set; } = true;
}
```

**File after edit should look exactly like this:**
```csharp
namespace ECommerce.Application.DTOs.Common;

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Success"
        };
    }

    public static ApiResponse<T> Error(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}
```

---

## Phase 2 — Rewrite RequestParameters Base Class

**File:** `src/backend/ECommerce.Application/DTOs/Common/RequestParameters.cs`

This file already exists. Overwrite it completely with the following. The property renames (`PageNumber` → `Page`, `SearchTerm` → `Search`) must match the query string contract above.

**Full file content:**
```csharp
namespace ECommerce.Application.DTOs.Common;

/// <summary>
/// Abstract base class for paginated query parameters.
/// Provides shared pagination, search, and sort properties.
/// Derive a class per feature and add only the filter properties that endpoint needs.
/// </summary>
public abstract class RequestParameters
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    /// <summary>
    /// Page number (1-based). Defaults to 1. Values less than 1 are clamped to 1.
    /// </summary>
    public int Page
    {
        get => _page;
        set => _page = value > 0 ? value : 1;
    }

    /// <summary>
    /// Items per page. Clamped between 1 and 100. Defaults to 20.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is > 0 and <= MaxPageSize ? value : DefaultPageSize;
    }

    /// <summary>
    /// Free-text search term. Applied to searchable fields defined by each feature.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Field name to sort by (e.g. "name", "price", "createdAt").
    /// Validated per endpoint via FluentValidation.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction: "asc" (default) or "desc".
    /// </summary>
    public string SortOrder { get; set; } = "asc";

    /// <summary>
    /// Number of rows to skip for the current page. Use this instead of manual (Page-1)*PageSize.
    /// </summary>
    public int GetSkip() => (Page - 1) * PageSize;

    /// <summary>
    /// True when SortOrder is "desc" (case-insensitive).
    /// </summary>
    public bool IsDescending => SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
}
```

**What changed from current file:**
- `PageNumber` renamed to `Page`
- `SearchTerm` renamed to `Search`
- `DefaultPageSize` changed from 10 to 20 (matches the most common default used across controllers; the frontend always sends pageSize explicitly so the backend default only affects manual API calls)
- Removed nullable check on `SortOrder` — it is initialized to `"asc"` and can never be null
- XML doc comments tightened to match codebase style

---

## Phase 3 — Create Query Parameter Classes

### 3.1 Delete `ProductQueryDto.cs`

**File:** `src/backend/ECommerce.Application/DTOs/Products/ProductQueryDto.cs`
**Action:** Delete the entire file. Replaced by `ProductQueryParameters.cs`.

### 3.2 Create `ProductQueryParameters.cs`

**File:** `src/backend/ECommerce.Application/DTOs/Products/ProductQueryParameters.cs`

```csharp
using ECommerce.Application.DTOs.Common;

namespace ECommerce.Application.DTOs.Products;

/// <summary>
/// Query parameters for the product listing endpoint.
/// Inherits page, pageSize, search, sortBy, sortOrder from RequestParameters.
/// </summary>
public class ProductQueryParameters : RequestParameters
{
    public Guid? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinRating { get; set; }
    public bool? IsFeatured { get; set; }
}
```

### 3.3 Create `PromoCodeQueryParameters.cs`

**File:** `src/backend/ECommerce.Application/DTOs/PromoCodes/PromoCodeQueryParameters.cs`

```csharp
using ECommerce.Application.DTOs.Common;

namespace ECommerce.Application.DTOs.PromoCodes;

/// <summary>
/// Query parameters for the admin promo code listing endpoint.
/// Inherits page, pageSize, search from RequestParameters.
/// </summary>
public class PromoCodeQueryParameters : RequestParameters
{
    public bool? IsActive { get; set; }
}
```

### 3.4 Create `InventoryQueryParameters.cs`

**File:** `src/backend/ECommerce.Application/DTOs/Inventory/InventoryQueryParameters.cs`

```csharp
using ECommerce.Application.DTOs.Common;

namespace ECommerce.Application.DTOs.Inventory;

/// <summary>
/// Query parameters for the admin inventory listing endpoint.
/// Inherits page, pageSize, search from RequestParameters.
/// </summary>
public class InventoryQueryParameters : RequestParameters
{
    public bool? LowStockOnly { get; set; }
}
```

### 3.5 Create `OrderQueryParameters.cs`

**File:** `src/backend/ECommerce.Application/DTOs/Orders/OrderQueryParameters.cs`

```csharp
using ECommerce.Application.DTOs.Common;

namespace ECommerce.Application.DTOs.Orders;

/// <summary>
/// Query parameters for order listing endpoints.
/// Currently pagination-only. Add status/date filters here when needed.
/// </summary>
public class OrderQueryParameters : RequestParameters { }
```

---

## Phase 4 — Create / Replace Validators

### 4.1 Delete `ProductQueryDtoValidator.cs`

**File:** `src/backend/ECommerce.Application/Validators/Products/ProductQueryDtoValidator.cs`
**Action:** Delete the entire file. Replaced by `ProductQueryParametersValidator.cs`.

### 4.2 Create `ProductQueryParametersValidator.cs`

**File:** `src/backend/ECommerce.Application/Validators/Products/ProductQueryParametersValidator.cs`

```csharp
using FluentValidation;
using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Validators.Products;

public class ProductQueryParametersValidator : AbstractValidator<ProductQueryParameters>
{
    public ProductQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(x => x.MinPrice ?? 0)
            .When(x => x.MaxPrice.HasValue && x.MinPrice.HasValue);
        RuleFor(x => x.MinRating).InclusiveBetween(0, 5).When(x => x.MinRating.HasValue);
        RuleFor(x => x.SortBy)
            .Must(x => string.IsNullOrEmpty(x) || new[] { "name", "price-asc", "price-desc", "rating", "newest" }.Contains(x))
            .WithMessage("Invalid sortBy value");
    }
}
```

### 4.3 Create `PromoCodeQueryParametersValidator.cs`

**File:** `src/backend/ECommerce.Application/Validators/PromoCodes/PromoCodeQueryParametersValidator.cs`
**Note:** Create the `PromoCodes` folder under `Validators` if it does not exist.

```csharp
using FluentValidation;
using ECommerce.Application.DTOs.PromoCodes;

namespace ECommerce.Application.Validators.PromoCodes;

public class PromoCodeQueryParametersValidator : AbstractValidator<PromoCodeQueryParameters>
{
    public PromoCodeQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
```

### 4.4 Create `InventoryQueryParametersValidator.cs`

**File:** `src/backend/ECommerce.Application/Validators/Inventory/InventoryQueryParametersValidator.cs`
**Note:** Create the `Inventory` folder under `Validators` if it does not exist.

```csharp
using FluentValidation;
using ECommerce.Application.DTOs.Inventory;

namespace ECommerce.Application.Validators.Inventory;

public class InventoryQueryParametersValidator : AbstractValidator<InventoryQueryParameters>
{
    public InventoryQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
```

### 4.5 Create `OrderQueryParametersValidator.cs`

**File:** `src/backend/ECommerce.Application/Validators/Orders/OrderQueryParametersValidator.cs`

```csharp
using FluentValidation;
using ECommerce.Application.DTOs.Orders;

namespace ECommerce.Application.Validators.Orders;

public class OrderQueryParametersValidator : AbstractValidator<OrderQueryParameters>
{
    public OrderQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
```

---

## Phase 5 — Update Service Interfaces

### 5.1 `IProductService.cs`

**File:** `src/backend/ECommerce.Application/Interfaces/IProductService.cs`

**Single change — one method signature:**

Replace:
```csharp
Task<PaginatedResult<ProductDto>> GetProductsAsync(ProductQueryDto query, CancellationToken cancellationToken = default);
```
With:
```csharp
Task<PaginatedResult<ProductDto>> GetProductsAsync(ProductQueryParameters parameters, CancellationToken cancellationToken = default);
```

No other methods in this interface change. `GetAllProductsAsync`, `SearchProductsAsync`, etc. keep their current signatures — they are called internally by `ProductService` and are out of scope for this rewrite.

### 5.2 `IPromoCodeService.cs`

**File:** `src/backend/ECommerce.Application/Interfaces/IPromoCodeService.cs`

Replace:
```csharp
Task<PaginatedResult<PromoCodeDto>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null, CancellationToken cancellationToken = default);
```
With:
```csharp
Task<PaginatedResult<PromoCodeDto>> GetAllAsync(PromoCodeQueryParameters parameters, CancellationToken cancellationToken = default);
```

**Add using if missing:**
```csharp
using ECommerce.Application.DTOs.PromoCodes;
```

### 5.3 `IInventoryService.cs`

**File:** `src/backend/ECommerce.Application/Interfaces/IInventoryService.cs`

Replace:
```csharp
Task<List<InventoryDto>> GetAllInventoryAsync(int page = 1, int pageSize = 50, string? search = null, bool? lowStockOnly = null, CancellationToken cancellationToken = default);
```
With:
```csharp
Task<List<InventoryDto>> GetAllInventoryAsync(InventoryQueryParameters parameters, CancellationToken cancellationToken = default);
```

**`GetInventoryHistoryAsync` stays unchanged.** It is a sub-resource endpoint (`/inventory/{id}/history`) with only `page` and `pageSize`. Inline params are appropriate here — no parameter object needed for two params.

### 5.4 `IOrderService.cs`

**File:** `src/backend/ECommerce.Application/Interfaces/IOrderService.cs`

Replace:
```csharp
Task<PaginatedResult<OrderDto>> GetUserOrdersAsync(Guid userId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
```
With:
```csharp
Task<PaginatedResult<OrderDto>> GetUserOrdersAsync(Guid userId, OrderQueryParameters parameters, CancellationToken cancellationToken = default);
```

Replace:
```csharp
Task<PaginatedResult<OrderDto>> GetAllOrdersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
```
With:
```csharp
Task<PaginatedResult<OrderDto>> GetAllOrdersAsync(OrderQueryParameters parameters, CancellationToken cancellationToken = default);
```

---

## Phase 6 — Update Service Implementations

### 6.1 `ProductService.cs`

**File:** `src/backend/ECommerce.Application/Services/ProductService.cs`

**Change 1 — `GetProductsAsync` method. Replace entire method:**

Old:
```csharp
public async Task<PaginatedResult<ProductDto>> GetProductsAsync(ProductQueryDto query, CancellationToken cancellationToken = default)
{
    var page = query?.Page ?? 1;
    var pageSize = query?.PageSize ?? 8;
    var skip = (page - 1) * pageSize;

    var (products, totalCount) = await _unitOfWork.Products.GetProductsWithFiltersAsync(
        skip,
        pageSize,
        query?.CategoryId,
        query?.Search,
        query?.MinPrice,
        query?.MaxPrice,
        query?.MinRating,
        query?.IsFeatured,
        query?.SortBy,
        cancellationToken: cancellationToken);

    return new PaginatedResult<ProductDto>
    {
        Items = products.Select(p => _mapper.Map<ProductDto>(p)).ToList(),
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
```

New:
```csharp
public async Task<PaginatedResult<ProductDto>> GetProductsAsync(ProductQueryParameters parameters, CancellationToken cancellationToken = default)
{
    var (products, totalCount) = await _unitOfWork.Products.GetProductsWithFiltersAsync(
        parameters.GetSkip(),
        parameters.PageSize,
        parameters.CategoryId,
        parameters.Search,
        parameters.MinPrice,
        parameters.MaxPrice,
        parameters.MinRating,
        parameters.IsFeatured,
        parameters.SortBy,
        cancellationToken: cancellationToken);

    return new PaginatedResult<ProductDto>
    {
        Items = products.Select(p => _mapper.Map<ProductDto>(p)).ToList(),
        TotalCount = totalCount,
        Page = parameters.Page,
        PageSize = parameters.PageSize
    };
}
```

**Change 2 — `GetAllProductsAsync` method. Replace the internal call only:**

Old:
```csharp
var query = new ProductQueryDto { Page = page, PageSize = pageSize };
return await GetProductsAsync(query, cancellationToken);
```

New:
```csharp
var parameters = new ProductQueryParameters { Page = page, PageSize = pageSize };
return await GetProductsAsync(parameters, cancellationToken);
```

### 6.2 `PromoCodeService.cs`

**File:** `src/backend/ECommerce.Application/Services/PromoCodeService.cs`

Replace the entire `GetAllAsync` method:

Old:
```csharp
public async Task<PaginatedResult<PromoCodeDto>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null, CancellationToken cancellationToken = default)
{
    var allPromoCodes = await _unitOfWork.PromoCodes.GetAllAsync(cancellationToken: cancellationToken);
    var filtered = allPromoCodes.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var searchTerm = search.ToLowerInvariant();
        filtered = filtered.Where(p => p.Code.ToLower().Contains(searchTerm));
    }

    if (isActive.HasValue)
    {
        filtered = filtered.Where(p => p.IsActive == isActive.Value);
    }

    var totalCount = filtered.Count();

    var items = filtered
        .OrderByDescending(p => p.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    var dtos = _mapper.Map<List<PromoCodeDto>>(items);

    return new PaginatedResult<PromoCodeDto>
    {
        Items = dtos,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
```

New:
```csharp
public async Task<PaginatedResult<PromoCodeDto>> GetAllAsync(PromoCodeQueryParameters parameters, CancellationToken cancellationToken = default)
{
    var allPromoCodes = await _unitOfWork.PromoCodes.GetAllAsync(cancellationToken: cancellationToken);
    var filtered = allPromoCodes.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(parameters.Search))
    {
        var searchTerm = parameters.Search.ToLowerInvariant();
        filtered = filtered.Where(p => p.Code.ToLower().Contains(searchTerm));
    }

    if (parameters.IsActive.HasValue)
    {
        filtered = filtered.Where(p => p.IsActive == parameters.IsActive.Value);
    }

    var totalCount = filtered.Count();

    var items = filtered
        .OrderByDescending(p => p.CreatedAt)
        .Skip(parameters.GetSkip())
        .Take(parameters.PageSize)
        .ToList();

    var dtos = _mapper.Map<List<PromoCodeDto>>(items);

    return new PaginatedResult<PromoCodeDto>
    {
        Items = dtos,
        TotalCount = totalCount,
        Page = parameters.Page,
        PageSize = parameters.PageSize
    };
}
```

### 6.3 `InventoryService.cs`

**File:** `src/backend/ECommerce.Application/Services/InventoryService.cs`

Replace the entire `GetAllInventoryAsync` method:

Old:
```csharp
public async Task<List<InventoryDto>> GetAllInventoryAsync(int page = 1, int pageSize = 50, string? search = null, bool? lowStockOnly = null, CancellationToken cancellationToken = default)
{
    var allProducts = await _unitOfWork.Products.GetAllAsync(cancellationToken: cancellationToken);
    var query = allProducts.AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var searchLower = search.ToLower();
        query = query.Where(p => p.Name.ToLower().Contains(searchLower) ||
                               (p.Sku != null && p.Sku.ToLower().Contains(searchLower)));
    }

    if (lowStockOnly == true)
    {
        query = query.Where(p => p.StockQuantity <= p.LowStockThreshold);
    }

    query = query.OrderBy(p => p.StockQuantity).ThenBy(p => p.Name);

    var products = query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    return products.Select(p => _mapper.Map<InventoryDto>(p)).ToList();
}
```

New:
```csharp
public async Task<List<InventoryDto>> GetAllInventoryAsync(InventoryQueryParameters parameters, CancellationToken cancellationToken = default)
{
    var allProducts = await _unitOfWork.Products.GetAllAsync(cancellationToken: cancellationToken);
    var query = allProducts.AsQueryable();

    if (!string.IsNullOrWhiteSpace(parameters.Search))
    {
        var searchLower = parameters.Search.ToLower();
        query = query.Where(p => p.Name.ToLower().Contains(searchLower) ||
                               (p.Sku != null && p.Sku.ToLower().Contains(searchLower)));
    }

    if (parameters.LowStockOnly == true)
    {
        query = query.Where(p => p.StockQuantity <= p.LowStockThreshold);
    }

    query = query.OrderBy(p => p.StockQuantity).ThenBy(p => p.Name);

    var products = query
        .Skip(parameters.GetSkip())
        .Take(parameters.PageSize)
        .ToList();

    return products.Select(p => _mapper.Map<InventoryDto>(p)).ToList();
}
```

### 6.4 `OrderService.cs`

**File:** `src/backend/ECommerce.Application/Services/OrderService.cs`

**Change 1 — `GetUserOrdersAsync`. Replace entire method:**

Old:
```csharp
public async Task<PaginatedResult<OrderDto>> GetUserOrdersAsync(Guid userId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Retrieving orders for user {UserId}, page {Page}", userId, page);

    var totalCount = await _unitOfWork.Orders.GetUserOrdersCountAsync(userId, cancellationToken: cancellationToken);
    var orders = await _unitOfWork.Orders.GetUserOrdersAsync(userId, (page - 1) * pageSize, pageSize, cancellationToken: cancellationToken);
    var dtos = orders.Select(o => _mapper.Map<OrderDto>(o)).ToList();

    return new PaginatedResult<OrderDto>
    {
        Items = dtos,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
```

New:
```csharp
public async Task<PaginatedResult<OrderDto>> GetUserOrdersAsync(Guid userId, OrderQueryParameters parameters, CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Retrieving orders for user {UserId}, page {Page}", userId, parameters.Page);

    var totalCount = await _unitOfWork.Orders.GetUserOrdersCountAsync(userId, cancellationToken: cancellationToken);
    var orders = await _unitOfWork.Orders.GetUserOrdersAsync(userId, parameters.GetSkip(), parameters.PageSize, cancellationToken: cancellationToken);
    var dtos = orders.Select(o => _mapper.Map<OrderDto>(o)).ToList();

    return new PaginatedResult<OrderDto>
    {
        Items = dtos,
        TotalCount = totalCount,
        Page = parameters.Page,
        PageSize = parameters.PageSize
    };
}
```

**Change 2 — `GetAllOrdersAsync`. Replace entire method:**

Old:
```csharp
public async Task<PaginatedResult<OrderDto>> GetAllOrdersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Retrieving all orders, page {Page}", page);

    var allOrders = await _unitOfWork.Orders.GetAllAsync(trackChanges: false, cancellationToken: cancellationToken);
    var totalCount = allOrders.Count();

    var orders = allOrders
        .OrderByDescending(o => o.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    var dtos = orders.Select(o => _mapper.Map<OrderDto>(o)).ToList();

    return new PaginatedResult<OrderDto>
    {
        Items = dtos,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
```

New:
```csharp
public async Task<PaginatedResult<OrderDto>> GetAllOrdersAsync(OrderQueryParameters parameters, CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Retrieving all orders, page {Page}", parameters.Page);

    var allOrders = await _unitOfWork.Orders.GetAllAsync(trackChanges: false, cancellationToken: cancellationToken);
    var totalCount = allOrders.Count();

    var orders = allOrders
        .OrderByDescending(o => o.CreatedAt)
        .Skip(parameters.GetSkip())
        .Take(parameters.PageSize)
        .ToList();

    var dtos = orders.Select(o => _mapper.Map<OrderDto>(o)).ToList();

    return new PaginatedResult<OrderDto>
    {
        Items = dtos,
        TotalCount = totalCount,
        Page = parameters.Page,
        PageSize = parameters.PageSize
    };
}
```

---

## Phase 7 — Update Controllers

### 7.1 `ProductsController.cs`

**File:** `src/backend/ECommerce.API/Controllers/ProductsController.cs`

Find the `GetProducts` action method. Make two changes:

**Change 1 — parameter:**
```csharp
// From:
[FromQuery] ProductQueryDto query
// To:
[FromQuery] ProductQueryParameters parameters
```

**Change 2 — service call in the method body:**
```csharp
// From:
var result = await _productService.GetProductsAsync(query, cancellationToken);
// To:
var result = await _productService.GetProductsAsync(parameters, cancellationToken);
```

**Change 3 — log message (if present), replace `query.` with `parameters.`:**
```csharp
// From (example pattern):
_logger.LogInformation("...", query.Page, query.PageSize);
// To:
_logger.LogInformation("...", parameters.Page, parameters.PageSize);
```

### 7.2 `PromoCodesController.cs`

**File:** `src/backend/ECommerce.API/Controllers/PromoCodesController.cs`

Find the `GetAllPromoCodes` action. Replace the entire parameter list and log/service call.

**Parameters — from:**
```csharp
public async Task<IActionResult> GetAllPromoCodes(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? search = null,
    [FromQuery] bool? isActive = null,
    CancellationToken cancellationToken = default)
```

**To:**
```csharp
public async Task<IActionResult> GetAllPromoCodes(
    [FromQuery] PromoCodeQueryParameters parameters,
    CancellationToken cancellationToken = default)
```

**Log + service call — from:**
```csharp
_logger.LogInformation("Retrieving promo codes (page: {Page}, pageSize: {PageSize}, search: {Search}, isActive: {IsActive})",
    page, pageSize, search, isActive);

var result = await _promoCodeService.GetAllAsync(page, pageSize, search, isActive, cancellationToken: cancellationToken);
```

**To:**
```csharp
_logger.LogInformation("Retrieving promo codes (page: {Page}, pageSize: {PageSize}, search: {Search}, isActive: {IsActive})",
    parameters.Page, parameters.PageSize, parameters.Search, parameters.IsActive);

var result = await _promoCodeService.GetAllAsync(parameters, cancellationToken: cancellationToken);
```

### 7.3 `InventoryController.cs`

**File:** `src/backend/ECommerce.API/Controllers/InventoryController.cs`

Find the `GetAllInventory` action. Replace params and body.

**Parameters — from:**
```csharp
public async Task<IActionResult> GetAllInventory(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    [FromQuery] string? search = null,
    [FromQuery] bool? lowStockOnly = null,
    CancellationToken cancellationToken = default)
```

**To:**
```csharp
public async Task<IActionResult> GetAllInventory(
    [FromQuery] InventoryQueryParameters parameters,
    CancellationToken cancellationToken = default)
```

**Log + service call — from:**
```csharp
_logger.LogInformation("Retrieving inventory (page: {Page}, pageSize: {PageSize}, search: {Search}, lowStockOnly: {LowStockOnly})",
    page, pageSize, search, lowStockOnly);

var inventory = await _inventoryService.GetAllInventoryAsync(page, pageSize, search, lowStockOnly, cancellationToken: cancellationToken);
```

**To:**
```csharp
_logger.LogInformation("Retrieving inventory (page: {Page}, pageSize: {PageSize}, search: {Search}, lowStockOnly: {LowStockOnly})",
    parameters.Page, parameters.PageSize, parameters.Search, parameters.LowStockOnly);

var inventory = await _inventoryService.GetAllInventoryAsync(parameters, cancellationToken: cancellationToken);
```

**`GetInventoryHistory` — no changes.** Keeps inline `[FromQuery] int page = 1, [FromQuery] int pageSize = 50`.

### 7.4 `OrdersController.cs`

**File:** `src/backend/ECommerce.API/Controllers/OrdersController.cs`

**Change 1 — `GetMyOrders` action:**

Parameters from:
```csharp
public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
```
To:
```csharp
public async Task<IActionResult> GetMyOrders([FromQuery] OrderQueryParameters parameters, CancellationToken cancellationToken = default)
```

Body from:
```csharp
_logger.LogInformation("Retrieving orders for user {UserId}, page {Page}", userId, page);
var result = await _orderService.GetUserOrdersAsync(userId, page, pageSize, cancellationToken: cancellationToken);
```
To:
```csharp
_logger.LogInformation("Retrieving orders for user {UserId}, page {Page}", userId, parameters.Page);
var result = await _orderService.GetUserOrdersAsync(userId, parameters, cancellationToken: cancellationToken);
```

**Change 2 — `GetAllOrders` action:**

Parameters from:
```csharp
public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
```
To:
```csharp
public async Task<IActionResult> GetAllOrders([FromQuery] OrderQueryParameters parameters, CancellationToken cancellationToken = default)
```

Body from:
```csharp
_logger.LogInformation("Retrieving all orders, page {Page}", page);
var result = await _orderService.GetAllOrdersAsync(page, pageSize, cancellationToken: cancellationToken);
```
To:
```csharp
_logger.LogInformation("Retrieving all orders, page {Page}", parameters.Page);
var result = await _orderService.GetAllOrdersAsync(parameters, cancellationToken: cancellationToken);
```

---

## Phase 8 — Update Unit Tests

### 8.1 `ProductServiceTests.cs`

**File:** `src/backend/ECommerce.Tests/Unit/Services/ProductServiceTests.cs`

Search and replace all occurrences:
- `ProductQueryDto` → `ProductQueryParameters`

Example (every call follows this pattern):
```csharp
// From:
var result = await _service.GetProductsAsync(new ProductQueryDto { Page = 1, PageSize = 10 });

// To:
var result = await _service.GetProductsAsync(new ProductQueryParameters { Page = 1, PageSize = 10 });
```

No mock setup changes needed — the repository mocks use `It.IsAny<int>()` which matches regardless of how skip/take are computed.

### 8.2 `OrderServiceTests.cs`

**File:** `src/backend/ECommerce.Tests/Unit/Services/OrderServiceTests.cs`

**Change 1 — `GetUserOrdersAsync` test calls:**
```csharp
// From:
var result = await _service.GetUserOrdersAsync(userId, page: 1, pageSize: 10);

// To:
var result = await _service.GetUserOrdersAsync(userId, new OrderQueryParameters { Page = 1, PageSize = 10 });
```

**Change 2 — `GetAllOrdersAsync` test calls:**
```csharp
// From:
var result = await _service.GetAllOrdersAsync(page: 1, pageSize: 20);

// To:
var result = await _service.GetAllOrdersAsync(new OrderQueryParameters { Page = 1, PageSize = 20 });
```

**Mock setups:** The repository mock for `GetUserOrdersAsync` currently uses `.Setup(r => r.GetUserOrdersAsync(userId, 0, 10))`. With `OrderQueryParameters { Page = 1, PageSize = 10 }`, `GetSkip()` = 0 and PageSize = 10. The values are identical — **no mock change needed**.

**Add using if missing:**
```csharp
using ECommerce.Application.DTOs.Orders;
```

### 8.3 `PromoCodeServiceTests.cs`

**File:** `src/backend/ECommerce.Tests/Unit/Services/PromoCodeServiceTests.cs`

Replace all `GetAllAsync` calls:

```csharp
// From:
var result = await _service.GetAllAsync(page: 1, pageSize: 10);
// To:
var result = await _service.GetAllAsync(new PromoCodeQueryParameters { Page = 1, PageSize = 10 });

// From:
var result = await _service.GetAllAsync(page: 1, pageSize: 10, search: "SAVE");
// To:
var result = await _service.GetAllAsync(new PromoCodeQueryParameters { Page = 1, PageSize = 10, Search = "SAVE" });

// From:
var result = await _service.GetAllAsync(page: 1, pageSize: 10, isActive: true);
// To:
var result = await _service.GetAllAsync(new PromoCodeQueryParameters { Page = 1, PageSize = 10, IsActive = true });
```

**Add using if missing:**
```csharp
using ECommerce.Application.DTOs.PromoCodes;
```

---

## Phase 9 — Integration Tests

**No changes needed.** Here is why:

ASP.NET Core binds `[FromQuery]` object properties by matching query parameter names to property names **case-insensitively**. The existing integration test query strings already use the correct names:

| Test query string | Binds to |
|---|---|
| `?page=1&pageSize=10` | `RequestParameters.Page`, `RequestParameters.PageSize` |
| `?page=1&pageSize=10&search=X` | + `RequestParameters.Search` |

Run the integration tests after Phase 7 completes. If any endpoint returns `400 Bad Request` that previously returned `200`, the cause will be a property name mismatch — check the table in the "Query Parameter Name Contract" section above.

---

## Phase 10 — Frontend Impact

**No code changes required in this rewrite.** Confirmed by checking every frontend API call:

| Frontend sends | Backend receives | Match? |
|---|---|---|
| `page=1` | `Page` | Yes (case-insensitive) |
| `pageSize=12` | `PageSize` | Yes |
| `search=shoes` | `Search` | Yes |
| `categoryId=...` | `CategoryId` | Yes |
| `minPrice=10` | `MinPrice` | Yes |
| `sortBy=newest` | `SortBy` | Yes |
| `isActive=true` | `IsActive` | Yes |
| `lowStockOnly=true` | `LowStockOnly` | Yes |

**Response shape is unchanged.** The frontend expects:
```typescript
{
  items: T[],
  totalCount: number,
  page: number,
  pageSize: number
}
```
This is exactly what `PaginatedResult<T>` produces. No frontend changes.

**Known pre-existing issue (out of scope):** `InventoryService.GetAllInventoryAsync` returns `List<InventoryDto>` instead of `PaginatedResult<InventoryDto>`. The admin inventory page does not receive `totalCount` from the backend. This should be fixed in a follow-up — not part of this rewrite.

---

## Phase 11 — Build & Verify

After all phases are complete:

**Step 1 — Build:**
```bash
cd src/backend
dotnet build ECommerce.sln --no-restore
```
Expected: **0 errors.**

**Step 2 — Tests:**
```bash
dotnet test ECommerce.Tests --no-restore
```

**If build errors occur, check in this order:**

1. **`ProductQueryDto` not found** → You forgot to delete the old file or a reference still exists. Search the entire solution for `ProductQueryDto` — replace with `ProductQueryParameters`.
2. **`PagedRequest` not found** → Something still references the deleted class. Search for `PagedRequest`.
3. **`MetaData` not found** → Something still references the deleted class. Search for `MetaData` (but not `MetaData` inside strings or comments).
4. **Method signature mismatch on service** → The controller or test is calling the old signature (e.g., `GetAllAsync(page, pageSize, ...)`). Replace with the parameter object version.
5. **Property `PageNumber` not found** → `RequestParameters` was renamed to `Page`. Search for `PageNumber` and replace with `Page`.
6. **Property `SearchTerm` not found** → `RequestParameters` was renamed to `Search`. Search for `SearchTerm` and replace with `Search`.

---

## Execution Checklist

Track progress here. Mark each item as done when complete. Do not skip items — the phases build on each other.

```
Phase 1 — Delete Dead Code
[ ] 1.1  Delete PagedList.cs
[ ] 1.2  Delete MetaData.cs
[ ] 1.3  Remove PagedRequest class from PaginatedResult.cs

Phase 2 — Base Class
[ ] 2.0  Overwrite RequestParameters.cs

Phase 3 — Query Parameter Classes
[ ] 3.1  Delete ProductQueryDto.cs
[ ] 3.2  Create ProductQueryParameters.cs
[ ] 3.3  Create PromoCodeQueryParameters.cs
[ ] 3.4  Create InventoryQueryParameters.cs
[ ] 3.5  Create OrderQueryParameters.cs

Phase 4 — Validators
[ ] 4.1  Delete ProductQueryDtoValidator.cs
[ ] 4.2  Create ProductQueryParametersValidator.cs
[ ] 4.3  Create PromoCodeQueryParametersValidator.cs
[ ] 4.4  Create InventoryQueryParametersValidator.cs
[ ] 4.5  Create OrderQueryParametersValidator.cs

Phase 5 — Service Interfaces
[ ] 5.1  Update IProductService.cs
[ ] 5.2  Update IPromoCodeService.cs
[ ] 5.3  Update IInventoryService.cs
[ ] 5.4  Update IOrderService.cs

Phase 6 — Service Implementations
[ ] 6.1  Update ProductService.cs
[ ] 6.2  Update PromoCodeService.cs
[ ] 6.3  Update InventoryService.cs
[ ] 6.4  Update OrderService.cs

Phase 7 — Controllers
[ ] 7.1  Update ProductsController.cs
[ ] 7.2  Update PromoCodesController.cs
[ ] 7.3  Update InventoryController.cs
[ ] 7.4  Update OrdersController.cs

Phase 8 — Unit Tests
[ ] 8.1  Update ProductServiceTests.cs
[ ] 8.2  Update OrderServiceTests.cs
[ ] 8.3  Update PromoCodeServiceTests.cs

Phase 9 — Integration Tests
[ ] 9.0  Run integration tests — confirm no query string changes needed

Phase 10 — Frontend
[ ] 10.0 Confirm no frontend changes required

Phase 11 — Verify
[ ] 11.1 dotnet build — 0 errors
[ ] 11.2 dotnet test — all pass
```

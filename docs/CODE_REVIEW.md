# Code Review: E-Commerce Backend Application

> Review Date: 2026-02-28
> Reviewer: Senior .NET Developer (10+ years experience)

---

## Executive Summary

The codebase demonstrates solid architectural decisions (Repository pattern, Unit of Work, Dependency Injection) but has several issues ranging from **N+1 query problems** to code smells and best practice violations.

---

## ✅ Fixes Applied (2026-02-28)

### Issue #1: N+1 Query in CategoryService ✅
- Changed `GetProductCountAsync` to use SQL `COUNT()` instead of loading entities
- Added batch `GetProductCountsAsync` method to get all counts in a single query
- Updated `CategoryService.GetTopLevelCategoriesAsync` to use batch method

### Issue #2: Pagination Bug in ProductService ✅
- Added overloaded `GetFeaturedAsync(skip, count)` method in repository
- Updated `ProductService.GetFeaturedProductsAsync` to properly paginate

### Issue #3: Inefficient Cart Total Calculation ✅
- Changed `CalculateTotalAsync` to use SQL `SUM(Quantity * Price)` aggregation
- No longer loads cart items into memory just to calculate total

### Issue #4: Over-fetching Reviews in ProductRepository ✅
- Removed `.Include(p => p.Reviews)` from `GetBySlugAsync`
- Reviews can be fetched separately via `ReviewRepository` if needed

### Issue #5: Reflection-based Sorting ⏸️ DEFERRED
- Low priority - requires significant refactoring using Expression trees

---

## 🚨 Critical Issues

### 1. N+1 Query Problem in `CategoryService.GetTopLevelCategoriesAsync()` ✅ FIXED
**Location**: `src/backend/ECommerce.Application/Services/CategoryService.cs:28-40`

```csharp
foreach (var dto in dtos)
{
    dto.ProductCount = await _unitOfWork.Categories.GetProductCountAsync(dto.Id, cancellationToken: cancellationToken);
}
```

**Impact**: For 10 categories, this makes 11 database queries (1 for categories + 10 for counts).

**Fix**: Add a single grouped query to get all product counts at once.

---

### 2. Incorrect Pagination in `ProductService.GetFeaturedProductsAsync()` ✅ FIXED
**Location**: `src/backend/ECommerce.Application/Services/ProductService.cs:116-129`

```csharp
var products = await _unitOfWork.Products.GetFeaturedAsync(pageSize); // Bug: ignores page parameter
TotalCount = products.Count(); // Wrong: uses collection count, not total
```

**Impact**: Pagination is broken - always returns only `pageSize` items regardless of total.

---

## ⚠️ Performance Issues

### 3. Over-fetching in `ProductRepository.GetBySlugAsync()` ✅ FIXED
**Location**: `src/backend/ECommerce.Infrastructure/Repositories/ProductRepository.cs:19-27`

```csharp
.Include(p => p.Reviews)  // Loads ALL reviews - could be thousands
```

**Impact**: For a popular product with 1000+ reviews, this loads unnecessary data.

---

### 4. Inefficient `CartRepository.CalculateTotalAsync()` ✅ FIXED
**Location**: `src/backend/ECommerce.Infrastructure/Repositories/CartRepository.cs:68-79`

```csharp
var cart = await DbSet.Include(c => c.Items).ThenInclude(...).FirstOrDefaultAsync(...);
return cart.Items.Sum(item => item.Quantity * item.Product.Price);
```

**Impact**: Loads entire cart with all images just to calculate a sum.

---

### 5. Reflection-based Sorting in `QueryableExtensions.ApplySort()` ⏸️ DEFERRED
**Location**: `src/backend/ECommerce.Infrastructure/Extensions/QueryableExtensions.cs:45-70`

**Impact**: Reflection has performance overhead; repeated calls create new expressions.

---

### 6. Unbounded Review Loading in Filtered Queries
**Location**: `src/backend/ECommerce.Infrastructure/Repositories/ProductRepository.cs:102-107`

In `GetProductsWithFiltersAsync`, including reviews when filtering by rating:
```csharp
.Include(p => p.Reviews)  // Loads all reviews for all products in result set
```

**Impact**: With rating filter, still loads ALL reviews. For products with many reviews, this is memory-intensive.

---

## 🔧 Code Smells

### 7. Hardcoded Configuration in `OrderService`
**Location**: `src/backend/ECommerce.Application/Services/OrderService.cs:29-46`

```csharp
private static readonly Dictionary<string, string> CountryCodeMap = new(...) { ... };
```

**Issue**: Should be in configuration file or database.

---

### 8. Inconsistent Tracking Defaults in `Repository.GetAllAsync()`
**Location**: `src/backend/ECommerce.Infrastructure/Repositories/Repository.cs:43-47`

```csharp
public virtual async Task<IEnumerable<T>> GetAllAsync(bool trackChanges = true, ...)
```

**Issue**: Defaulting to tracking for read-only operations is wasteful. Should default to `false`.

---

### 9. Magic Strings for Address Types

In `OrderService.CreateOrderEntityAsync()`:
```csharp
shippingAddress.Type = "Shipping";  // Magic string
billingAddress.Type = "Billing";     // Magic string
```

**Fix**: Use constants or an enum.

---

### 10. Silent Error Swallowing in `OrderService.IncrementPromoCodeUsageAsync()`
**Location**: `src/backend/ECommerce.Application/Services/OrderService.cs:339-354`

```csharp
catch (Exception promoEx)
{
    _logger.LogError(promoEx, "Failed to increment usage count...");
    // Continue - order was created successfully
}
```

**Issue**: This is a business-critical operation. Silent failure could lead to promo code abuse.

---

### 11. Race Condition Handling in `CartService.AddToCartAsync()`
**Location**: `src/backend/ECommerce.Application/Services/CartService.cs:105-129`

The try-catch for duplicate key exception is good, but the fallback logic reloads the cart unnecessarily.

---

## 📋 Best Practice Violations

### 12. Missing `AsNoTracking()` in Read Operations
Multiple places don't specify `trackChanges: false` when only reading data, causing unnecessary change tracking overhead.

---

### 13. No Input Validation on Public APIs

Methods like `ProductService.SearchProductsAsync()` don't validate `pageSize` - a malicious client could request `pageSize = int.MaxValue`.

---

### 14. Inconsistent DateTime Usage
Mix of `DateTime.UtcNow` and potential local time without explicit `DateTimeKind`.

---

### 15. Missing CancellationToken Propagation
Some async methods don't fully propagate `CancellationToken` to all underlying operations.

---

## ✅ Positive Observations

1. **Good transaction handling** in OrderService with proper rollback on failure
2. **Consistent use of CancellationToken** for async operations
3. **Proper separation of concerns** with Repository and Service layers
4. **Good use of dependency injection** throughout
5. **Proper use of AsNoTracking()** for read operations
6. **Raw SQL for atomic operations** (stock reduction) to prevent race conditions

---

## 🆕 Additional Findings (2026-03-02)

### 16. WishlistService Loads ALL Entries In-Memory
**Location**: `src/backend/ECommerce.Application/Services/WishlistService.cs:32-35`

```csharp
var wishlistEntries = await _unitOfWork.Wishlists.GetAllAsync(trackChanges: false, cancellationToken);
var userWishlistEntries = wishlistEntries
    .Where(w => w.UserId == userId)
    .ToList();
```

**Impact**: MAJOR - Loads ALL wishlist entries from ALL users into memory, then filters. With 100K users, this could load millions of records.

**Recommendation**: Create a dedicated repository method:
```csharp
public async Task<IEnumerable<Wishlist>> GetByUserIdAsync(Guid userId, ...)
{
    return await DbSet
        .Where(w => w.UserId == userId)
        .Include(w => w.Product)
        .ThenInclude(p => p.Images)
        .ToListAsync(cancellationToken);
}
```

---

### 17. WishlistService.ClearWishlistAsync - Same Issue
**Location**: `src/backend/ECommerce.Application/Services/WishlistService.cs:98-101`

```csharp
var allWishlistEntries = await _unitOfWork.Wishlists.GetAllAsync(trackChanges: true, cancellationToken);
var userWishlistEntries = allWishlistEntries
    .Where(w => w.UserId == userId)
    .ToList();
```

**Impact**: Same as #16 - loads entire wishlist table.

---

### 18. WishlistService.RemoveFromWishlistAsync - Same Issue
**Location**: `src/backend/ECommerce.Application/Services/WishlistService.cs:73-75`

**Impact**: Same pattern - loads all wishlist entries.

---

### 19. CategoryRepository Includes Unnecessary Products Collection
**Location**: `src/backend/ECommerce.Infrastructure/Repositories/CategoryRepository.cs:27`

```csharp
.Include(c => c.Products)  // Unnecessary - loads ALL products in category
```

**Impact**: When getting a category by slug, this loads all products even when only category details are needed.

**Recommendation**: Remove unless specifically needed for the use case.

---

### 20. ProductRepository Still Includes Reviews in Filtered Query
**Location**: `src/backend/ECommerce.Infrastructure/Repositories/ProductRepository.cs:121`

```csharp
.Include(p => p.Category)
.Include(p => p.Images)
.Include(p => p.Reviews)  // Still loads ALL reviews for pagination!
```

**Impact**: When paginating 20 products with 100 reviews each, loads 2000 review records unnecessarily. The reviews are used for rating filter/sort but shouldn't be loaded into memory.

**Recommendation**: Use projection or computed columns for rating:
```csharp
// Option 1: Use raw SQL with computed average
// Option 2: Create a separate ProductSummaryDto that excludes Reviews
// Option 3: Only include Reviews when explicitly requested
```

---

### 21. AutoMapper Profile Forces Reviews Loading
**Location**: `src/backend/ECommerce.Application/MappingProfile.cs:43-45, 51-53`

```csharp
.ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src =>
    src.Reviews.Any() ? src.Reviews.Average(r => (decimal)r.Rating) : 0))
```

**Impact**: Forces Reviews to be loaded for every product listing just to calculate average rating. Should be calculated at database level.

**Recommendation**: Calculate AverageRating and ReviewCount at repository level using SQL aggregation, not in mapping.

---

### 22. ProductService.GetFeaturedProductsAsync Uses Wrong Total Count
**Location**: `src/backend/ECommerce.Application/Services/ProductService.cs:119`

```csharp
var totalCount = await _unitOfWork.Products.GetActiveProductsCountAsync(); // BUG: Should be featured count!
```

**Impact**: Pagination shows wrong total count - shows ALL active products instead of featured products.

**Fix**: Add `GetFeaturedProductsCountAsync()` to repository and use it.

---

### 23. CartService Unnecessarily Reloads Cart
**Location**: `src/backend/ECommerce.Application/Services/CartService.cs:134, 183, 200`

```csharp
// After every modification, reloads entire cart
var cart = await _unitOfWork.Carts.GetCartWithItemsAsync(cartId, cancellationToken);
```

**Impact**: Performance hit - reloads cart after every add/update/remove. The cart entity is already in memory after modifications.

**Recommendation**: Instead of reloading, either:
1. Use the existing cart entity from memory
2. Or use AsNoTracking() for the modification queries and accept stale data temporarily

---

### 24. OrderService Creates New Dictionary Per Instance
**Location**: `src/backend/ECommerce.Application/Services/OrderService.cs:29-46`

```csharp
private static readonly Dictionary<string, string> CountryCodeMap = ...
```

**Issue**: While marked `static readonly`, the dictionary is created when the service instance is created. For high-traffic scenarios, this should be a static readonly field at class level or injected as singleton.

---

### 25. Inconsistent Null Handling in Services
**Location**: Multiple places

**Issue**: Some methods check for null and throw exceptions, others return null or default. Creates unpredictable API behavior.

**Recommendation**: Establish a consistent failure-handling policy per feature/flow:
- Repository methods: Return null for not found (standard pattern)
- Service methods: Use one style consistently in a flow (either `Result<T>` or typed exceptions)
- Use `ArgumentNullException.ThrowIfNull()` for parameter validation

**Reviewer decision matrix**:

| Scenario | Preferred Pattern | Why |
|---|---|---|
| Predictable business outcome (not found, invalid state, ownership, inventory) | `Result<T>` | Explicit control flow; easier failure-path testing |
| Unexpected/infrastructure failure (DB/network/external provider) | Typed exception + middleware | Centralized HTTP mapping and observability |
| Mixed in same method for business outcomes | ❌ Avoid | Creates inconsistent API behavior |

---

### 26. ReviewRepository Missing Pagination
**Location**: `src/backend/ECommerce.Infrastructure/Repositories/ReviewRepository.cs:21-35`

```csharp
public async Task<IEnumerable<Review>> GetByProductIdAsync(..., CancellationToken)
{
    return await query
        .Include(r => r.User)
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync(cancellationToken);  // No pagination!
}
```

**Impact**: Products with thousands of reviews will load all reviews into memory.

**Fix**: Add skip/take parameters.

---

### 27. Missing Pagination Limits
**Location**: Multiple services

**Issue**: No maximum pageSize validation. Client can request `pageSize = int.MaxValue` causing memory issues.

**Recommendation**: Add max page size validation:
```csharp
private const int MaxPageSize = 100;
var effectivePageSize = Math.Min(parameters.PageSize, MaxPageSize);
```

---

### 28. UserRepository.GetByEmailAsync Not Using Index
**Location**: `src/backend/ECommerce.Infrastructure/Repositories/UserRepository.cs:22-26`

**Issue**: If Email column doesn't have a unique index, this query could be slow for large user tables.

**Recommendation**: Ensure database migration adds index on Email column.

---

### 29. CartService Has Race Condition Despite Handling
**Location**: `src/backend/ECommerce.Application/Services/CartService.cs:105-129`

**Issue**: The duplicate key exception handling is good but the re-fetch logic could fail if the cart is modified between the initial add attempt and the reload.

**Recommendation**: Use optimistic concurrency with row version, or use explicit locking.

---

### 30. No HTTP Response Caching Headers
**Location**: Controllers

**Issue**: GET endpoints don't set cache control headers. Products and categories rarely change but are fetched frequently.

**Recommendation**: Add response caching for read-only endpoints:
```csharp
[ResponseCache(Duration = 300, VaryByHeader = "Accept")]
public async Task<ActionResult<...>> GetProducts(...)
```

---

## 📝 Priority Recommendations

| Priority | Issue | Impact |
|----------|-------|--------|
| 🔴 Critical | #16-18 Wishlist N+1 | Loads entire table into memory |
| 🔴 Critical | #20-21 Reviews loading | Memory bloat with many reviews |
| 🟠 High | #22 Wrong total count | Broken pagination UI |
| 🟠 High | #27 Missing page limits | DoS vulnerability |
| 🟡 Medium | #19 Unnecessary includes | Performance |
| 🟡 Medium | #23 Cart reloading | Performance |
| 🟢 Low | #10, #24-30 | Code quality |
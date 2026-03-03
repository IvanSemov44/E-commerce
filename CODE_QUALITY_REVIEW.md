# Backend Code Quality Review
**Date**: March 2, 2026  
**Status**: ✅ **No Compilation Errors** | ⚠️ **Performance & Best Practice Issues Found**

---

## Executive Summary

The backend code is **architecturally sound** with proper separation of concerns and clean patterns. However, there are **3 critical N+1 query issues** and several best practice violations that should be addressed before scaling to production.

---

## 🔴 CRITICAL ISSUES

### 1. **N+1 Query Problem in OrderService.ProcessOrderItemsAsync()**
**Location**: [OrderService.cs](src/backend/ECommerce.Application/Services/OrderService.cs#L210-L250)  
**Severity**: 🔴 CRITICAL - Performance will degrade with large orders

**Issue**:
```csharp
foreach (var itemDto in itemDtos)
{
    // ❌ N+1: Each loop iteration executes a separate database query
    var product = await _unitOfWork.Products.GetByIdAsync(productId, trackChanges: false, cancellationToken);
    
    // ❌ Lazy loading: product.Images accessed without eager loading
    var imageUrl = product.Images.FirstOrDefault()?.Url;
}
```

**Impact**:
- For an order with 10 items: **11 queries** (1 + 10 product lookups)
- Generic `GetByIdAsync()` doesn't load `Images` collection → triggers lazy loading

**Solution**:
1. Batch-load all products at once:
```csharp
var productIds = itemDtos.Select(i => Guid.Parse(i.ProductId)).ToList();
var products = await _unitOfWork.Products.GetByIdsAsync(productIds, trackChanges: false, cancellationToken);
var productDict = products.ToDictionary(p => p.Id);

foreach (var itemDto in itemDtos)
{
    var product = productDict[productId];
    // Access product.Images (already eager loaded)
}
```

2. Add batch method to `IProductRepository`:
```csharp
Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges = false, CancellationToken ct = default);
```

---

### 2. **Lazy Loading Risk in CartRepository.CalculateTotalAsync()**
**Location**: [CartRepository.cs](src/backend/ECommerce.Infrastructure/Repositories/CartRepository.cs#L67-L72)

**Issue**:
```csharp
public async Task<decimal> CalculateTotalAsync(Guid cartId, CancellationToken cancellationToken = default)
{
    // ❌ This accesses ci.Product.Price without explicit Include
    // If Product not loaded, triggers lazy loading per CartItem
    var total = await DbSet
        .Where(c => c.Id == cartId)
        .SelectMany(c => c.Items)
        .SumAsync(ci => ci.Quantity * ci.Product.Price, cancellationToken);
    return total;
}
```

**Impact**: 
- Potential N+1 if `Product` navigation is not already loaded in the context
- CartRepository methods like `GetByUserIdAsync()` do include products, but this is fragile

**Solution**:
```csharp
.Include(ci => ci.Product)  // Add explicit Include before SumAsync
.SumAsync(ci => ci.Quantity * ci.Product.Price, cancellationToken);
```

---

### 3. **Lazy Loading Risk in Generic Repository.GetByIdAsync()**
**Location**: [Repository.cs](src/backend/ECommerce.Infrastructure/Repositories/Repository.cs#L33-L38)

**Issue**:
```csharp
public virtual async Task<T?> GetByIdAsync(Guid id, bool trackChanges = true, CancellationToken cancellationToken = default)
{
    var query = trackChanges ? DbSet : DbSet.AsNoTracking();
    // ❌ No navigation properties eagerly loaded
    return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
```

**Problem**:
- When specialized repositories (ProductRepository, ReviewRepository, etc.) override `GetByIdAsync()`, they often don't include required navigation properties
- Example: [OrderRepository](src/backend/ECommerce.Infrastructure/Repositories/OrderRepository.cs) doesn't override, so uses base class without includes

**Affected Methods**:
- `ProductRepository.GetByIdAsync()` - doesn't load `Images` or `Category`
- `OrderRepository.GetByIdAsync()` - doesn't load `Items`, `User`, addresses
- `UserRepository.GetByIdAsync()` - doesn't load `Roles` or claims

**Solution**: Create overrides in specialized repositories:
```csharp
// In ProductRepository
public override async Task<Product?> GetByIdAsync(Guid id, bool trackChanges = true, CancellationToken cancellationToken = default)
{
    var query = trackChanges ? DbSet : DbSet.AsNoTracking();
    return await query
        .Include(p => p.Images)
        .Include(p => p.Category)
        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
```

---

## ⚠️ CODE SMELLS & BEST PRACTICES

### 4. **Circular Mapping Configuration in MappingProfile**
**Location**: [MappingProfile.cs](src/backend/ECommerce.Application/MappingProfile.cs#L117-L119)

**Issue**:
```csharp
CreateMap<Review, ProductReviewDto>()
    .ForMember(dest => dest.UserName, opt => opt.MapFrom(src =>
        src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : "Anonymous"))
    .ReverseMap();  // ❌ This enables automatic reverse mapping
```

**Problem**:
- `ReverseMap()` on a Review→DTO mapping tries to auto-map ProductReviewDto → Review
- During deserialization, this could cause unexpected behavior if code accidentally uses the reverse mapping
- Best practice: Only add `.ReverseMap()` when actually needed for write operations

**Recommendation**: Remove `.ReverseMap()` from read-only DTOs:
```csharp
CreateMap<Review, ProductReviewDto>()
    .ForMember(dest => dest.UserName, opt => opt.MapFrom(src =>
        src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : "Anonymous"));
```

---

### 5. **Missing Batch GetByIds Method Across Repositories**
**Location**: Multiple repositories

**Issue**: 
- Services often need to load multiple related items but do it in loops
- Example: OrderService should load all products for an order in one query
- [WishlistService.GetUserWishlistAsync()](src/backend/ECommerce.Application/Services/WishlistService.cs#L28) correctly uses `GetAllByUserIdAsync()` (good pattern)
- But [AddToWishlistAsync()](src/backend/ECommerce.Application/Services/WishlistService.cs#L42) could use `ExistsAsync()` instead of `GetByIdAsync()` for validation

**Recommendation**: Add standard batch methods to `IRepository<T>`:
```csharp
Task<IEnumerable<T>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges = false, CancellationToken ct = default);
Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
```

---

### 6. **Premature Filtering with Reviews in ProductRepository**
**Location**: [ProductRepository.cs](src/backend/ECommerce.Infrastructure/Repositories/ProductRepository.cs#L165-L170)

**Issue**:
```csharp
if (minRating.HasValue)
{
    query = query.Where(p =>
        p.Reviews.Any(r => r.IsApproved) &&
        p.Reviews.Where(r => r.IsApproved).Average(r => r.Rating) >= (double)minRating.Value);
}
```

**Problem**:
- This is executed in LINQ to Entities, which is good (not client-side)
- However, `.Average()` is calculated for every product even when it won't be used
- **This entire section needs optimization** - consider using a separate column `AverageRating` calculated via database view or trigger

**Current Status**: Code comment says "FIX: Use subquery instead of loading all reviews" but the issue isn't fully resolved

**Better Solution**:
1. Add `AverageRating` and `ReviewCount` as computed/persisted columns
2. Update via trigger on Review insert/update/delete
3. Or use a summary table updated asynchronously

---

### 7. **Mapping Creates Nested Product Loading**
**Location**: [MappingProfile.cs](src/backend/ECommerce.Application/MappingProfile.cs#L125-L133)

**Issue**:
```csharp
CreateMap<CartItem, CartItemDto>()
    .ForMember(dest => dest.ProductImage, opt => opt.MapFrom(src =>
        src.Product != null
            ? (src.Product.Images.FirstOrDefault(x => x.IsPrimary) != null
                ? src.Product.Images.FirstOrDefault(x => x.IsPrimary)!.Url
                : src.Product.Images.FirstOrDefault() != null
                    ? src.Product.Images.FirstOrDefault()!.Url
                    : null)
            : null))
```

**Problem**:
- Complex LINQ in mapping that accesses `.FirstOrDefault()` twice (inefficient)
- Assumes `Product.Images` is loaded (works with current CartRepository but fragile)

**Solution**:
```csharp
.ForMember(dest => dest.ProductImage, opt => opt.MapFrom(src =>
    src.Product?.Images?.FirstOrDefault(x => x.IsPrimary)?.Url 
    ?? src.Product?.Images?.FirstOrDefault()?.Url));
```

---

### 8. **WishlistService ClearWishlistAsync() Could Be More Efficient**
**Location**: [WishlistService.cs](src/backend/ECommerce.Application/Services/WishlistService.cs#L84-L95)

**Issue**:
```csharp
var userWishlistEntries = await _unitOfWork.Wishlists.GetAllByUserIdAsync(userId, trackChanges: true, cancellationToken);

foreach (var entry in userWishlistEntries)
{
    await _unitOfWork.Wishlists.DeleteAsync(entry, cancellationToken);  // ❌ One delete per iteration
}
if (userWishlistEntries.Any())
{
    await _unitOfWork.SaveChangesAsync(cancellationToken);
}
```

**Problem**:
- Loads ALL wishlist entries just to delete them individually
- Better: Use database batch delete: `WHERE UserId = {userId}`

**Solution**:
```csharp
await _unitOfWork.Wishlists.DeleteByUserIdAsync(userId, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);
```

Add method to `IWishlistRepository`:
```csharp
Task DeleteByUserIdAsync(Guid userId, CancellationToken ct = default);
```

---

### 9. **AutoMapper Performance: Computed Warnings in MappingProfile**
**Location**: [MappingProfile.cs](src/backend/ECommerce.Application/MappingProfile.cs#L48-L60)

**Issue**:
```csharp
CreateMap<Product, ProductDto>()
    // Comment says: FIX: AverageRating and ReviewCount should be calculated at DB level, not during mapping
    // These require Reviews to be loaded which causes performance issues
    .ForMember(dest => dest.AverageRating, opt => opt.Ignore())
    .ForMember(dest => dest.ReviewCount, opt => opt.Ignore())
```

**Problem**:
- These fields are ignored (set to defaults) because Reviews aren't loaded
- Frontend receives `0` for `AverageRating` and `ReviewCount`
- Should be calculated at database level via computed columns

**Status**: Documented but unfixed - see issue #6 above for solution

---

## ✅ BEST PRACTICES OBSERVED (Good!)

1. **✓ Unit of Work Pattern** - Correctly implemented in [UnitOfWork.cs](src/backend/ECommerce.Infrastructure/UnitOfWork.cs)
   - Single entry point for all DB access
   - Lazy initialization with `??=` operator
   - Transaction support

2. **✓ CancellationToken Support** - Consistently applied across all async methods
   - Proper default parameter: `CancellationToken cancellationToken = default`

3. **✓ AsNoTracking() Usage** - Correctly used in read-only queries
   - [ProductRepository](src/backend/ECommerce.Infrastructure/Repositories/ProductRepository.cs#L21)
   - [OrderRepository](src/backend/ECommerce.Infrastructure/Repositories/OrderRepository.cs#L22)

4. **✓ Typed Domain Exceptions** - Used throughout (e.g., `ProductNotFoundException`, `InsufficientStockException`)
   - No generic `throw new Exception()`

5. **✓ Order Service Transaction Handling** - Proper transaction scope:
   - [OrderService.cs L75-105](src/backend/ECommerce.Application/Services/OrderService.cs#L75-L105)
   - Begin, validate, save, reduce stock, commit

6. **✓ Security: Server-side Price Lookup** - Price cannot be manipulated by client
   - [OrderService.ProcessOrderItemsAsync()](src/backend/ECommerce.Application/Services/OrderService.cs#L220)

7. **✓ Pessimistic Locking for Stock** - Uses raw SQL UPDATE:
   - [ProductRepository.TryReduceStockAsync()](src/backend/ECommerce.Infrastructure/Repositories/ProductRepository.cs#L200-L213)
   - Prevents overselling in concurrent orders

---

## 📋 SUMMARY TABLE

| Issue | Severity | File | Type | Fix Effort |
|-------|----------|------|------|-----------|
| N+1 in OrderService.ProcessOrderItemsAsync | 🔴 CRITICAL | OrderService.cs | Performance | Medium |
| N+1 in CartRepository.CalculateTotalAsync | 🔴 CRITICAL | CartRepository.cs | Performance | Low |
| Missing eager loading in Repository.GetByIdAsync | 🔴 CRITICAL | Repository.cs | Performance | Medium |
| ReverseMap on read-only DTOs | ⚠️ Medium | MappingProfile.cs | Best Practice | Low |
| Missing batch GetByIds methods | ⚠️ Medium | All Repos | Best Practice | Medium |
| Inefficient review filtering/rating | ⚠️ Medium | ProductRepository.cs | Performance | High |
| Complex mapping logic | ⚠️ Low | MappingProfile.cs | Maintainability | Low |
| WishlistService.ClearWishlistAsync | ⚠️ Low | WishlistService.cs | Performance | Low |
| Computed fields ignored in mapping | ⚠️ Medium | MappingProfile.cs | Data Quality | High |

---

## 🚀 RECOMMENDED ACTIONS (Priority Order)

1. **IMMEDIATE** (Before next deploy):
   - [ ] Fix N+1 in OrderService with batch product loading
   - [ ] Add explicit `.Include(ci => ci.Product)` in CartRepository.CalculateTotalAsync()
   - [ ] Override GetByIdAsync in specialized repositories with eager loading

2. **SOON** (Before performance testing):
   - [ ] Add `GetByIdsAsync()` and `ExistsAsync()` to base repository
   - [ ] Optimize review filtering in ProductRepository
   - [ ] Replace WishlistService.ClearWishlistAsync with batch delete

3. **LATER** (Backlog):
   - [ ] Remove unnecessary `.ReverseMap()` from read-only DTOs
   - [ ] Implement computed columns for AverageRating/ReviewCount
   - [ ] Refactor complex mapping logic into helper methods

---

## 📊 Test Coverage Notes

The codebase has good test infrastructure:
- Unit tests in [ECommerce.Tests/Unit/](src/backend/ECommerce.Tests/Unit/)
- Integration tests framework with `IntegrationTestBase`
- Validators tested thoroughly

**Recommendation**: Add performance/query count tests:
```csharp
[TestMethod]
public async Task GetProductById_Should_Execute_Exactly_OneQuery()
{
    // Use .Include() assertions to verify no lazy loading
}
```

---

**Generated**: March 2, 2026  
**Next Review**: After fixes applied

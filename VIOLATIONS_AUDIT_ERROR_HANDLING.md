# Error Handling Rule Violations - Audit Report

**Date**: March 5, 2026  
**Baseline**: Updated backend guides (BACKEND_CODING_GUIDE.md, CODE_REVIEW.md, COMPREHENSIVE_CODE_REVIEW.md)  
**Violation Rule**: Using **typed exceptions** for **predictable business failures** instead of **`Result<T>`**

---

## Current Pattern (Entire Backend)

All services use **typed exceptions** (throw) for business logic failures.
Controllers let exceptions bubble → **GlobalExceptionMiddleware** catches and maps to HTTP.

**Problem**: This violates the new rules which state:
- ✅ `Result<T>` for predictable business outcomes (validation, state, ownership, inventory)
- ✅ Typed exceptions only for unexpected/infrastructure failures (DB unavailable, network error)
- ❌ Don't mix both patterns in the same method

---

## Services Using Exceptions for Predictable Failures (Non-Compliant)

### 1. **AuthService.cs** (Multiple violations)
**File**: `src/backend/ECommerce.Application/Services/AuthService.cs`

| Method | Current Behavior | Should Use |
|--------|---|---|
| `RegisterAsync` | `throw new DuplicateEmailException(dto.Email)` | `Result<T>` — predictable (duplicate email) |
| `LoginAsync` | `throw new InvalidCredentialsException()` | `Result<T>` — predictable (invalid login) |
| `RefreshTokenAsync` | `throw new InvalidTokenException()` | `Result<T>` — predictable (expired/invalid token) |
| `VerifyEmailAsync` | `throw InvalidTokenException()` | `Result<T>` — predictable (invalid token) |
| `ResetPasswordAsync` | `throw InvalidTokenException()` | `Result<T>` — predictable (invalid token) |
| `ChangePasswordAsync` | `throw InvalidCredentialsException()` | `Result<T>` — predictable (wrong password) |

**Impact**: 6+business failures using exceptions instead of explicit `Result<T>`.

---

### 2. **CartService.cs** (Multiple violations)
**File**: `src/backend/ECommerce.Application/Services/CartService.cs`

| Method | Current Behavior | Should Use |
|--------|---|---|
| `AddToCartAsync` | `throw new InvalidQuantityException(...)` | `Result<T>` |
| `AddToCartAsync` | `throw new ProductNotFoundException(productId)` | `Result<T>` |
| `AddToCartAsync` | `throw new InsufficientStockException(...)` | `Result<T>` |
| `UpdateCartItemAsync` | `throw new InvalidQuantityException(...)` | `Result<T>` |
| `UpdateCartItemAsync` | `throw new CartItemNotFoundException(...)` | `Result<T>` |
| `UpdateCartItemAsync` | `throw new InsufficientStockException(...)` | `Result<T>` |
| `RemoveFromCartAsync` | `throw new CartItemNotFoundException(...)` | `Result<T>` |
| `ValidateCartAsync` | `throw ProductNotFoundException(...)` on invalid items | `Result<T>` |
| `ValidateCartAsync` | `throw InsufficientStockException(...)` on low stock | `Result<T>` |
| `ValidateCartAsync` | `throw ProductNotAvailableException(...)` on unavailable | `Result<T>` |

**Impact**: 10+ business failures using exceptions.

---

### 3. **CategoryService.cs** (Multiple violations)
**File**: `src/backend/ECommerce.Application/Services/CategoryService.cs`

| Method | Current Behavior | Should Use |
|--------|---|---|
| `GetCategoryByIdAsync` | `throw new CategoryNotFoundException(id)` | `Result<T>` — not found |
| `GetCategoryBySlugAsync` | `throw new CategoryNotFoundException(slug)` | `Result<T>` — not found |
| `CreateCategoryAsync` | `throw new DuplicateCategorySlugException(slug)` | `Result<T>` — duplicate |
| `UpdateCategoryAsync` | `throw new DuplicateCategorySlugException(slug)` | `Result<T>` — duplicate |
| `DeleteCategoryAsync` | `throw new CategoryHasProductsException(id)` | `Result<T>` — state conflict |
| `GetAllCategoriesAsync` | `throw new InvalidPaginationException(...)` | `Result<T>` — validation |
| `GetTopLevelCategoriesAsync` | `throw new InvalidPaginationException(...)` | `Result<T>` — validation |

**Impact**: 7 business failures using exceptions.

---

### 4. **InventoryService.cs** (Multiple violations)
**File**: `src/backend/ECommerce.Application/Services/InventoryService.cs`

| Method | Current Behavior | Should Use |
|--------|---|---|
| `ReduceStockAsync` | `throw new InvalidQuantityException(...)` | `Result<T>` |
| `ReduceStockAsync` | `throw new ProductNotFoundException(...)` | `Result<T>` |
| `ReduceStockAsync` | `throw new InsufficientStockException(...)` | `Result<T>` |
| `ReduceStockBatchAsync` | `throw new ProductNotFoundException(...)` | `Result<T>` (multiple items) |
| `ReduceStockBatchAsync` | `throw new InsufficientStockException(...)` | `Result<T>` (multiple items) |
| `AdjustStockAsync` | `throw new InvalidQuantityException(...)` | `Result<T>` |
| `AdjustStockAsync` | `throw new ProductNotFoundException(...)` | `Result<T>` |

**Impact**: 7+ business failures using exceptions.

---

### 5. **OrderService.cs** (Multiple violations)
**File**: `src/backend/ECommerce.Application/Services/OrderService.cs`

| Method | Current Behavior | Should Use |
|---|---|---|
| `CreateOrderAsync` | `throw new CartNotFoundException(...)` | `Result<T>` — not found |
| `CreateOrderAsync` | `throw new ProductNotFoundException(...)` | `Result<T>` — not found |
| `CreateOrderAsync` | `throw new InsufficientStockException(...)` | `Result<T>` — inventory |
| `GetOrderByIdAsync` | `throw new OrderNotFoundException(id)` | `Result<T>` — not found |
| (Pattern continues for status updates, cancellations) | Multiple exceptions | `Result<T>` |

**Impact**: 10+ business failures using exceptions.

---

### 6. **ProductService.cs** (Multiple violations)
**File**: `src/backend/ECommerce.Application/Services/ProductService.cs`

| Method | Current Behavior | Should Use |
|---|---|---|
| `GetProductByIdAsync` | `throw new ProductNotFoundException(id)` | `Result<T>` — not found |
| `GetProductBySlugAsync` | `throw new ProductNotFoundException(slug)` | `Result<T>` — not found |
| `CreateProductAsync` | `throw new DuplicateProductSlugException(...)` | `Result<T>` — duplicate |
| `UpdateProductAsync` | `throw new ProductNotFoundException(...)` | `Result<T>` — not found |
| `DeleteProductAsync` | `throw new ProductNotFoundException(...)` | `Result<T>` — not found |

**Impact**: 5+ business failures using exceptions.

---

### 7. **PromoCodeService.cs** (Violations)
**File**: `src/backend/ECommerce.Application/Services/PromoCodeService.cs`

| Method | Current Behavior | Should Use |
|---|---|---|
| `ValidateAndApplyAsync` | Throws on expired/invalid code | `Result<T>` |
| `GetPromoCodeAsync` | `throw PromoCodeNotFoundException()` | `Result<T>` — not found |

**Impact**: 2+ business failures.

---

### 8. **ReviewService.cs** (Violations)
**File**: `src/backend/ECommerce.Application/Services/ReviewService.cs`

| Method | Current Behavior | Should Use |
|---|---|---|
| `CreateReviewAsync` | Throws on invalid product | `Result<T>` |
| `DeleteReviewAsync` | Throws on ownership violation | `Result<T>` — ownership |

**Impact**: Ownership checks using exceptions.

---

### 9. **WishlistService.cs** (Violations)
**File**: `src/backend/ECommerce.Application/Services/WishlistService.cs`

| Method | Current Behavior | Should Use |
|---|---|---|
| `AddToWishlistAsync` | Throws on product not found | `Result<T>` |
| `RemoveFromWishlistAsync` | Throws on item not found | `Result<T>` |

**Impact**: 2+ business failures.

---

## Controllers (No Violations But Dependent on Services)

All 13 controllers rely on service exceptions bubbling up to middleware.  
If services migrate to `Result<T>`, controllers must be updated to:

```csharp
// Current
var result = await _service.CreateAsync(...);
return CreatedAtAction(...);

// After migration
var result = await _service.CreateAsync(...);
return result.Match(
    onSuccess: data => CreatedAtAction(...),
    onFailure: error => BadRequest(ApiResponse<T>.Failure(...))
);
```

**Affected Controllers**:
1. AuthController.cs
2. CartController.cs
3. CategoriesController.cs
4. OrdersController.cs
5. ProductsController.cs
6. InventoryController.cs
7. PaymentsController.cs
8. PromoCodesController.cs
9. ReviewsController.cs
10. WishlistController.cs
11. ProfileController.cs
12. DashboardController.cs
13. PaymentsController.cs

---

## Exception Files (Should Stay Exception-Based for Infrastructure)

These should **continue using typed exceptions** (infrastructure failures):

| File | Reason |
|---|---|
| `GlobalExceptionMiddleware.cs` | Maps all exceptions to HTTP responses ✅ |
| `Core/Exceptions/*.cs` | Exception definitions ✅ |
| Payment gateway integrations | External service failures ✅ |
| Database connection failures | Infrastructure (not business logic) ✅ |

---

## Summary: Files Requiring Migration

| Category | Count | Files |
|---|---|---|
| **Services using exceptions for business logic** | 9 | AuthService, CartService, CategoryService, InventoryService, OrderService, ProductService, PromoCodeService, ReviewService, WishlistService |
| **Controllers dependent on exception-based services** | 13 | All controllers |
| **DTOs/Validators** | 0 | No changes needed (still used in Result<T>) |
| **Infrastructure/Repositories** | 0 | OK (return null for not found) |

**Total Files Affected**: ~22 files

---

## Migration Tasks

### Phase 1: Define Result<T> Base Types
- [ ] Create `Core/Results/Result.cs` (or move from existing template in guide)
- [ ] Add `Result<T>` struct with `Ok()`, `Fail()`, `Match()` helpers

### Phase 2: Service Migration (Priority: High-Value Services)
Pick 1-2 high-impact services per sprint:

1. **Sprint 1**: AuthService → Result<T>
   - 6 methods with business failures
   - Impacts: Login, register, password reset

2. **Sprint 2**: CartService → Result<T>
   - 10 methods 
   - High frequency, UX critical

3. **Sprint 3**: OrderService → Result<T>
   - 10+ methods
   - Financial impact (critical path)

4. **Sprint 4**: ProductService, CategoryService → Result<T>
5. **Sprint 5**: InventoryService, PromoCodeService → Result<T>
6. **Sprint 6**: WishlistService, ReviewService, Other services → Result<T>

### Phase 3: Controller Migration (Parallel with Services)
- Update controllers to handle `Result<T>.Match()` on service calls
- Add tests for both success and failure paths

### Phase 4: Testing
- Update service unit tests to verify `Result<T>` failures instead of caught exceptions
- Add integration tests for controller `Result<T>` mapping

---

## Rationale for This Migration

### Current (Exception-Based) Issues:
- ❌ Hard to test (need exception setup)
- ❌ No explicit failure paths in signature
- ❌ Ambiguous which exceptions are expected vs. unexpected
- ❌ Forces middleware dependency for HTTP mapping

### Future (Result<T>) Benefits:
- ✅ Explicit in method signature: `public Task<Result<T>>`
- ✅ Testable without exception setup
- ✅ Clear failure handling path: `result.Match(onSuccess, onFailure)`
- ✅ Predictable business outcomes separated from infrastructure failures
- ✅ Aligns with documented standards (BACKEND_CODING_GUIDE.md)

---

## Notes for Implementation

1. **Don't remove exception types** — keep them for infrastructure failures
2. **Use Result<T> only in services** — controllers stay thin, just map `Result<T>` to HTTP
3. **Keep `GlobalExceptionMiddleware`** — catches unexpected exceptions and infrastructure failures
4. **Incremental migration** — don't refactor everything at once; migrate one feature per sprint

---

**Report Generated**: 2026-03-05  
**Next Steps**: Prioritize Phase 1 (Result<T> base type), then select first service for migration

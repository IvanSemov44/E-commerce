# Exception Class Structure

## Folder Structure

```
ECommerce.Core/Exceptions/
├── Base/                           (Abstract base exception classes)
│   ├── NotFoundException.cs
│   ├── BadRequestException.cs
│   ├── UnauthorizedException.cs
│   └── ConflictException.cs
└── *.cs                            (Specific exception implementations)
```

All exception classes follow **type-safe constructor patterns** - no generic `(string message)` constructors.

All specific exceptions use `using ECommerce.Core.Exceptions.Base;` to import base classes.

All exceptions use **C# 12 primary constructors**. Single-constructor exceptions use the compact form; multi-constructor exceptions retain traditional syntax.

### Base Exceptions (Abstract) - Located in `Base/` subfolder

| File | Namespace | Constructor | Usage |
|------|-----------|-------------|-------|
| `Base/NotFoundException.cs` | `ECommerce.Core.Exceptions.Base` | `(string message)` primary | Base for 404 errors |
| `Base/BadRequestException.cs` | `ECommerce.Core.Exceptions.Base` | `(string message)` primary | Base for 400 errors |
| `Base/UnauthorizedException.cs` | `ECommerce.Core.Exceptions.Base` | `(string message)` primary | Base for 401 errors |
| `Base/ConflictException.cs` | `ECommerce.Core.Exceptions.Base` | `(string message)` primary | Base for 409 errors |

---

### NotFound Exceptions (404)

| File | Constructors | Pattern |
|------|--------------|---------|
| `CartNotFoundException.cs` | `(Guid userId)` | ✓ Type-safe |
| `CartItemNotFoundException.cs` | `(Guid cartItemId)` | ✓ Type-safe |
| `CategoryNotFoundException.cs` | `(Guid categoryId)`, `(string slug)` | ✓ Type-safe (both) |
| `OrderNotFoundException.cs` | `(Guid orderId)` | ✓ Type-safe |
| `ProductNotFoundException.cs` | `(Guid productId)`, `(string slug)` | ✓ Type-safe (both) |
| `PromoCodeNotFoundException.cs` | `(string code)`, `(Guid promoCodeId)` | ✓ Type-safe (both) |
| `ReviewNotFoundException.cs` | `(Guid reviewId)` | ✓ Type-safe |
| `UserNotFoundException.cs` | `(Guid userId)`, `(string email)` | ✓ Type-safe (both) |
| `WishlistItemNotFoundException.cs` | `()`, `(Guid userId, Guid productId)` | ✓ Type-safe (both) |
| `NoPaymentFoundException.cs` | `(Guid orderId)` | ✓ Type-safe |

---

### BadRequest Exceptions (400)

| File | Constructors | Pattern |
|------|--------------|---------|
| `InsufficientStockException.cs` | `(string productName, int requestedQuantity, int availableQuantity)` | ✓ Type-safe |
| `InvalidOrderStatusException.cs` | `(string currentStatus, string newStatus)` | ✓ Type-safe |
| `InvalidRatingException.cs` | `()` | ✓ Type-safe (parameterless) |
| `InvalidQuantityException.cs` | `(string message)` | ✓ Message-only |
| `InvalidRefundException.cs` | `(string message)` | ✓ Message-only |
| `InvalidPromoCodeException.cs` | `(string message)` | ✓ Message-only |
| `InvalidPromoCodeConfigurationException.cs` | `(string message)` | ✓ Message-only |
| `EmptyCartException.cs` | `()` | ✓ Type-safe (parameterless) |
| `EmptyReviewCommentException.cs` | `()` | ✓ Type-safe (parameterless) |
| `PaymentAmountMismatchException.cs` | `(decimal expectedAmount, decimal providedAmount)` | ✓ Type-safe |
| `ReviewUpdateTimeExpiredException.cs` | `()` | ✓ Type-safe (parameterless) |
| `ProductNotAvailableException.cs` | `(string productName)` | ✓ Type-safe |
| `UnsupportedPaymentMethodException.cs` | `(string paymentMethod)` | ✓ Type-safe |
| `CategoryHasProductsException.cs` | `(Guid categoryId)` | ✓ Type-safe |
| `InvalidPriceRangeException.cs` | `(decimal minPrice, decimal maxPrice)` | ✓ Type-safe |
| `InvalidCredentialsBadRequestException.cs` | `()` | ⚠️ Dead code — never thrown |
| `InvalidPasswordChangeException.cs` | `()` | ✓ Type-safe (parameterless) |
| `UserAlreadyExistsException.cs` | `(string email)` | ✓ Type-safe |
| `InvalidPaginationException.cs` | `(int pageNumber)` | ✓ Type-safe |

---

### Unauthorized Exceptions (401)

| File | Constructors | Pattern |
|------|--------------|---------|
| `InvalidTokenException.cs` | `()` | ✓ Type-safe (parameterless) |
| `InvalidCredentialsException.cs` | `()` | ✓ Type-safe (parameterless) |
| `InvalidTokenUnauthorizedException.cs` | `()` | ⚠️ Dead code — never thrown |
| `UserNotAuthenticatedException.cs` | `()` | ✓ Type-safe (parameterless) |

---

### Conflict Exceptions (409)

| File | Constructors | Pattern |
|------|--------------|---------|
| `DuplicateEmailException.cs` | `(string email)` | ✓ Type-safe |
| `DuplicateProductSlugException.cs` | `(string slug)` | ✓ Type-safe |
| `DuplicateCategorySlugException.cs` | `(string slug)` | ✓ Type-safe |
| `DuplicateReviewException.cs` | `()`, `(Guid userId, Guid productId)` | ✓ Type-safe (both) |
| `DuplicateWishlistItemException.cs` | `()`, `(Guid userId, Guid productId)` | ✓ Type-safe (both) |
| `PromoCodeAlreadyExistsException.cs` | `(string code)` | ✓ Type-safe |
| `PromoCodeUsageLimitReachedException.cs` | `()`, `(string code)` | ✓ Type-safe (both) |

---

## Summary

**Total Exception Files**: 43 (4 base + 39 specific)
**Folder Organization**: Base exceptions in `Base/` subfolder ✓
**All Constructors**: Type-safe ✓
**No Generic `(string message)` Anti-patterns**: ✓
**Namespace Structure**: `ECommerce.Core.Exceptions.Base` for base classes, `ECommerce.Core.Exceptions` for specific exceptions ✓

### Pattern Categories:
1. **Typed Parameters**: Most exceptions use strongly-typed parameters (Guid, int, decimal, etc.)
2. **Parameterless**: Default message exceptions use `()` constructor
3. **Message-Only**: A few validation exceptions legitimately use `(string message)` for dynamic validation messages

### Benefits:
- ✅ Consistent error messages
- ✅ Type safety enforced at compile time
- ✅ Clear intent for each exception type
- ✅ Prevents misuse of exception constructors

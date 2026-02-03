# Exception Class Structure

## Folder: `ECommerce.Core/Exceptions`

All exception classes follow **type-safe constructor patterns** - no generic `(string message)` constructors.

### Base Exceptions (Abstract)

| File | Constructors | Usage |
|------|--------------|-------|
| `NotFoundException.cs` | Abstract base class | Base for 404 errors |
| `BadRequestException.cs` | `(string message)` protected | Base for 400 errors |
| `UnauthorizedException.cs` | Abstract base class | Base for 401 errors |
| `ConflictException.cs` | Abstract base class | Base for 409 errors |

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

#### Nested in BadRequestException.cs

| Class | Constructors | Pattern |
|-------|--------------|---------|
| `InvalidPriceRangeBadRequestException` | `(decimal minPrice, decimal maxPrice)` | ✓ Type-safe |
| `InvalidCredentialsBadRequestException` | `()` | ✓ Type-safe (parameterless) |
| `InvalidPasswordChangeBadRequestException` | `()` | ✓ Type-safe (parameterless) |
| `UserAlreadyExistsBadRequestException` | `(string email)` | ✓ Type-safe |
| `InvalidPaginationBadRequestException` | `(int pageNumber)` | ✓ Type-safe |

---

### Unauthorized Exceptions (401)

| File | Constructors | Pattern |
|------|--------------|---------|
| `InvalidTokenException.cs` | `()` | ✓ Type-safe (parameterless) |
| `InvalidCredentialsException.cs` | `()` | ✓ Type-safe (parameterless) |

#### Nested in UnauthorizedException.cs

| Class | Constructors | Pattern |
|-------|--------------|---------|
| `InvalidTokenUnauthorizedException` | `()` | ✓ Type-safe (parameterless) |
| `UserNotAuthenticatedUnauthorizedException` | `()` | ✓ Type-safe (parameterless) |

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

**Total Exception Files**: 36  
**All Constructors**: Type-safe ✓  
**No Generic `(string message)` Anti-patterns**: ✓  

### Pattern Categories:
1. **Typed Parameters**: Most exceptions use strongly-typed parameters (Guid, int, decimal, etc.)
2. **Parameterless**: Default message exceptions use `()` constructor
3. **Message-Only**: A few validation exceptions legitimately use `(string message)` for dynamic validation messages

### Benefits:
- ✅ Consistent error messages
- ✅ Type safety enforced at compile time
- ✅ Clear intent for each exception type
- ✅ Prevents misuse of exception constructors

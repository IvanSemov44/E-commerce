# Phase 4, Step 4: Shopping Cutover

**Prerequisite**: Steps 1–3 complete, `dotnet build` clean, all existing tests pass.

---

## Pre-Cutover Verification

```bash
# 1. Integration tests
cd src/backend
dotnet test

# 2. Characterization tests
dotnet test ECommerce.Tests/ECommerce.Tests.csproj \
    --filter "FullyQualifiedName~CartCharacterizationTests|FullyQualifiedName~WishlistCharacterizationTests"

# 3. E2E tests (backend must be running)
cd src/frontend/storefront
npx playwright test api-cart.spec.ts api-wishlist.spec.ts --reporter=list
```

All three must be green before proceeding.

---

## Scope decision: anonymous carts

The existing `CartService` supports anonymous carts via `sessionId`. The new `Cart` aggregate requires a `UserId`. **Phase 4 does NOT migrate anonymous cart support.** The new controller:

- For endpoints that received `userId = null` and `sessionId` previously: return a fresh empty cart DTO (no DB hit).
- `POST /cart/get-or-create`: always returns an empty cart for anonymous users.
- `POST /cart/add-item`: requires auth in the new controller. Anonymous add is deferred.

Document this in the controller with a `// TODO Phase 8` comment.

---

## Error code → HTTP status mapping

| Error code | HTTP status |
|---|---|
| `CART_NOT_FOUND` | 404 Not Found |
| `CART_ITEM_NOT_FOUND` | 404 Not Found |
| `WISHLIST_NOT_FOUND` | 404 Not Found |
| `PRODUCT_NOT_FOUND` | 404 Not Found |
| `CART_FULL` / `WISHLIST_FULL` | 422 Unprocessable |
| `QUANTITY_INVALID` | 422 Unprocessable |
| `VALIDATION_FAILED` | 400 Bad Request |
| `UNAUTHORIZED` | 401 Unauthorized |
| `FORBIDDEN` | 403 Forbidden |
| `INSUFFICIENT_STOCK` | 422 Unprocessable |

---

## DTOs (defined at top of CartController.cs or in separate file)

```csharp
namespace ECommerce.API.Controllers.DTOs;

public record AddToCartDto(Guid ProductId, int Quantity);
public record UpdateCartItemDto(int Quantity);
```

---

## Task 1: Rewrite CartController

Keep all existing route paths, HTTP methods, and route aliases. Replace `ICartService` with `IMediator`. Trust `[Authorize]` to enforce auth (no manual checks).

```csharp
using ECommerce.Shopping.Application.Commands.AddToCart;
using ECommerce.Shopping.Application.Commands.ClearCart;
using ECommerce.Shopping.Application.Commands.RemoveFromCart;
using ECommerce.Shopping.Application.Commands.UpdateCartItemQuantity;
using ECommerce.Shopping.Application.Queries.GetCart;
using ECommerce.Shopping.Application.Queries.ValidateCart;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.API.Controllers.DTOs;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController(
    IMediator _mediator,
    ICurrentUserService _currentUser,
    ILogger<CartController> _logger) : ControllerBase
{
    // ── GET /api/cart ──────────────────────────────────────────────────────
    /// <summary>Get the authenticated user's cart (load-or-create).</summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        var userId = _currentUser.UserId; // [Authorize] guarantees non-null
        _logger.LogInformation("Getting cart for user {UserId}", userId);

        try
        {
            var result = await _mediator.Send(new GetCartQuery(userId), ct);
            return result.IsSuccess
                ? Ok(ApiResponse<CartDto>.Ok(result.GetDataOrThrow(), "Cart retrieved successfully"))
                : MapResult(result.GetErrorOrThrow());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart for user {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Failure("An unexpected error occurred.", "INTERNAL_ERROR"));
        }
    }

    // ── POST /api/cart/get-or-create ───────────────────────────────────────
    /// <summary>Get or create cart. Anonymous users receive empty stub.</summary>
    [HttpPost("get-or-create")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrCreateCart(CancellationToken ct)
    {
        if (_currentUser.UserIdOrNull is Guid userId)
        {
            _logger.LogInformation("Getting cart for authenticated user {UserId}", userId);
            var result = await _mediator.Send(new GetCartQuery(userId), ct);
            return result.IsSuccess
                ? Ok(ApiResponse<CartDto>.Ok(result.GetDataOrThrow(), "Cart retrieved successfully"))
                : MapResult(result.GetErrorOrThrow());
        }

        // Anonymous user: return empty cart without DB hit
        _logger.LogDebug("Returning empty cart for anonymous user");
        var emptyCart = new CartDto(Guid.Empty, Guid.Empty, [], 0m);
        return Ok(ApiResponse<CartDto>.Ok(emptyCart, "Cart retrieved successfully"));
    }

    // ── POST /api/cart/add-item ────────────────────────────────────────────
    /// <summary>Add item to cart. BREAKING: now requires authentication (was anonymous in Phase 3).</summary>
    [HttpPost("add-item")]
    [Authorize]
    [ValidationFilter]
    public async Task<IActionResult> AddToCart(
        [FromBody] AddToCartDto dto, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("Adding item {ProductId} (qty={Quantity}) to cart for user {UserId}", 
            dto.ProductId, dto.Quantity, userId);

        try
        {
            var result = await _mediator.Send(
                new AddToCartCommand(userId, dto.ProductId, dto.Quantity), ct);
            
            if (!result.IsSuccess)
                return MapResult(result.GetErrorOrThrow());

            var cart = result.GetDataOrThrow();
            return CreatedAtAction(nameof(GetCart), new { }, 
                ApiResponse<CartDto>.Ok(cart, "Item added to cart successfully"));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict adding item to cart for user {UserId}", userId);
            return Conflict(ApiResponse<object>.Failure("Cart was modified. Please retry.", "CONCURRENCY_CONFLICT"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart for user {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Failure("An unexpected error occurred.", "INTERNAL_ERROR"));
        }
    }

    // ── PUT /api/cart/update-item/{id} AND /api/cart/items/{id} ───────────
    /// <summary>Update cart item quantity. Both route aliases supported: /update-item/{id} and /items/{id}.</summary>
    [HttpPut("update-item/{cartItemId:guid}")]
    [HttpPut("items/{cartItemId:guid}")]
    [Authorize]
    [ValidationFilter]
    public async Task<IActionResult> UpdateCartItem(
        Guid cartItemId, [FromBody] UpdateCartItemDto dto, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("Updating cart item {CartItemId} to quantity {Quantity} for user {UserId}", 
            cartItemId, dto.Quantity, userId);

        try
        {
            var result = await _mediator.Send(
                new UpdateCartItemQuantityCommand(userId, cartItemId, dto.Quantity), ct);
            
            return result.IsSuccess
                ? Ok(ApiResponse<CartDto>.Ok(result.GetDataOrThrow(), "Cart item updated successfully"))
                : MapResult(result.GetErrorOrThrow());
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating cart item {CartItemId} for user {UserId}", 
                cartItemId, userId);
            return Conflict(ApiResponse<object>.Failure("Cart was modified. Please retry.", "CONCURRENCY_CONFLICT"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item {CartItemId} for user {UserId}", cartItemId, userId);
            return StatusCode(500, ApiResponse<object>.Failure("An unexpected error occurred.", "INTERNAL_ERROR"));
        }
    }

    // ── DELETE /api/cart/remove-item/{id} AND /api/cart/items/{id} ─────────
    /// <summary>Remove item from cart. Both route aliases supported: /remove-item/{id} and /items/{id}.</summary>
    [HttpDelete("remove-item/{cartItemId:guid}")]
    [HttpDelete("items/{cartItemId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoveFromCart(Guid cartItemId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("Removing cart item {CartItemId} for user {UserId}", cartItemId, userId);

        try
        {
            var result = await _mediator.Send(new RemoveFromCartCommand(userId, cartItemId), ct);
            
            return result.IsSuccess
                ? Ok(ApiResponse<CartDto>.Ok(result.GetDataOrThrow(), "Item removed from cart successfully"))
                : MapResult(result.GetErrorOrThrow());
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict removing item {CartItemId} for user {UserId}", 
                cartItemId, userId);
            return Conflict(ApiResponse<object>.Failure("Cart was modified. Please retry.", "CONCURRENCY_CONFLICT"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item {CartItemId} for user {UserId}", cartItemId, userId);
            return StatusCode(500, ApiResponse<object>.Failure("An unexpected error occurred.", "INTERNAL_ERROR"));
        }
    }

    // ── POST /api/cart/clear AND DELETE /api/cart ──────────────────────────
    /// <summary>Clear cart (both authenticated and anonymous). Anonymous clear returns empty stub without DB hit.</summary>
    [HttpPost("clear")]
    [HttpDelete]
    [AllowAnonymous]
    public async Task<IActionResult> ClearCart(CancellationToken ct)
    {
        var userId = _currentUser.UserIdOrNull;
        _logger.LogInformation("Clearing cart for user {UserId}", userId ?? Guid.Empty);

        try
        {
            var result = await _mediator.Send(new ClearCartCommand(userId), ct);
            
            return result.IsSuccess
                ? Ok(ApiResponse<CartDto>.Ok(result.GetDataOrThrow(), "Cart cleared successfully"))
                : MapResult(result.GetErrorOrThrow());
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict clearing cart for user {UserId}", userId);
            return Conflict(ApiResponse<object>.Failure("Cart was modified. Please retry.", "CONCURRENCY_CONFLICT"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Failure("An unexpected error occurred.", "INTERNAL_ERROR"));
        }
    }

    // ── POST /api/cart/validate/{cartId} ───────────────────────────────────
    /// <summary>Validate cart for checkout. Owner or admin only. Returns 422 if any item out of stock.</summary>
    [HttpPost("validate/{cartId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateCart(Guid cartId, CancellationToken ct)
    {
        var userId = _currentUser.UserIdOrNull;
        var isAdmin = _currentUser.IsAuthenticated && HasAdminRole();
        
        _logger.LogInformation("Validating cart {CartId} (user={UserId}, isAdmin={IsAdmin})", 
            cartId, userId ?? Guid.Empty, isAdmin);

        try
        {
            var result = await _mediator.Send(new ValidateCartQuery(cartId, userId, isAdmin), ct);
            
            return result.IsSuccess
                ? Ok(ApiResponse<object>.Ok(new { }, "Cart is valid"))
                : MapResult(result.GetErrorOrThrow());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating cart {CartId}", cartId);
            return StatusCode(500, ApiResponse<object>.Failure("An unexpected error occurred.", "INTERNAL_ERROR"));
        }
    }

    private bool HasAdminRole() =>
        _currentUser.RoleOrNull is "Admin" or "SuperAdmin";

    private IActionResult MapResult(DomainError error) => error.Code switch
    {
        "CART_NOT_FOUND" or "CART_ITEM_NOT_FOUND" or "PRODUCT_NOT_FOUND"
            => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),

        "CART_FULL" or "QUANTITY_INVALID" or "INSUFFICIENT_STOCK"
            => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),

        "UNAUTHORIZED"
            => Unauthorized(ApiResponse<object>.Failure(error.Message, error.Code)),

        "FORBIDDEN"
            => StatusCode(403, ApiResponse<object>.Failure(error.Message, error.Code)),

        "VALIDATION_FAILED"
            => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),

        "CONCURRENCY_CONFLICT"
            => Conflict(ApiResponse<object>.Failure(error.Message, error.Code)),

        _ => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code))
    };
}
```

**BREAKING CHANGE**: `POST /cart/add-item` and `PUT /cart/update-item/{id}` now require authentication. Previously they were `[AllowAnonymous]`. This is an intentional Phase 4 scope reduction. Update characterization tests in step-0 if not already done.

---

## Wishlist DTOs (defined in DTOs file)

```csharp
namespace ECommerce.API.Controllers.DTOs;

public record AddToWishlistDto(Guid ProductId);
```

---

## Task 2: Rewrite WishlistController

```csharp
using ECommerce.Shopping.Application.Commands.AddToWishlist;
using ECommerce.Shopping.Application.Commands.ClearWishlist;
using ECommerce.Shopping.Application.Commands.RemoveFromWishlist;
using ECommerce.Shopping.Application.Queries.GetWishlist;
using ECommerce.Shopping.Application.Queries.IsProductInWishlist;
using ECommerce.API.Controllers.DTOs;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]  // All endpoints require authentication
public class WishlistController(
    IMediator _mediator,
    ICurrentUserService _currentUser,
    ILogger<WishlistController> _logger) : ControllerBase
{
    /// <summary>Get the authenticated user's wishlist (load-or-create).</summary>
    [HttpGet]
    public async Task<IActionResult> GetWishlist(CancellationToken ct)
    {
        var userId = _currentUser.UserId; // [Authorize] guarantees non-null
        _logger.LogInformation("Getting wishlist for user {UserId}", userId);

        try
        {
            var result = await _mediator.Send(new GetWishlistQuery(userId), ct);
            return result.IsSuccess
                ? Ok(ApiResponse<WishlistDto>.Ok(result.GetDataOrThrow(), "Wishlist retrieved successfully"))
                : MapResult(result.GetErrorOrThrow());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wishlist for user {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Failure("An unexpected error occurred.", "INTERNAL_ERROR"));
        }
    }

    /// <summary>Add product to wishlist (idempotent).</summary>
    [HttpPost("add")]
    [ValidationFilter]
    public async Task<IActionResult> AddToWishlist(
        [FromBody] AddToWishlistDto dto, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("Adding product {ProductId} to wishlist for user {UserId}", 
            dto.ProductId, userId);

        try
        {
            var result = await _mediator.Send(new AddToWishlistCommand(userId, dto.ProductId), ct);
            
            if (!result.IsSuccess)
                return MapResult(result.GetErrorOrThrow());

            var wishlist = result.GetDataOrThrow();
            return CreatedAtAction(nameof(GetWishlist), new { },
                ApiResponse<WishlistDto>.Ok(wishlist, "Product added to wishlist successfully"));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict adding to wishlist for user {UserId}", userId);
            return Conflict(ApiResponse<object>.Failure("Wishlist was modified. Please retry.", "CONCURRENCY_CONFLICT"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product {ProductId} to wishlist for user {UserId}", 
                dto.ProductId, userId);
            return StatusCode(500, ApiResponse<object>.Failure("An unexpected error occurred.", "INTERNAL_ERROR"));
        }
    }

    /// <summary>Remove product from wishlist (no-op if not present).</summary>
    [HttpDelete("remove/{productId:guid}")]
    public async Task<IActionResult> RemoveFromWishlist(Guid productId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("Removing product {ProductId} from wishlist for user {UserId}", 
            productId, userId);

        try
        {
            var result = await _mediator.Send(new RemoveFromWishlistCommand(userId, productId), ct);
            
            return result.IsSuccess
                ? Ok(ApiResponse<WishlistDto>.Ok(result.GetDataOrThrow(), "Product removed from wishlist successfully"))
                : MapResult(result.GetErrorOrThrow());
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict removing from wishlist for user {UserId}", userId);
            return Conflict(ApiResponse<object>.Failure("Wishlist was modified. Please retry.", "CONCURRENCY_CONFLICT"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing product {ProductId} from wishlist for user {UserId}", 
                productId, userId);
            return StatusCode(500, ApiResponse<object>.Failure("An unexpected error occurred.", "INTERNAL_ERROR"));
        }
    }

    /// <summary>Check if product is in wishlist. Returns plain bool in data field.</summary>
    [HttpGet("contains/{productId:guid}")]
    public async Task<IActionResult> IsProductInWishlist(Guid productId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        _logger.LogDebug("Checking if product {ProductId} is in wishlist for user {UserId}", 
            productId, userId);

        try
        {
            var result = await _mediator.Send(new IsProductInWishlistQuery(userId, productId), ct);
            
            // IMPORTANT: data field must be a plain bool, not an object
            // Characterization tests verify: typeof body.data === 'boolean'
            return Ok(ApiResponse<bool>.Ok(result.GetDataOrThrow(), "Check completed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking wishlist for product {ProductId}, user {UserId}", 
                productId, userId);
            return StatusCode(500, ApiResponse<object>.Failure("An unexpected error occurred.", "INTERNAL_ERROR"));
        }
    }

    /// <summary>Clear entire wishlist.</summary>
    [HttpPost("clear")]
    public async Task<IActionResult> ClearWishlist(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("Clearing wishlist for user {UserId}", userId);

        try
        {
            var result = await _mediator.Send(new ClearWishlistCommand(userId), ct);
            
            return result.IsSuccess
                ? Ok(ApiResponse<WishlistDto>.Ok(result.GetDataOrThrow(), "Wishlist cleared successfully"))
                : MapResult(result.GetErrorOrThrow());
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict clearing wishlist for user {UserId}", userId);
            return Conflict(ApiResponse<object>.Failure("Wishlist was modified. Please retry.", "CONCURRENCY_CONFLICT"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing wishlist for user {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Failure("An unexpected error occurred.", "INTERNAL_ERROR"));
        }
    }

    private IActionResult MapResult(DomainError error) => error.Code switch
    {
        "PRODUCT_NOT_FOUND" or "WISHLIST_NOT_FOUND"
            => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),

        "WISHLIST_FULL"
            => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),

        "VALIDATION_FAILED"
            => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),

        "CONCURRENCY_CONFLICT"
            => Conflict(ApiResponse<object>.Failure(error.Message, error.Code)),

        _ => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code))
    };
}
```

---

## Task 3: Delete old services

Once both controllers are updated and all tests pass:

```bash
rm src/backend/ECommerce.Application/Services/CartService.cs
rm src/backend/ECommerce.Application/Services/WishlistService.cs
rm src/backend/ECommerce.Application/Interfaces/ICartService.cs
rm src/backend/ECommerce.Application/Interfaces/IWishlistService.cs
```

Remove DI registrations from `Program.cs`:
```csharp
// REMOVE:
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
```

---

## Post-Cutover Verification

```bash
cd src/backend
dotnet test

dotnet test ECommerce.Tests/ECommerce.Tests.csproj \
    --filter "FullyQualifiedName~CartCharacterizationTests|FullyQualifiedName~WishlistCharacterizationTests"

cd src/frontend/storefront
npx playwright test api-cart.spec.ts api-wishlist.spec.ts --reporter=list
```

---

## Acceptance Criteria

**Controllers**:
- [ ] `CartController` and `WishlistController` both inject `IMediator`, `ICurrentUserService`, and `ILogger<T>`
- [ ] All actions use `await _mediator.Send()` — no direct service calls
- [ ] `ICurrentUserService.UserId` used on `[Authorize]` endpoints (no null checks needed)
- [ ] `ICurrentUserService.UserIdOrNull` used on `[AllowAnonymous]` endpoints (with null checks)
- [ ] All error paths use private `MapResult()` method for consistent error → HTTP code mapping
- [ ] `DbUpdateConcurrencyException` caught and returns 409 Conflict
- [ ] All unhandled exceptions logged and return 500 with `INTERNAL_ERROR` code

**Routes & Aliases**:
- [ ] Cart: Both `/api/cart/update-item/{id}` and `/api/cart/items/{id}` route to same action
- [ ] Cart: Both `/api/cart/remove-item/{id}` and `/api/cart/items/{id}` route to same action
- [ ] Wishlist: All endpoints have `[Authorize]` at class level (no anonymous access)

**Auth Breaking Changes**:
- [ ] `POST /cart/add-item` requires `[Authorize]` (was anonymous in Phase 3) — documented
- [ ] `PUT /cart/update-item/{id}` requires `[Authorize]` (was anonymous in Phase 3) — documented
- [ ] `POST /cart/get-or-create` remains `[AllowAnonymous]` — returns empty stub for anon users
- [ ] `POST /cart/clear` remains `[AllowAnonymous]` — handles null userId without DB hit
- [ ] `POST /cart/validate/{id}` remains `[AllowAnonymous]` — checks ownership/admin role in handler

**Response Shapes**:
- [ ] `GET /wishlist/contains/{id}` returns `ApiResponse<bool>` with plain bool in `data` field (not object)
- [ ] All POST create endpoints return `CreatedAtAction()` with Location header (optional: may use plain `Ok()`)
- [ ] All success responses include message: "...successfully", "Cart retrieved", etc.
- [ ] All error responses use `MapResult()` with correct HTTP status codes

**Error Mappings**:
- [ ] `CART_NOT_FOUND`, `CART_ITEM_NOT_FOUND`, `PRODUCT_NOT_FOUND` → 404 NotFound
- [ ] `CART_FULL`, `QUANTITY_INVALID`, `INSUFFICIENT_STOCK`, `WISHLIST_FULL` → 422 UnprocessableEntity
- [ ] `VALIDATION_FAILED` → 400 BadRequest
- [ ] `UNAUTHORIZED` → 401 Unauthorized
- [ ] `FORBIDDEN` → 403 Forbidden
- [ ] `CONCURRENCY_CONFLICT` → 409 Conflict
- [ ] Unhandled exceptions → 500 Internal Server Error with `INTERNAL_ERROR` code

**DTOs**:
- [ ] `AddToCartDto(ProductId, Quantity)` defined
- [ ] `UpdateCartItemDto(Quantity)` defined
- [ ] `AddToWishlistDto(ProductId)` defined
- [ ] All DTOs use validation attributes or `[ValidationFilter]` on controllers

**Logging**:
- [ ] `ILogger<CartController>` and `ILogger<WishlistController>` injected
- [ ] Key operations logged: GetCart, AddToCart, RemoveFromCart, ClearCart, etc.
- [ ] Errors and concurrency conflicts logged with context (userId, productId, etc.)

**Service Cleanup**:
- [ ] Old `CartService`, `WishlistService`, `ICartService`, `IWishlistService` deleted
- [ ] DI registration for old services removed from `Program.cs`
- [ ] `dotnet build` passes with no references to old service types

**Testing**:
- [ ] All characterization tests from step-0 pass post-cutover
- [ ] Characterization tests verify auth requirements (401 on protected endpoints)
- [ ] Characterization tests verify route aliases both work
- [ ] All e2e tests from step-0b pass post-cutover
- [ ] `dotnet test` passes for entire solution

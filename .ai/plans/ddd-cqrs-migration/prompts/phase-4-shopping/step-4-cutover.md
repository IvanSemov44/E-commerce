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

## Task 1: Rewrite CartController

Keep all existing route paths, HTTP methods, and route aliases. Replace `ICartService` with `IMediator`.

```csharp
using ECommerce.Shopping.Application.Commands.AddToCart;
using ECommerce.Shopping.Application.Commands.ClearCart;
using ECommerce.Shopping.Application.Commands.RemoveFromCart;
using ECommerce.Shopping.Application.Commands.UpdateCartItemQuantity;
using ECommerce.Shopping.Application.Queries.GetCart;
using ECommerce.Shopping.Application.Queries.ValidateCart;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController(IMediator _mediator, ICurrentUserService _currentUser) : ControllerBase
{
    // ── GET /api/cart ──────────────────────────────────────────────────────
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        if (_currentUser.UserIdOrNull is not Guid userId)
            return Unauthorized(ApiResponse<object>.Failure("Authentication required.", "UNAUTHORIZED"));

        var result = await _mediator.Send(new GetCartQuery(userId), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Cart retrieved successfully"))
            : MapResult(result.GetErrorOrThrow());
    }

    // ── POST /api/cart/get-or-create ───────────────────────────────────────
    [HttpPost("get-or-create")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrCreateCart(CancellationToken ct)
    {
        if (_currentUser.UserIdOrNull is Guid userId)
        {
            var result = await _mediator.Send(new GetCartQuery(userId), ct);
            return result.IsSuccess
                ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Cart retrieved or created successfully"))
                : MapResult(result.GetErrorOrThrow());
        }

        // Anonymous: return empty cart stub
        // TODO Phase 8: implement session-based anonymous cart
        return Ok(ApiResponse<object>.Ok(
            new { Id = Guid.Empty, UserId = (Guid?)null, Items = Array.Empty<object>(), Subtotal = 0m },
            "Cart retrieved or created successfully"));
    }

    // ── POST /api/cart/add-item ────────────────────────────────────────────
    [HttpPost("add-item")]
    [Authorize]           // Phase 4: anonymous add deferred (was AllowAnonymous)
    [ValidationFilter]
    public async Task<IActionResult> AddToCart(
        [FromBody] AddToCartDto dto, CancellationToken ct)
    {
        if (_currentUser.UserIdOrNull is not Guid userId)
            return Unauthorized(ApiResponse<object>.Failure("Authentication required.", "UNAUTHORIZED"));

        var result = await _mediator.Send(new AddToCartCommand(userId, dto.ProductId, dto.Quantity), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Item added to cart successfully"))
            : MapResult(result.GetErrorOrThrow());
    }

    // ── PUT /api/cart/update-item/{id} AND /api/cart/items/{id} ───────────
    [HttpPut("update-item/{cartItemId:guid}")]
    [HttpPut("items/{cartItemId:guid}")]
    [Authorize]           // Phase 4: requires auth (was AllowAnonymous)
    [ValidationFilter]
    public async Task<IActionResult> UpdateCartItem(
        Guid cartItemId, [FromBody] UpdateCartItemDto dto, CancellationToken ct)
    {
        if (_currentUser.UserIdOrNull is not Guid userId)
            return Unauthorized(ApiResponse<object>.Failure("Authentication required.", "UNAUTHORIZED"));

        var result = await _mediator.Send(
            new UpdateCartItemQuantityCommand(userId, cartItemId, dto.Quantity), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Cart item updated successfully"))
            : MapResult(result.GetErrorOrThrow());
    }

    // ── DELETE /api/cart/remove-item/{id} AND /api/cart/items/{id} ─────────
    [HttpDelete("remove-item/{cartItemId:guid}")]
    [HttpDelete("items/{cartItemId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoveFromCart(Guid cartItemId, CancellationToken ct)
    {
        if (_currentUser.UserIdOrNull is not Guid userId)
            return Unauthorized(ApiResponse<object>.Failure("Authentication required.", "UNAUTHORIZED"));

        var result = await _mediator.Send(new RemoveFromCartCommand(userId, cartItemId), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Item removed from cart successfully"))
            : MapResult(result.GetErrorOrThrow());
    }

    // ── POST /api/cart/clear AND DELETE /api/cart ──────────────────────────
    [HttpPost("clear")]
    [HttpDelete]
    [AllowAnonymous]
    public async Task<IActionResult> ClearCart(CancellationToken ct)
    {
        var userId = _currentUser.UserIdOrNull;
        var result = await _mediator.Send(new ClearCartCommand(userId), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Cart cleared successfully"))
            : MapResult(result.GetErrorOrThrow());
    }

    // ── POST /api/cart/validate/{cartId} ───────────────────────────────────
    [HttpPost("validate/{cartId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateCart(Guid cartId, CancellationToken ct)
    {
        var userId  = _currentUser.UserIdOrNull;
        var isAdmin = _currentUser.IsAuthenticated &&
                      (_currentUser.RoleOrNull?.ToString() is "Admin" or "SuperAdmin");

        var result = await _mediator.Send(new ValidateCartQuery(cartId, userId, isAdmin), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(new { }, "Cart is valid"))
            : MapResult(result.GetErrorOrThrow());
    }

    private IActionResult MapResult(DomainError error) => error.Code switch
    {
        "CART_NOT_FOUND" or "CART_ITEM_NOT_FOUND" or "PRODUCT_NOT_FOUND"
            => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),

        "CART_FULL" or "WISHLIST_FULL" or "QUANTITY_INVALID" or "INSUFFICIENT_STOCK"
            => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),

        "UNAUTHORIZED"
            => Unauthorized(ApiResponse<object>.Failure(error.Message, error.Code)),

        "FORBIDDEN"
            => StatusCode(403, ApiResponse<object>.Failure(error.Message, error.Code)),

        "VALIDATION_FAILED"
            => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),

        _ => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code))
    };
}
```

**Breaking change note**: `POST /cart/add-item` and `PUT /cart/update-item/{id}` now require authentication. Previously they were `[AllowAnonymous]`. This is an intentional scope reduction for Phase 4 — document with a comment and verify the characterization tests were updated to reflect this.

---

## Task 2: Rewrite WishlistController

```csharp
using ECommerce.Shopping.Application.Commands.AddToWishlist;
using ECommerce.Shopping.Application.Commands.ClearWishlist;
using ECommerce.Shopping.Application.Commands.RemoveFromWishlist;
using ECommerce.Shopping.Application.Queries.GetWishlist;
using ECommerce.Shopping.Application.Queries.IsProductInWishlist;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController(IMediator _mediator, ICurrentUserService _currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetWishlist(CancellationToken ct)
    {
        if (_currentUser.UserIdOrNull is not Guid userId)
            return Unauthorized(ApiResponse<object>.Failure("Authentication required.", "UNAUTHORIZED"));

        var result = await _mediator.Send(new GetWishlistQuery(userId), ct);
        return Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Wishlist retrieved successfully"));
    }

    [HttpPost("add")]
    [ValidationFilter]
    public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistDto dto, CancellationToken ct)
    {
        if (_currentUser.UserIdOrNull is not Guid userId)
            return Unauthorized(ApiResponse<object>.Failure("Authentication required.", "UNAUTHORIZED"));

        var result = await _mediator.Send(new AddToWishlistCommand(userId, dto.ProductId), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Product added to wishlist successfully"))
            : MapResult(result.GetErrorOrThrow());
    }

    [HttpDelete("remove/{productId:guid}")]
    public async Task<IActionResult> RemoveFromWishlist(Guid productId, CancellationToken ct)
    {
        if (_currentUser.UserIdOrNull is not Guid userId)
            return Unauthorized(ApiResponse<object>.Failure("Authentication required.", "UNAUTHORIZED"));

        var result = await _mediator.Send(new RemoveFromWishlistCommand(userId, productId), ct);
        return Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Product removed from wishlist successfully"));
    }

    [HttpGet("contains/{productId:guid}")]
    public async Task<IActionResult> IsProductInWishlist(Guid productId, CancellationToken ct)
    {
        if (_currentUser.UserIdOrNull is not Guid userId)
            return Unauthorized(ApiResponse<object>.Failure("Authentication required.", "UNAUTHORIZED"));

        var result = await _mediator.Send(new IsProductInWishlistQuery(userId, productId), ct);
        // data must be a plain bool — the characterization test pins this shape
        return Ok(ApiResponse<bool>.Ok(result.GetDataOrThrow(), "Check completed successfully"));
    }

    [HttpPost("clear")]
    public async Task<IActionResult> ClearWishlist(CancellationToken ct)
    {
        if (_currentUser.UserIdOrNull is not Guid userId)
            return Unauthorized(ApiResponse<object>.Failure("Authentication required.", "UNAUTHORIZED"));

        var result = await _mediator.Send(new ClearWishlistCommand(userId), ct);
        return Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Wishlist cleared successfully"));
    }

    private IActionResult MapResult(DomainError error) => error.Code switch
    {
        "PRODUCT_NOT_FOUND" or "WISHLIST_NOT_FOUND"
            => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),

        "WISHLIST_FULL"
            => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),

        "VALIDATION_FAILED"
            => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),

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

- [ ] `CartController` updated to use `IMediator`; all route paths and aliases preserved
- [ ] `WishlistController` updated to use `IMediator`
- [ ] `POST /cart/add-item` now requires auth — documented with `// TODO Phase 8` comment
- [ ] `GET /wishlist/contains/{id}` returns `ApiResponse<bool>` (bool in `data`, not object)
- [ ] `QUANTITY_INVALID` and `CART_FULL` → 422; `CART_ITEM_NOT_FOUND` → 404
- [ ] Old `CartService`, `WishlistService`, `ICartService`, `IWishlistService` deleted
- [ ] All characterization tests pass post-cutover
- [ ] All e2e tests pass post-cutover

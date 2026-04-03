# Phase 4, Step 2: Shopping Application Project

**Prerequisite**: Step 1 (`ECommerce.Shopping.Domain`) is complete and `dotnet build` passes.

---

## Task: Create ECommerce.Shopping.Application Project

### 1. Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Shopping.Application -f net10.0 -o Shopping/ECommerce.Shopping.Application
dotnet sln ../../ECommerce.sln add Shopping/ECommerce.Shopping.Application/ECommerce.Shopping.Application.csproj

dotnet add Shopping/ECommerce.Shopping.Application/ECommerce.Shopping.Application.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add Shopping/ECommerce.Shopping.Application/ECommerce.Shopping.Application.csproj \
    reference Shopping/ECommerce.Shopping.Domain/ECommerce.Shopping.Domain.csproj

dotnet add Shopping/ECommerce.Shopping.Application/ECommerce.Shopping.Application.csproj package MediatR
dotnet add Shopping/ECommerce.Shopping.Application/ECommerce.Shopping.Application.csproj package FluentValidation
dotnet add Shopping/ECommerce.Shopping.Application/ECommerce.Shopping.Application.csproj \
    package Microsoft.EntityFrameworkCore  # for cross-context product query via AppDbContext

rm Shopping/ECommerce.Shopping.Application/Class1.cs
```

### 2. Create application errors

**File: `Shopping/ECommerce.Shopping.Application/Errors/ShoppingApplicationErrors.cs`**

```csharp
using ECommerce.SharedKernel.Results;

namespace ECommerce.Shopping.Application.Errors;

public static class ShoppingApplicationErrors
{
    public static readonly DomainError CartNotFound     = new("CART_NOT_FOUND",     "Cart not found.");
    public static readonly DomainError WishlistNotFound = new("WISHLIST_NOT_FOUND", "Wishlist not found.");
    public static readonly DomainError ProductNotFound  = new("PRODUCT_NOT_FOUND",  "Product not found or inactive.");
    public static readonly DomainError Unauthorized     = new("UNAUTHORIZED",       "Authentication required.");
    public static readonly DomainError Forbidden        = new("FORBIDDEN",          "You do not have permission to access this cart.");
}
```

### 3. Create DTOs

**File: `Shopping/ECommerce.Shopping.Application/DTOs/CartItemDto.cs`**
```csharp
namespace ECommerce.Shopping.Application.DTOs;

public record CartItemDto(
    Guid    Id,
    Guid    ProductId,
    int     Quantity,
    decimal UnitPrice,
    string  Currency,
    decimal LineTotal
);
```

**File: `Shopping/ECommerce.Shopping.Application/DTOs/CartDto.cs`**
```csharp
namespace ECommerce.Shopping.Application.DTOs;

public record CartDto(
    Guid            Id,
    Guid            UserId,
    List<CartItemDto> Items,
    decimal         Subtotal
);
```

**File: `Shopping/ECommerce.Shopping.Application/DTOs/WishlistDto.cs`**
```csharp
namespace ECommerce.Shopping.Application.DTOs;

public record WishlistDto(
    Guid        Id,
    Guid        UserId,
    List<Guid>  ProductIds
);
```

### 4. Create a mapper helper

Mapping from aggregate to DTO is needed in every handler. Put it in one place.

**File: `Shopping/ECommerce.Shopping.Application/Mapping/CartMappingExtensions.cs`**

```csharp
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Aggregates.Wishlist;

namespace ECommerce.Shopping.Application.Mapping;

public static class ShoppingMappingExtensions
{
    public static CartDto ToDto(this Cart cart) => new(
        cart.Id,
        cart.UserId,
        cart.Items.Select(i => new CartItemDto(
            i.Id, i.ProductId, i.Quantity, i.UnitPrice, i.Currency,
            i.UnitPrice * i.Quantity)).ToList(),
        cart.Subtotal);

    public static WishlistDto ToDto(this Wishlist wishlist) => new(
        wishlist.Id,
        wishlist.UserId,
        wishlist.ProductIds.ToList());
}
```

---

### 5. Cart queries

**`Queries/GetCart/GetCartQuery.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Queries.GetCart;

public record GetCartQuery(Guid UserId) : IRequest<Result<CartDto>>;
```

**`Queries/GetCart/GetCartQueryHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Errors;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Queries.GetCart;

public class GetCartQueryHandler(ICartRepository _carts)
    : IRequestHandler<GetCartQuery, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(GetCartQuery query, CancellationToken ct)
    {
        // Load-or-create: callers always get a cart back
        var cart = await _carts.GetByUserIdAsync(query.UserId, ct)
                   ?? Cart.Create(query.UserId);

        return Result<CartDto>.Ok(cart.ToDto());
    }
}
```

**`Queries/ValidateCart/ValidateCartQuery.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Shopping.Application.Queries.ValidateCart;

public record ValidateCartQuery(
    Guid  CartId,
    Guid? RequestingUserId,
    bool  IsAdmin
) : IRequest<Result>;
```

**`Queries/ValidateCart/ValidateCartQueryHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Application.Errors;
using ECommerce.Shopping.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Application.Queries.ValidateCart;

public class ValidateCartQueryHandler(
    ICartRepository _carts,
    IShoppingDbReader _db   // see step-3 for this interface
) : IRequestHandler<ValidateCartQuery, Result>
{
    public async Task<Result> Handle(ValidateCartQuery query, CancellationToken ct)
    {
        var cart = await _carts.GetByIdAsync(query.CartId, ct);
        if (cart is null) return Result.Fail(ShoppingApplicationErrors.CartNotFound);

        // Only the cart owner or an admin may validate
        if (!query.IsAdmin && query.RequestingUserId != cart.UserId)
            return Result.Fail(ShoppingApplicationErrors.Forbidden);

        // Check each product still exists and has sufficient inventory
        foreach (var item in cart.Items)
        {
            var inStock = await _db.IsInStockAsync(item.ProductId, item.Quantity, ct);
            if (!inStock)
                return Result.Fail(new DomainError("INSUFFICIENT_STOCK",
                    $"Product {item.ProductId} has insufficient stock."));
        }

        return Result.Ok();
    }
}
```

---

### 6. Cart commands

**`Commands/AddToCart/AddToCartCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Commands.AddToCart;

public record AddToCartCommand(
    Guid UserId,
    Guid ProductId,
    int  Quantity
) : IRequest<Result<CartDto>>, ITransactionalCommand;
```

**`Commands/AddToCart/AddToCartCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Shopping.Application.Commands.AddToCart;

public class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
    }
}
```

**`Commands/AddToCart/AddToCartCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Errors;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Commands.AddToCart;

public class AddToCartCommandHandler(
    ICartRepository _carts,
    IShoppingDbReader _db,  // cross-context product lookup (see step-3)
    IUnitOfWork _uow
) : IRequestHandler<AddToCartCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(AddToCartCommand command, CancellationToken ct)
    {
        // Cross-context validation: product must exist and be active
        // TODO Phase 8: replace with HTTP call to Catalog service when moving to microservices
        var product = await _db.GetProductPriceAsync(command.ProductId, ct);
        if (product is null)
            return Result<CartDto>.Fail(ShoppingApplicationErrors.ProductNotFound);

        var cart = await _carts.GetByUserIdAsync(command.UserId, ct)
                   ?? Cart.Create(command.UserId);

        var result = cart.AddItem(command.ProductId, command.Quantity, product.Price, product.Currency);
        if (!result.IsSuccess) return Result<CartDto>.Fail(result.GetErrorOrThrow());

        await _carts.UpsertAsync(cart, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<CartDto>.Ok(cart.ToDto());
    }
}
```

---

**`Commands/RemoveFromCart/RemoveFromCartCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Commands.RemoveFromCart;

public record RemoveFromCartCommand(
    Guid UserId,
    Guid CartItemId
) : IRequest<Result<CartDto>>, ITransactionalCommand;
```

**`Commands/RemoveFromCart/RemoveFromCartCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Errors;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Commands.RemoveFromCart;

public class RemoveFromCartCommandHandler(
    ICartRepository _carts,
    IUnitOfWork _uow
) : IRequestHandler<RemoveFromCartCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(RemoveFromCartCommand command, CancellationToken ct)
    {
        var cart = await _carts.GetByUserIdAsync(command.UserId, ct);
        if (cart is null) return Result<CartDto>.Fail(ShoppingApplicationErrors.CartNotFound);

        var result = cart.RemoveItem(command.CartItemId);
        if (!result.IsSuccess) return Result<CartDto>.Fail(result.GetErrorOrThrow());

        await _carts.UpsertAsync(cart, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<CartDto>.Ok(cart.ToDto());
    }
}
```

---

**`Commands/UpdateCartItemQuantity/UpdateCartItemQuantityCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Commands.UpdateCartItemQuantity;

public record UpdateCartItemQuantityCommand(
    Guid UserId,
    Guid CartItemId,
    int  NewQuantity
) : IRequest<Result<CartDto>>, ITransactionalCommand;
```

**`Commands/UpdateCartItemQuantity/UpdateCartItemQuantityCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Shopping.Application.Commands.UpdateCartItemQuantity;

public class UpdateCartItemQuantityCommandValidator : AbstractValidator<UpdateCartItemQuantityCommand>
{
    public UpdateCartItemQuantityCommandValidator()
    {
        RuleFor(x => x.NewQuantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
    }
}
```

**`Commands/UpdateCartItemQuantity/UpdateCartItemQuantityCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Errors;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Commands.UpdateCartItemQuantity;

public class UpdateCartItemQuantityCommandHandler(
    ICartRepository _carts,
    IUnitOfWork _uow
) : IRequestHandler<UpdateCartItemQuantityCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(UpdateCartItemQuantityCommand command, CancellationToken ct)
    {
        var cart = await _carts.GetByUserIdAsync(command.UserId, ct);
        if (cart is null) return Result<CartDto>.Fail(ShoppingApplicationErrors.CartNotFound);

        var result = cart.UpdateItemQuantity(command.CartItemId, command.NewQuantity);
        if (!result.IsSuccess) return Result<CartDto>.Fail(result.GetErrorOrThrow());

        await _carts.UpsertAsync(cart, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<CartDto>.Ok(cart.ToDto());
    }
}
```

---

**`Commands/ClearCart/ClearCartCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Commands.ClearCart;

public record ClearCartCommand(Guid? UserId)
    : IRequest<Result<CartDto>>, ITransactionalCommand;
```

**`Commands/ClearCart/ClearCartCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Commands.ClearCart;

public class ClearCartCommandHandler(
    ICartRepository _carts,
    IUnitOfWork _uow
) : IRequestHandler<ClearCartCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(ClearCartCommand command, CancellationToken ct)
    {
        if (command.UserId is null)
        {
            // Anonymous clear: return an empty cart DTO without touching the DB
            var empty = Cart.Create(Guid.Empty);
            return Result<CartDto>.Ok(empty.ToDto());
        }

        var cart = await _carts.GetByUserIdAsync(command.UserId.Value, ct);
        if (cart is null)
        {
            // No cart to clear — return empty
            return Result<CartDto>.Ok(Cart.Create(command.UserId.Value).ToDto());
        }

        cart.Clear();
        await _carts.UpsertAsync(cart, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<CartDto>.Ok(cart.ToDto());
    }
}
```

---

### 7. Wishlist queries and commands

**`Queries/GetWishlist/GetWishlistQuery.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Queries.GetWishlist;

public record GetWishlistQuery(Guid UserId) : IRequest<Result<WishlistDto>>;
```

**`Queries/GetWishlist/GetWishlistQueryHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Queries.GetWishlist;

public class GetWishlistQueryHandler(IWishlistRepository _wishlists)
    : IRequestHandler<GetWishlistQuery, Result<WishlistDto>>
{
    public async Task<Result<WishlistDto>> Handle(GetWishlistQuery query, CancellationToken ct)
    {
        var wishlist = await _wishlists.GetByUserIdAsync(query.UserId, ct)
                       ?? Wishlist.Create(query.UserId);
        return Result<WishlistDto>.Ok(wishlist.ToDto());
    }
}
```

**`Queries/IsProductInWishlist/IsProductInWishlistQuery.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Shopping.Application.Queries.IsProductInWishlist;

public record IsProductInWishlistQuery(Guid UserId, Guid ProductId) : IRequest<Result<bool>>;
```

**`Queries/IsProductInWishlist/IsProductInWishlistQueryHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Queries.IsProductInWishlist;

public class IsProductInWishlistQueryHandler(IWishlistRepository _wishlists)
    : IRequestHandler<IsProductInWishlistQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(IsProductInWishlistQuery query, CancellationToken ct)
    {
        var wishlist = await _wishlists.GetByUserIdAsync(query.UserId, ct);
        var contains = wishlist?.Contains(query.ProductId) ?? false;
        return Result<bool>.Ok(contains);
    }
}
```

**`Commands/AddToWishlist/AddToWishlistCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Commands.AddToWishlist;

public record AddToWishlistCommand(Guid UserId, Guid ProductId)
    : IRequest<Result<WishlistDto>>, ITransactionalCommand;
```

**`Commands/AddToWishlist/AddToWishlistCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Errors;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Commands.AddToWishlist;

public class AddToWishlistCommandHandler(
    IWishlistRepository _wishlists,
    IShoppingDbReader _db,   // cross-context product check
    IUnitOfWork _uow
) : IRequestHandler<AddToWishlistCommand, Result<WishlistDto>>
{
    public async Task<Result<WishlistDto>> Handle(AddToWishlistCommand command, CancellationToken ct)
    {
        // Cross-context: product must exist
        var productExists = await _db.ProductExistsAsync(command.ProductId, ct);
        if (!productExists)
            return Result<WishlistDto>.Fail(ShoppingApplicationErrors.ProductNotFound);

        var wishlist = await _wishlists.GetByUserIdAsync(command.UserId, ct)
                       ?? Wishlist.Create(command.UserId);

        var result = wishlist.AddProduct(command.ProductId);
        if (!result.IsSuccess) return Result<WishlistDto>.Fail(result.GetErrorOrThrow());

        await _wishlists.UpsertAsync(wishlist, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<WishlistDto>.Ok(wishlist.ToDto());
    }
}
```

**`Commands/RemoveFromWishlist/RemoveFromWishlistCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Commands.RemoveFromWishlist;

public record RemoveFromWishlistCommand(Guid UserId, Guid ProductId)
    : IRequest<Result<WishlistDto>>, ITransactionalCommand;
```

**`Commands/RemoveFromWishlist/RemoveFromWishlistCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Commands.RemoveFromWishlist;

public class RemoveFromWishlistCommandHandler(
    IWishlistRepository _wishlists,
    IUnitOfWork _uow
) : IRequestHandler<RemoveFromWishlistCommand, Result<WishlistDto>>
{
    public async Task<Result<WishlistDto>> Handle(RemoveFromWishlistCommand command, CancellationToken ct)
    {
        var wishlist = await _wishlists.GetByUserIdAsync(command.UserId, ct)
                       ?? Wishlist.Create(command.UserId);

        wishlist.RemoveProduct(command.ProductId); // no-op if not present — never fails
        await _wishlists.UpsertAsync(wishlist, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<WishlistDto>.Ok(wishlist.ToDto());
    }
}
```

**`Commands/ClearWishlist/ClearWishlistCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Commands.ClearWishlist;

public record ClearWishlistCommand(Guid UserId)
    : IRequest<Result<WishlistDto>>, ITransactionalCommand;
```

**`Commands/ClearWishlist/ClearWishlistCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Commands.ClearWishlist;

public class ClearWishlistCommandHandler(
    IWishlistRepository _wishlists,
    IUnitOfWork _uow
) : IRequestHandler<ClearWishlistCommand, Result<WishlistDto>>
{
    public async Task<Result<WishlistDto>> Handle(ClearWishlistCommand command, CancellationToken ct)
    {
        var wishlist = await _wishlists.GetByUserIdAsync(command.UserId, ct);
        if (wishlist is null)
            return Result<WishlistDto>.Ok(Wishlist.Create(command.UserId).ToDto());

        wishlist.Clear();
        await _wishlists.UpsertAsync(wishlist, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<WishlistDto>.Ok(wishlist.ToDto());
    }
}
```

---

### 8. Cross-context reader interface

Handlers need to query `Products` (Catalog) and `InventoryItems` (Inventory) without taking a direct EF dependency in Application. Define an interface here; implement it in Infrastructure.

**File: `Shopping/ECommerce.Shopping.Application/Interfaces/IShoppingDbReader.cs`**

```csharp
namespace ECommerce.Shopping.Application.Interfaces;

public record ProductPriceInfo(decimal Price, string Currency);

public interface IShoppingDbReader
{
    /// <summary>Returns price info for an active, non-deleted product, or null if not found.</summary>
    Task<ProductPriceInfo?> GetProductPriceAsync(Guid productId, CancellationToken ct);

    /// <summary>Returns true if the product exists and is active.</summary>
    Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct);

    /// <summary>Returns true if available inventory >= requested quantity.</summary>
    Task<bool> IsInStockAsync(Guid productId, int quantity, CancellationToken ct);
}
```

> **Why an interface instead of direct `AppDbContext`?** The Application project must not reference EF. The interface keeps Application testable (fake in tests, real in Infrastructure). In Phase 8, the implementation behind this interface becomes an HTTP client to the Catalog service.

---

### 9. Phase 7 event handler stub

**`EventHandlers/ClearCartOnOrderPlacedHandler.cs`**
```csharp
// NOTE: OrderPlacedEvent will come from the Ordering context (Phase 7).
// When Phase 7 arrives:
//   1. Add reference to ECommerce.Ordering.Domain (or shared contracts)
//   2. Implement this handler:
//
// public class ClearCartOnOrderPlacedHandler(
//     ICartRepository _carts,
//     IUnitOfWork _uow,
//     ILogger<ClearCartOnOrderPlacedHandler> _logger
// ) : INotificationHandler<OrderPlacedEvent>
// {
//     public async Task Handle(OrderPlacedEvent notification, CancellationToken ct)
//     {
//         var cart = await _carts.GetByUserIdAsync(notification.UserId, ct);
//         if (cart is null) return;
//         cart.Clear();
//         await _carts.UpsertAsync(cart, ct);
//         await _uow.SaveChangesAsync(ct);
//     }
// }

namespace ECommerce.Shopping.Application.EventHandlers;
// Placeholder — implement in Phase 7
```

### 10. Verify

```bash
cd src/backend
dotnet build Shopping/ECommerce.Shopping.Application/ECommerce.Shopping.Application.csproj
dotnet build
```

---

## Acceptance Criteria

- [ ] `ECommerce.Shopping.Application` project created and added to solution
- [ ] `IShoppingDbReader` interface isolates cross-context product/stock queries from Application logic
- [ ] **Cart**: `GetCartQuery` (load-or-create), `AddToCartCommand` (cross-context product check), `RemoveFromCartCommand`, `UpdateCartItemQuantityCommand`, `ClearCartCommand` (handles null userId for anonymous)
- [ ] **Cart**: `ValidateCartQuery` (owner/admin check + stock check)
- [ ] **Wishlist**: `GetWishlistQuery`, `IsProductInWishlistQuery`, `AddToWishlistCommand`, `RemoveFromWishlistCommand`, `ClearWishlistCommand`
- [ ] All commands implement `ITransactionalCommand` and inject `IUnitOfWork`
- [ ] `ShoppingMappingExtensions.ToDto()` for both `Cart` and `Wishlist`
- [ ] `ClearCartOnOrderPlacedHandler` stub with Phase 7 comment
- [ ] `dotnet build` passes

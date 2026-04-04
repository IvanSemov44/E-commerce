using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Application.Errors;
using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Queries.ValidateCart;

public class ValidateCartQueryHandler(
    ICartRepository _carts,
    IShoppingDbReader _db
) : IRequestHandler<ValidateCartQuery, Result>
{
    public async Task<Result> Handle(ValidateCartQuery query, CancellationToken ct)
    {
        var cart = await _carts.GetByIdAsync(query.CartId, ct);
        if (cart is null) return Result.Fail(ShoppingApplicationErrors.CartNotFound);

        if (!query.IsAdmin && query.RequestingUserId != cart.UserId)
            return Result.Fail(ShoppingApplicationErrors.Forbidden);

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
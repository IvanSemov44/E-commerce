using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Errors;
using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Commands.AddToCart;

public class AddToCartCommandHandler(
    ICartRepository _carts,
    IShoppingProductReader _productReader
) : IRequestHandler<AddToCartCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(AddToCartCommand command, CancellationToken ct)
    {
        var product = await _productReader.GetProductPriceAsync(command.ProductId, ct);
        if (product is null)
            return Result<CartDto>.Fail(ShoppingApplicationErrors.ProductNotFound);

        var cart = command.UserId is Guid uid
            ? await _carts.GetOrCreateForUserAsync(uid, ct)
            : command.SessionId is not null
                ? await _carts.GetOrCreateForSessionAsync(command.SessionId, ct)
                : null;

        if (cart is null)
            return Result<CartDto>.Fail(ShoppingApplicationErrors.CartNotFound);

        var result = cart.AddItem(command.ProductId, command.Quantity, product.Price, product.Currency);
        if (!result.IsSuccess) return Result<CartDto>.Fail(result.GetErrorOrThrow());

        return Result<CartDto>.Ok(cart.ToDto());
    }
}

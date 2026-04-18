using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Errors;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Commands.RemoveFromCart;

public class RemoveFromCartCommandHandler(
    ICartRepository _carts
) : IRequestHandler<RemoveFromCartCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(RemoveFromCartCommand command, CancellationToken ct)
    {
        var cart = command.UserId is Guid uid
            ? await _carts.GetOrCreateForUserAsync(uid, ct)
            : command.SessionId is not null
                ? await _carts.GetOrCreateForSessionAsync(command.SessionId, ct)
                : null;

        if (cart is null) return Result<CartDto>.Fail(ShoppingApplicationErrors.CartNotFound);

        var result = cart.RemoveItem(command.CartItemId);
        if (!result.IsSuccess) return Result<CartDto>.Fail(result.GetErrorOrThrow());

        return Result<CartDto>.Ok(cart.ToDto());
    }
}

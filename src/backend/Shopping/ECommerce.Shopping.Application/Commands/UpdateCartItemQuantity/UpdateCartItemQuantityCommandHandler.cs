using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Errors;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Application.Helpers;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Commands.UpdateCartItemQuantity;

public class UpdateCartItemQuantityCommandHandler(
    ICartRepository _carts
) : IRequestHandler<UpdateCartItemQuantityCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(UpdateCartItemQuantityCommand command, CancellationToken ct)
    {
        var cart = await _carts.ResolveCartAsync(command.UserId, command.SessionId, ct);
        if (cart is null) return Result<CartDto>.Fail(ShoppingApplicationErrors.CartNotFound);

        var result = cart.UpdateItemQuantity(command.CartItemId, command.NewQuantity);
        if (!result.IsSuccess) return Result<CartDto>.Fail(result.GetErrorOrThrow());

        return Result<CartDto>.Ok(cart.ToDto());
    }
}

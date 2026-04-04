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

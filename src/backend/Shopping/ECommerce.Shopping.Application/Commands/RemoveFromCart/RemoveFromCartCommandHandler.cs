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
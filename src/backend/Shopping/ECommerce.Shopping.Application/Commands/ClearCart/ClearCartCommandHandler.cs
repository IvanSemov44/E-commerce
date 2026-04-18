using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Application.Helpers;
using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Commands.ClearCart;

public class ClearCartCommandHandler(
    ICartRepository _carts
) : IRequestHandler<ClearCartCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(ClearCartCommand command, CancellationToken ct)
    {
        var cart = await _carts.ResolveCartAsync(command.UserId, command.SessionId, ct);
        if (cart is null)
            return Result<CartDto>.Ok(Cart.Create(Guid.Empty).ToDto());

        cart.Clear();

        return Result<CartDto>.Ok(cart.ToDto());
    }
}

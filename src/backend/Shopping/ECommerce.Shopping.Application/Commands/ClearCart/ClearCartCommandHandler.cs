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
            var empty = Cart.Create(Guid.Empty);
            return Result<CartDto>.Ok(empty.ToDto());
        }

        var cart = await _carts.GetByUserIdAsync(command.UserId.Value, ct);
        if (cart is null)
        {
            return Result<CartDto>.Ok(Cart.Create(command.UserId.Value).ToDto());
        }

        cart.Clear();
        await _carts.UpsertAsync(cart, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<CartDto>.Ok(cart.ToDto());
    }
}
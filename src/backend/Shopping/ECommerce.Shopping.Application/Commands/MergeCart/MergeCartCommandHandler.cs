using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Commands.MergeCart;

public class MergeCartCommandHandler(
    ICartRepository _carts,
    IUnitOfWork _uow
) : IRequestHandler<MergeCartCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(MergeCartCommand command, CancellationToken ct)
    {
        // Load session cart (anonymous cart)
        var sessionCart = await _carts.GetBySessionIdAsync(command.SessionId, ct);
        if (sessionCart is null || sessionCart.IsEmpty)
        {
            // No session cart or empty — user authenticates without prior shopping, return empty user cart
            var emptyUserCart = Cart.Create(command.UserId);
            return Result<CartDto>.Ok(emptyUserCart.ToDto());
        }

        // Load or create user cart
        var userCart = await _carts.GetByUserIdAsync(command.UserId, ct)
                    ?? Cart.Create(command.UserId);

        // Merge items: idempotent — AddItem increases qty if product already exists
        foreach (var item in sessionCart.Items)
        {
            var addResult = userCart.AddItem(
                item.ProductId,
                item.Quantity,
                item.UnitPrice,
                item.Currency);

            if (!addResult.IsSuccess)
                return Result<CartDto>.Fail(addResult.GetErrorOrThrow());
        }

        // Delete session cart (cleanup after merge)
        await _carts.DeleteAsync(sessionCart, ct);

        // Save merged user cart
        await _carts.UpsertAsync(userCart, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<CartDto>.Ok(userCart.ToDto());
    }
}

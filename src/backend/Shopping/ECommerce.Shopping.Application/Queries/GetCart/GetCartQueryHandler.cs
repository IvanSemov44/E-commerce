using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Queries.GetCart;

public class GetCartQueryHandler(ICartRepository _carts)
    : IRequestHandler<GetCartQuery, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(GetCartQuery query, CancellationToken ct)
    {
        var cart = await _carts.GetByUserIdAsync(query.UserId, ct)
                   ?? Cart.Create(query.UserId);

        return Result<CartDto>.Ok(cart.ToDto());
    }
}

namespace ECommerce.Shopping.Application.Queries.GetCart;

public class GetCartQueryHandler(ICartRepository _carts)
    : IRequestHandler<GetCartQuery, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(GetCartQuery query, CancellationToken ct)
    {
        var cart = query.UserId is Guid uid
            ? await _carts.GetByUserIdAsync(uid, ct)
            : query.SessionId is not null
                ? await _carts.GetBySessionIdAsync(query.SessionId, ct)
                : null;

        if (cart is null) return Result<CartDto>.Fail(ShoppingApplicationErrors.CartNotFound);

        return Result<CartDto>.Ok(cart.ToDto());
    }
}

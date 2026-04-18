
namespace ECommerce.Shopping.Application.Commands.ClearCart;

public class ClearCartCommandHandler(
    ICartRepository _carts
) : IRequestHandler<ClearCartCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(ClearCartCommand command, CancellationToken ct)
    {
        var cart = command.UserId is Guid uid
            ? await _carts.GetOrCreateForUserAsync(uid, ct)
            : command.SessionId is not null
                ? await _carts.GetOrCreateForSessionAsync(command.SessionId, ct)
                : null;

        if (cart is null)
            return Result<CartDto>.Ok(Cart.Create(Guid.Empty).ToDto());

        cart.Clear();

        return Result<CartDto>.Ok(cart.ToDto());
    }
}

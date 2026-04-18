namespace ECommerce.Shopping.Application.Commands.UpdateCartItemQuantity;

public class UpdateCartItemQuantityCommandHandler(
    ICartRepository _carts
) : IRequestHandler<UpdateCartItemQuantityCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(UpdateCartItemQuantityCommand command, CancellationToken ct)
    {
        var cart = command.UserId is Guid uid
            ? await _carts.GetOrCreateForUserAsync(uid, ct)
            : command.SessionId is not null
                ? await _carts.GetOrCreateForSessionAsync(command.SessionId, ct)
                : null;

        if (cart is null) return Result<CartDto>.Fail(ShoppingApplicationErrors.CartNotFound);

        var result = cart.UpdateItemQuantity(command.CartItemId, command.NewQuantity);
        if (!result.IsSuccess) return Result<CartDto>.Fail(result.GetErrorOrThrow());

        return Result<CartDto>.Ok(cart.ToDto());
    }
}

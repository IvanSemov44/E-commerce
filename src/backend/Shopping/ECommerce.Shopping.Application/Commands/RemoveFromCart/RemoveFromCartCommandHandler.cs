namespace ECommerce.Shopping.Application.Commands.RemoveFromCart;

public class RemoveFromCartCommandHandler(
    ICartRepository _carts
) : IRequestHandler<RemoveFromCartCommand, Result>
{
    public async Task<Result> Handle(RemoveFromCartCommand command, CancellationToken ct)
    {
        var cart = await _carts.ResolveAsync(command.UserId, command.SessionId, ct);
        if (cart is null) return Result.Fail(ShoppingApplicationErrors.CartNotFound);

        var result = cart.RemoveItem(command.CartItemId);
        if (!result.IsSuccess) return Result.Fail(result.GetErrorOrThrow());

        return Result.Ok();
    }
}

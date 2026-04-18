namespace ECommerce.Shopping.Application.Commands.UpdateCartItemQuantity;

public class UpdateCartItemQuantityCommandHandler(
    ICartRepository _carts
) : IRequestHandler<UpdateCartItemQuantityCommand, Result>
{
    public async Task<Result> Handle(UpdateCartItemQuantityCommand command, CancellationToken ct)
    {
        var cart = await _carts.ResolveAsync(command.UserId, command.SessionId, ct);
        if (cart is null) return Result.Fail(ShoppingApplicationErrors.CartNotFound);

        var result = cart.UpdateItemQuantity(command.CartItemId, command.NewQuantity);
        if (!result.IsSuccess) return Result.Fail(result.GetErrorOrThrow());

        return Result.Ok();
    }
}

namespace ECommerce.Shopping.Application.Commands.AddToCart;

public class AddToCartCommandHandler(
    ICartRepository _carts,
    IShoppingProductReader _productReader
) : IRequestHandler<AddToCartCommand, Result>
{
    public async Task<Result> Handle(AddToCartCommand command, CancellationToken ct)
    {
        var product = await _productReader.GetProductPriceAsync(command.ProductId, ct);
        if (product is null)
            return Result.Fail(ShoppingApplicationErrors.ProductNotFound);

        var cart = await _carts.ResolveAsync(command.UserId, command.SessionId, ct);
        if (cart is null)
            return Result.Fail(ShoppingApplicationErrors.CartNotFound);

        var result = cart.AddItem(command.ProductId, command.Quantity, product.Price, product.Currency);
        if (!result.IsSuccess) return Result.Fail(result.GetErrorOrThrow());

        return Result.Ok();
    }
}

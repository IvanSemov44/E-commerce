namespace ECommerce.Shopping.Application.Commands.AddToWishlist;

public class AddToWishlistCommandHandler(
    IWishlistRepository _wishlists,
    IShoppingProductReader _productReader
) : IRequestHandler<AddToWishlistCommand, Result>
{
    public async Task<Result> Handle(AddToWishlistCommand command, CancellationToken ct)
    {
        var product = await _productReader.GetProductPriceAsync(command.ProductId, ct);
        if (product is null)
            return Result.Fail(ShoppingApplicationErrors.ProductNotFound);

        var wishlist = await _wishlists.GetOrCreateForUserAsync(command.UserId, ct);

        var result = wishlist.AddProduct(command.ProductId);
        if (!result.IsSuccess) return Result.Fail(result.GetErrorOrThrow());

        return Result.Ok();
    }
}

namespace ECommerce.Shopping.Application.Commands.RemoveFromWishlist;

public class RemoveFromWishlistCommandHandler(
    IWishlistRepository _wishlists
) : IRequestHandler<RemoveFromWishlistCommand, Result>
{
    public async Task<Result> Handle(RemoveFromWishlistCommand command, CancellationToken ct)
    {
        var wishlist = await _wishlists.GetOrCreateForUserAsync(command.UserId, ct);
        wishlist.RemoveProduct(command.ProductId);
        return Result.Ok();
    }
}

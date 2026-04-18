namespace ECommerce.Shopping.Application.Commands.ClearWishlist;

public class ClearWishlistCommandHandler(
    IWishlistRepository _wishlists
) : IRequestHandler<ClearWishlistCommand, Result>
{
    public async Task<Result> Handle(ClearWishlistCommand command, CancellationToken ct)
    {
        var wishlist = await _wishlists.GetByUserIdAsync(command.UserId, ct);
        wishlist?.Clear();
        return Result.Ok();
    }
}

using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Queries.IsProductInWishlist;

public class IsProductInWishlistQueryHandler(IWishlistRepository _wishlists)
    : IRequestHandler<IsProductInWishlistQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(IsProductInWishlistQuery query, CancellationToken ct)
    {
        var wishlist = await _wishlists.GetByUserIdAsync(query.UserId, ct);
        var contains = wishlist?.Contains(query.ProductId) ?? false;
        return Result<bool>.Ok(contains);
    }
}
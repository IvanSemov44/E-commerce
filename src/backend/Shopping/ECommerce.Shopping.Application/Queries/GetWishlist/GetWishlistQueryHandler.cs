using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Queries.GetWishlist;

public class GetWishlistQueryHandler(IWishlistRepository _wishlists)
    : IRequestHandler<GetWishlistQuery, Result<WishlistDto>>
{
    public async Task<Result<WishlistDto>> Handle(GetWishlistQuery query, CancellationToken ct)
    {
        var wishlist = await _wishlists.GetByUserIdAsync(query.UserId, ct)
                       ?? Wishlist.Create(query.UserId);
        return Result<WishlistDto>.Ok(wishlist.ToDto());
    }
}
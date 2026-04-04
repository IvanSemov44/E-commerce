using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Commands.ClearWishlist;

public class ClearWishlistCommandHandler(
    IWishlistRepository _wishlists,
    IUnitOfWork _uow
) : IRequestHandler<ClearWishlistCommand, Result<WishlistDto>>
{
    public async Task<Result<WishlistDto>> Handle(ClearWishlistCommand command, CancellationToken ct)
    {
        var wishlist = await _wishlists.GetByUserIdAsync(command.UserId, ct);
        if (wishlist is null)
            return Result<WishlistDto>.Ok(Wishlist.Create(command.UserId).ToDto());

        wishlist.Clear();
        await _wishlists.UpsertAsync(wishlist, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<WishlistDto>.Ok(wishlist.ToDto());
    }
}
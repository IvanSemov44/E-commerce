using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Commands.RemoveFromWishlist;

public class RemoveFromWishlistCommandHandler(
    IWishlistRepository _wishlists,
    IUnitOfWork _uow
) : IRequestHandler<RemoveFromWishlistCommand, Result<WishlistDto>>
{
    public async Task<Result<WishlistDto>> Handle(RemoveFromWishlistCommand command, CancellationToken ct)
    {
        var wishlist = await _wishlists.GetByUserIdAsync(command.UserId, ct)
                       ?? Wishlist.Create(command.UserId);

        wishlist.RemoveProduct(command.ProductId);
        await _wishlists.UpsertAsync(wishlist, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<WishlistDto>.Ok(wishlist.ToDto());
    }
}
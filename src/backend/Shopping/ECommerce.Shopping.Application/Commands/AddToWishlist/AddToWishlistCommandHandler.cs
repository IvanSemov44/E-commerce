using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Errors;
using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Commands.AddToWishlist;

public class AddToWishlistCommandHandler(
    IWishlistRepository _wishlists,
    IShoppingProductReader _productReader,
    IUnitOfWork _uow
) : IRequestHandler<AddToWishlistCommand, Result<WishlistDto>>
{
    public async Task<Result<WishlistDto>> Handle(AddToWishlistCommand command, CancellationToken ct)
    {
        var productExists = await _productReader.ProductExistsAsync(command.ProductId, ct);
        if (!productExists)
            return Result<WishlistDto>.Fail(ShoppingApplicationErrors.ProductNotFound);

        var wishlist = await _wishlists.GetByUserIdAsync(command.UserId, ct)
                       ?? Wishlist.Create(command.UserId);

        var result = wishlist.AddProduct(command.ProductId);
        if (!result.IsSuccess) return Result<WishlistDto>.Fail(result.GetErrorOrThrow());

        await _wishlists.UpsertAsync(wishlist, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<WishlistDto>.Ok(wishlist.ToDto());
    }
}

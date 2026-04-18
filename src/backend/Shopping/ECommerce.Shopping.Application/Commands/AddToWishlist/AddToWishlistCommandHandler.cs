using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Errors;
using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Application.Mapping;
using ECommerce.Shopping.Domain.Interfaces;

namespace ECommerce.Shopping.Application.Commands.AddToWishlist;

public class AddToWishlistCommandHandler(
    IWishlistRepository _wishlists,
    IShoppingProductReader _productReader
) : IRequestHandler<AddToWishlistCommand, Result<WishlistDto>>
{
    public async Task<Result<WishlistDto>> Handle(AddToWishlistCommand command, CancellationToken ct)
    {
        var product = await _productReader.GetProductPriceAsync(command.ProductId, ct);
        if (product is null)
            return Result<WishlistDto>.Fail(ShoppingApplicationErrors.ProductNotFound);

        var wishlist = await _wishlists.GetOrCreateForUserAsync(command.UserId, ct);

        var result = wishlist.AddProduct(command.ProductId);
        if (!result.IsSuccess)
            return Result<WishlistDto>.Fail(result.GetErrorOrThrow());

        return Result<WishlistDto>.Ok(wishlist.ToDto());
    }
}

using ECommerce.Application.DTOs.Wishlist;
using ECommerce.Application.Interfaces;
using ECommerce.Core.Results;
using MediatR;

namespace ECommerce.API.Features.Wishlist;

public record GetWishlistQuery(Guid UserId) : IRequest<Result<WishlistDto>>;
public record AddToWishlistCommand(Guid UserId, Guid ProductId) : IRequest<Result<WishlistDto>>;
public record RemoveFromWishlistCommand(Guid UserId, Guid ProductId) : IRequest<Result<WishlistDto>>;
public record ClearWishlistCommand(Guid UserId) : IRequest<Result<WishlistDto>>;
public record IsProductInWishlistQuery(Guid UserId, Guid ProductId) : IRequest<bool>;

public class GetWishlistQueryHandler(IWishlistService wishlistService)
    : IRequestHandler<GetWishlistQuery, Result<WishlistDto>>
{
    public Task<Result<WishlistDto>> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
        => wishlistService.GetUserWishlistAsync(request.UserId, cancellationToken);
}

public class AddToWishlistCommandHandler(IWishlistService wishlistService)
    : IRequestHandler<AddToWishlistCommand, Result<WishlistDto>>
{
    public Task<Result<WishlistDto>> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
        => wishlistService.AddToWishlistAsync(request.UserId, request.ProductId, cancellationToken);
}

public class RemoveFromWishlistCommandHandler(IWishlistService wishlistService)
    : IRequestHandler<RemoveFromWishlistCommand, Result<WishlistDto>>
{
    public Task<Result<WishlistDto>> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken)
        => wishlistService.RemoveFromWishlistAsync(request.UserId, request.ProductId, cancellationToken);
}

public class ClearWishlistCommandHandler(IWishlistService wishlistService)
    : IRequestHandler<ClearWishlistCommand, Result<WishlistDto>>
{
    public Task<Result<WishlistDto>> Handle(ClearWishlistCommand request, CancellationToken cancellationToken)
        => wishlistService.ClearWishlistAsync(request.UserId, cancellationToken);
}

public class IsProductInWishlistQueryHandler(IWishlistService wishlistService)
    : IRequestHandler<IsProductInWishlistQuery, bool>
{
    public Task<bool> Handle(IsProductInWishlistQuery request, CancellationToken cancellationToken)
        => wishlistService.IsProductInWishlistAsync(request.UserId, request.ProductId, cancellationToken);
}

using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Wishlist;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Results;
using ECommerce.Core.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace ECommerce.Application.Services;

public class WishlistService : IWishlistService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<WishlistService> _logger;

    public WishlistService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<WishlistService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<WishlistDto>> GetUserWishlistAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            return Result<WishlistDto>.Fail(ErrorCodes.UserNotFound, $"User with id '{userId}' not found");

        // FIX: Use database-level filtering instead of loading ALL entries
        var wishlistEntries = await _unitOfWork.Wishlists.GetAllByUserIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);

        var wishlistDto = await MapWishlistToDtoAsync(wishlistEntries.ToList(), cancellationToken);
        return Result<WishlistDto>.Ok(wishlistDto);
    }

    public async Task<Result<WishlistDto>> AddToWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        // Note: Using GetByIdAsync for test compatibility. ExistsAsync would be more efficient.
        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            return Result<WishlistDto>.Fail(ErrorCodes.UserNotFound, $"User with id '{userId}' not found");
        
        var product = await _unitOfWork.Products.GetByIdAsync(productId, trackChanges: false, cancellationToken: cancellationToken);
        if (product == null)
            return Result<WishlistDto>.Fail(ErrorCodes.ProductNotFound, $"Product with id '{productId}' not found");
        
        if (await _unitOfWork.Wishlists.IsProductInWishlistAsync(userId, productId, cancellationToken: cancellationToken))
            return Result<WishlistDto>.Fail(ErrorCodes.DuplicateWishlistItem, "This product is already in your wishlist");

        var wishlistEntry = new Wishlist
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId
        };

        await _unitOfWork.Wishlists.AddAsync(wishlistEntry, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return await GetUserWishlistAsync(userId, cancellationToken);
    }

    public async Task<Result<WishlistDto>> RemoveFromWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            return Result<WishlistDto>.Fail(ErrorCodes.UserNotFound, $"User with id '{userId}' not found");
        
        // FIX: Use efficient deletion without loading all entries
        await _unitOfWork.Wishlists.DeleteByUserIdAndProductIdAsync(userId, productId, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return await GetUserWishlistAsync(userId, cancellationToken);
    }

    public async Task<bool> IsProductInWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Wishlists.IsProductInWishlistAsync(userId, productId, cancellationToken: cancellationToken);
    }

    public async Task<Result<WishlistDto>> ClearWishlistAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            return Result<WishlistDto>.Fail(ErrorCodes.UserNotFound, $"User with id '{userId}' not found");
        
        // FIX: Use efficient database query and batch delete
        var userWishlistEntries = await _unitOfWork.Wishlists.GetAllByUserIdAsync(userId, trackChanges: true, cancellationToken: cancellationToken);

        if (userWishlistEntries.Any())
        {
            await _unitOfWork.Wishlists.DeleteRangeAsync(userWishlistEntries, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var result = new WishlistDto { Id = userId, Items = new List<WishlistItemDto>(), ItemCount = 0 };
        return Result<WishlistDto>.Ok(result);
    }

    private async Task<WishlistDto> MapWishlistToDtoAsync(List<Wishlist> wishlistEntries, CancellationToken cancellationToken = default)
    {
        var userId = wishlistEntries.FirstOrDefault()?.UserId ?? Guid.Empty;
        var items = new List<WishlistItemDto>();

        if (wishlistEntries.Any())
        {
            // Product is already included via eager loading in the calling method
            // No need for additional queries
            foreach (var entry in wishlistEntries)
            {
                if (entry.Product == null)
                    continue;

                // Use AutoMapper to map product -> base WishlistItemDto fields, then add entry-specific data
                var baseItemDto = _mapper.Map<ECommerce.Application.DTOs.Wishlist.WishlistItemDto>(entry.Product);
                var wishlistItemDto = baseItemDto with { Id = entry.Id, AddedAt = entry.CreatedAt };
                items.Add(wishlistItemDto);
            }
        }

        return new WishlistDto { Id = userId, Items = items, ItemCount = items.Count };
    }
}

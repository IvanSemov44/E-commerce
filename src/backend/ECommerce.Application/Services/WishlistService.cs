using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Wishlist;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Exceptions;
using System.Threading;

namespace ECommerce.Application.Services;

public class WishlistService : IWishlistService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public WishlistService(
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<WishlistDto> GetUserWishlistAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            throw new UserNotFoundException(userId);

        var wishlistEntries = await _unitOfWork.Wishlists.GetAllAsync(trackChanges: false, cancellationToken: cancellationToken);
        var userWishlistEntries = wishlistEntries
            .Where(w => w.UserId == userId)
            .ToList();

        return await MapWishlistToDtoAsync(userWishlistEntries);
    }

    public async Task<WishlistDto> AddToWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            throw new UserNotFoundException(userId);
        var product = await _unitOfWork.Products.GetByIdAsync(productId, trackChanges: false, cancellationToken: cancellationToken);
        if (product == null)
            throw new ProductNotFoundException(productId);
        if (await _unitOfWork.Wishlists.IsProductInWishlistAsync(userId, productId, cancellationToken: cancellationToken))
            throw new DuplicateWishlistItemException();

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

    public async Task<WishlistDto> RemoveFromWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            throw new UserNotFoundException(userId);
        var wishlistEntries = await _unitOfWork.Wishlists.GetAllAsync(trackChanges: false, cancellationToken: cancellationToken);
        var entryToRemove = wishlistEntries.FirstOrDefault(w => w.UserId == userId && w.ProductId == productId);
        if (entryToRemove == null)
            throw new WishlistItemNotFoundException();
        await _unitOfWork.Wishlists.DeleteAsync(entryToRemove, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return await GetUserWishlistAsync(userId, cancellationToken);
    }

    public async Task<bool> IsProductInWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Wishlists.IsProductInWishlistAsync(userId, productId, cancellationToken: cancellationToken);
    }

    public async Task<WishlistDto> ClearWishlistAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            throw new UserNotFoundException(userId);
        var wishlistEntries = await _unitOfWork.Wishlists.GetAllAsync(trackChanges: false, cancellationToken: cancellationToken);
        var userWishlistEntries = wishlistEntries
            .Where(w => w.UserId == userId)
            .ToList();

        foreach (var entry in userWishlistEntries)
        {
            await _unitOfWork.Wishlists.DeleteAsync(entry, cancellationToken: cancellationToken);
        }
        if (userWishlistEntries.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        }

        return new WishlistDto { Id = userId, Items = new List<WishlistItemDto>(), ItemCount = 0 };
    }

    private async Task<WishlistDto> MapWishlistToDtoAsync(List<Wishlist> wishlistEntries)
    {
        var dto = new WishlistDto { Id = wishlistEntries.FirstOrDefault()?.UserId ?? Guid.Empty };

        foreach (var entry in wishlistEntries)
        {
            // Fetch product details (repository may not include navigation property)
            var product = await _unitOfWork.Products.GetByIdAsync(entry.ProductId, trackChanges: false);
            if (product == null) continue;

            // Use AutoMapper to map product -> WishlistItemDto, then fill entry-specific fields
            var wishlistItemDto = _mapper.Map<ECommerce.Application.DTOs.Wishlist.WishlistItemDto>(product);
            wishlistItemDto.Id = entry.Id;
            wishlistItemDto.AddedAt = entry.CreatedAt;

            dto.Items.Add(wishlistItemDto);
        }

        dto.ItemCount = dto.Items.Count;
        return dto;
    }
}

using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Wishlist;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Exceptions;

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

    public async Task<WishlistDto> GetUserWishlistAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false);
        if (user == null)
            throw new UserNotFoundException(userId);

        var wishlistEntries = await _unitOfWork.Wishlists.GetAllAsync(trackChanges: false);
        var userWishlistEntries = wishlistEntries
            .Where(w => w.UserId == userId)
            .ToList();

        return await MapWishlistToDtoAsync(userWishlistEntries);
    }

    public async Task<WishlistDto> AddToWishlistAsync(Guid userId, Guid productId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false);
        if (user == null)
            throw new UserNotFoundException(userId);
        var product = await _unitOfWork.Products.GetByIdAsync(productId, trackChanges: false);
        if (product == null)
            throw new ProductNotFoundException(productId);
        if (await _unitOfWork.Wishlists.IsProductInWishlistAsync(userId, productId))
            throw new DuplicateWishlistItemException();

        var wishlistEntry = new Wishlist
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId
        };

        await _unitOfWork.Wishlists.AddAsync(wishlistEntry);
        await _unitOfWork.SaveChangesAsync();

        return await GetUserWishlistAsync(userId);
    }

    public async Task<WishlistDto> RemoveFromWishlistAsync(Guid userId, Guid productId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false);
        if (user == null)
            throw new UserNotFoundException(userId);
        var wishlistEntries = await _unitOfWork.Wishlists.GetAllAsync(trackChanges: false);
        var entryToRemove = wishlistEntries.FirstOrDefault(w => w.UserId == userId && w.ProductId == productId);
        if (entryToRemove == null)
            throw new WishlistItemNotFoundException();
        await _unitOfWork.Wishlists.DeleteAsync(entryToRemove);
        await _unitOfWork.SaveChangesAsync();

        return await GetUserWishlistAsync(userId);
    }

    public async Task<bool> IsProductInWishlistAsync(Guid userId, Guid productId)
    {
        return await _unitOfWork.Wishlists.IsProductInWishlistAsync(userId, productId);
    }

    public async Task<WishlistDto> ClearWishlistAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false);
        if (user == null)
            throw new UserNotFoundException(userId);
        var wishlistEntries = await _unitOfWork.Wishlists.GetAllAsync(trackChanges: false);
        var userWishlistEntries = wishlistEntries
            .Where(w => w.UserId == userId)
            .ToList();

        foreach (var entry in userWishlistEntries)
        {
            await _unitOfWork.Wishlists.DeleteAsync(entry);
        }
        if (userWishlistEntries.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return new WishlistDto { Id = userId, Items = new List<WishlistItemDto>(), ItemCount = 0 };
    }

    private async Task<WishlistDto> MapWishlistToDtoAsync(List<Wishlist> wishlistEntries)
    {
        var dto = new WishlistDto { Id = wishlistEntries.FirstOrDefault()?.UserId ?? Guid.Empty };

        foreach (var entry in wishlistEntries)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(entry.ProductId, trackChanges: false);
            if (product == null) continue;

            var wishlistItemDto = new WishlistItemDto
            {
                Id = entry.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                ProductImage = product.Images.FirstOrDefault(x => x.IsPrimary)?.Url
                    ?? product.Images.FirstOrDefault()?.Url,
                Price = product.Price,
                CompareAtPrice = product.CompareAtPrice,
                StockQuantity = product.StockQuantity,
                IsAvailable = product.IsActive && product.StockQuantity > 0,
                AddedAt = entry.CreatedAt
            };

            dto.Items.Add(wishlistItemDto);
        }

        dto.ItemCount = dto.Items.Count;
        return dto;
    }
}

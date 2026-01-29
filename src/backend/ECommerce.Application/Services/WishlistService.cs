using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Wishlist;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;

namespace ECommerce.Application.Services;

public class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public WishlistService(
        IWishlistRepository wishlistRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _wishlistRepository = wishlistRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<WishlistDto> GetUserWishlistAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException($"User {userId} not found");

        // Get all wishlist entries for this user
        var wishlistEntries = await _wishlistRepository.GetAllAsync();
        var userWishlistEntries = wishlistEntries
            .Where(w => w.UserId == userId)
            .ToList();

        return await MapWishlistToDtoAsync(userWishlistEntries);
    }

    public async Task<WishlistDto> AddToWishlistAsync(Guid userId, Guid productId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException($"User {userId} not found");

        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
            throw new InvalidOperationException($"Product {productId} not found");

        // Check if already in wishlist
        if (await _wishlistRepository.IsProductInWishlistAsync(userId, productId))
            throw new InvalidOperationException("Product already in wishlist");

        var wishlistEntry = new Wishlist
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId
        };

        await _wishlistRepository.AddAsync(wishlistEntry);
        await _wishlistRepository.SaveChangesAsync();

        // Reload user wishlist
        return await GetUserWishlistAsync(userId);
    }

    public async Task<WishlistDto> RemoveFromWishlistAsync(Guid userId, Guid productId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException($"User {userId} not found");

        // Get all wishlist entries
        var wishlistEntries = await _wishlistRepository.GetAllAsync();
        var entryToRemove = wishlistEntries.FirstOrDefault(w => w.UserId == userId && w.ProductId == productId)
            ?? throw new InvalidOperationException("Product not in wishlist");

        await _wishlistRepository.DeleteAsync(entryToRemove);
        await _wishlistRepository.SaveChangesAsync();

        // Reload user wishlist
        return await GetUserWishlistAsync(userId);
    }

    public async Task<bool> IsProductInWishlistAsync(Guid userId, Guid productId)
    {
        return await _wishlistRepository.IsProductInWishlistAsync(userId, productId);
    }

    public async Task<WishlistDto> ClearWishlistAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException($"User {userId} not found");

        // Get all wishlist entries for this user
        var wishlistEntries = await _wishlistRepository.GetAllAsync();
        var userWishlistEntries = wishlistEntries
            .Where(w => w.UserId == userId)
            .ToList();

        // Delete all entries
        foreach (var entry in userWishlistEntries)
        {
            await _wishlistRepository.DeleteAsync(entry);
        }
        if (userWishlistEntries.Count > 0)
        {
            await _wishlistRepository.SaveChangesAsync();
        }

        // Return empty wishlist
        return new WishlistDto { Id = userId, Items = new List<WishlistItemDto>(), ItemCount = 0 };
    }

    private async Task<WishlistDto> MapWishlistToDtoAsync(List<Wishlist> wishlistEntries)
    {
        var dto = new WishlistDto { Id = wishlistEntries.FirstOrDefault()?.UserId ?? Guid.Empty };

        foreach (var entry in wishlistEntries)
        {
            var product = await _productRepository.GetByIdAsync(entry.ProductId);
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

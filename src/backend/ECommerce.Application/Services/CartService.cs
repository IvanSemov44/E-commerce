using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;

namespace ECommerce.Application.Services;

public class CartService : ICartService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public CartService(
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    private async Task<Cart> GetOrCreateCartEntityAsync(Guid? userId, string? sessionId)
    {
        Cart? cart = null;

        if (userId.HasValue)
        {
            cart = await _unitOfWork.Carts.GetByUserIdAsync(userId.Value);
        }
        else if (!string.IsNullOrEmpty(sessionId))
        {
            cart = await _unitOfWork.Carts.GetBySessionIdAsync(sessionId);
        }

        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId,
                SessionId = sessionId,
                Items = new List<CartItem>()
            };

            await _unitOfWork.Carts.AddAsync(cart);
            await _unitOfWork.SaveChangesAsync();
        }

        return cart;
    }

    public async Task<CartDto> GetOrCreateCartAsync(Guid? userId, string? sessionId)
    {
        var cart = await GetOrCreateCartEntityAsync(userId, sessionId);
        return await MapCartToDtoAsync(cart);
    }

    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);
        if (cart == null)
            throw new CartNotFoundException($"Cart not found for user {userId}");

        return await MapCartToDtoAsync(cart);
    }

    public async Task<CartDto> AddToCartAsync(Guid? userId, string? sessionId, Guid productId, int quantity)
    {
        if (quantity <= 0)
            throw new InvalidQuantityException("Quantity must be greater than 0");

        var product = await _unitOfWork.Products.GetByIdAsync(productId, trackChanges: false);
        if (product == null)
            throw new ProductNotFoundException(productId);

        if (product.StockQuantity < quantity)
            throw new InsufficientStockException(product.Name, quantity, product.StockQuantity);

        var cart = await GetOrCreateCartEntityAsync(userId, sessionId);

        var existingItem = cart.Items.FirstOrDefault(x => x.ProductId == productId);

        if (existingItem != null)
        {
            // Check if adding more quantity exceeds stock
            if (existingItem.Quantity + quantity > product.StockQuantity)
                throw new InsufficientStockException(product.Name, existingItem.Quantity + quantity, product.StockQuantity);

            existingItem.Quantity += quantity;
            await _unitOfWork.SaveChangesAsync();
        }
        else
        {
            var cartItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = productId,
                Quantity = quantity
            };
            await _unitOfWork.CartItems.AddAsync(cartItem);
            await _unitOfWork.SaveChangesAsync();
        }

        // Reload cart to get fresh data
        cart = await _unitOfWork.Carts.GetCartWithItemsAsync(cart.Id);
        if (cart == null)
            throw new CartNotFoundException(cart.Id);

        return await MapCartToDtoAsync(cart);
    }

    public async Task<CartDto> UpdateCartItemAsync(Guid? userId, string? sessionId, Guid cartItemId, int quantity)
    {
        if (quantity < 0)
            throw new InvalidQuantityException("Quantity cannot be negative");

        var cart = await GetOrCreateCartEntityAsync(userId, sessionId);
        var cartItem = cart.Items.FirstOrDefault(x => x.Id == cartItemId);
        if (cartItem == null)
            throw new CartItemNotFoundException(cartItemId);

        var product = await _unitOfWork.Products.GetByIdAsync(cartItem.ProductId, trackChanges: false);
        if (product == null)
            throw new ProductNotFoundException(cartItem.ProductId);

        if (quantity > product.StockQuantity)
            throw new InsufficientStockException(product.Name, quantity, product.StockQuantity);

        if (quantity == 0)
        {
            cart.Items.Remove(cartItem);
        }
        else
        {
            cartItem.Quantity = quantity;
        }

        await _unitOfWork.SaveChangesAsync();
        return await MapCartToDtoAsync(cart);
    }

    public async Task<CartDto> RemoveFromCartAsync(Guid? userId, string? sessionId, Guid cartItemId)
    {
        var cart = await GetOrCreateCartEntityAsync(userId, sessionId);
        var cartItem = cart.Items.FirstOrDefault(x => x.Id == cartItemId);
        if (cartItem == null)
            throw new CartItemNotFoundException(cartItemId);

        await _unitOfWork.CartItems.DeleteAsync(cartItem);
        await _unitOfWork.SaveChangesAsync();

        // Reload cart to get fresh data
        cart = await _unitOfWork.Carts.GetCartWithItemsAsync(cart.Id);
        if (cart == null)
            throw new CartNotFoundException(cart.Id);

        return await MapCartToDtoAsync(cart);
    }

    public async Task<CartDto> ClearCartAsync(Guid? userId, string? sessionId)
    {
        var cart = await GetOrCreateCartEntityAsync(userId, sessionId);

        foreach (var item in cart.Items.ToList())
        {
            await _unitOfWork.CartItems.DeleteAsync(item);
        }
        await _unitOfWork.SaveChangesAsync();

        // Reload cart to get fresh data
        cart = await _unitOfWork.Carts.GetCartWithItemsAsync(cart.Id);
        if (cart == null)
            throw new CartNotFoundException(cart.Id);

        return await MapCartToDtoAsync(cart);
    }

    public async Task<CartDto> GetCartByIdAsync(Guid cartId)
    {
        var cart = await _unitOfWork.Carts.GetCartWithItemsAsync(cartId);
        if (cart == null)
            throw new CartNotFoundException(cartId);

        return await MapCartToDtoAsync(cart);
    }

    public async Task ValidateCartAsync(Guid cartId)
    {
        var cart = await _unitOfWork.Carts.GetCartWithItemsAsync(cartId);
        if (cart == null)
            throw new CartNotFoundException(cartId);

        foreach (var item in cart.Items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, trackChanges: false);
            if (product == null)
                throw new ProductNotFoundException(item.ProductId);

            if (product.StockQuantity < item.Quantity)
                throw new InsufficientStockException(product.Name, item.Quantity, product.StockQuantity);

            if (!product.IsActive)
                throw new ProductNotAvailableException(product.Name);
        }
    }

    private async Task<CartDto> MapCartToDtoAsync(Cart cart)
    {
        // Use AutoMapper to map cart and its items (CartRepository ensures Product is included)
        var dto = _mapper.Map<CartDto>(cart);

        dto.Subtotal = dto.Items.Sum(x => x.Total);
        dto.Total = dto.Subtotal; // Could add tax/shipping calculations here

        return dto;
    }
}

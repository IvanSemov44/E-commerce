using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;

namespace ECommerce.Application.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IRepository<CartItem> _cartItemRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public CartService(
        ICartRepository cartRepository,
        IRepository<CartItem> cartItemRepository,
        IProductRepository productRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _productRepository = productRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    private async Task<Cart> GetOrCreateCartEntityAsync(Guid? userId, string? sessionId)
    {
        Cart? cart = null;

        if (userId.HasValue)
        {
            cart = await _cartRepository.GetByUserIdAsync(userId.Value);
        }
        else if (!string.IsNullOrEmpty(sessionId))
        {
            cart = await _cartRepository.GetBySessionIdAsync(sessionId);
        }

        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId,
                SessionId = sessionId,
                Items = new List<CartItem>()
            };

            await _cartRepository.AddAsync(cart);
            await _cartRepository.SaveChangesAsync();
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
        var cart = await _cartRepository.GetByUserIdAsync(userId);
        if (cart == null)
            throw new CartNotFoundException($"Cart not found for user {userId}");

        return await MapCartToDtoAsync(cart);
    }

    public async Task<CartDto> AddToCartAsync(Guid? userId, string? sessionId, Guid productId, int quantity)
    {
        if (quantity <= 0)
            throw new InvalidQuantityException("Quantity must be greater than 0");

        var product = await _productRepository.GetByIdAsync(productId);
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
            await _cartItemRepository.AddAsync(cartItem);
            await _unitOfWork.SaveChangesAsync();
        }

        // Reload cart to get fresh data
        cart = await _cartRepository.GetCartWithItemsAsync(cart.Id);
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

        var product = await _productRepository.GetByIdAsync(cartItem.ProductId);
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

        await _cartRepository.SaveChangesAsync();
        return await MapCartToDtoAsync(cart);
    }

    public async Task<CartDto> RemoveFromCartAsync(Guid? userId, string? sessionId, Guid cartItemId)
    {
        var cart = await GetOrCreateCartEntityAsync(userId, sessionId);
        var cartItem = cart.Items.FirstOrDefault(x => x.Id == cartItemId);
        if (cartItem == null)
            throw new CartItemNotFoundException(cartItemId);

        await _cartItemRepository.DeleteAsync(cartItem);
        await _unitOfWork.SaveChangesAsync();

        // Reload cart to get fresh data
        cart = await _cartRepository.GetCartWithItemsAsync(cart.Id);
        if (cart == null)
            throw new CartNotFoundException(cart.Id);

        return await MapCartToDtoAsync(cart);
    }

    public async Task<CartDto> ClearCartAsync(Guid? userId, string? sessionId)
    {
        var cart = await GetOrCreateCartEntityAsync(userId, sessionId);

        foreach (var item in cart.Items.ToList())
        {
            await _cartItemRepository.DeleteAsync(item);
        }
        await _unitOfWork.SaveChangesAsync();

        // Reload cart to get fresh data
        cart = await _cartRepository.GetCartWithItemsAsync(cart.Id);
        if (cart == null)
            throw new CartNotFoundException(cart.Id);

        return await MapCartToDtoAsync(cart);
    }

    public async Task<CartDto> GetCartByIdAsync(Guid cartId)
    {
        var cart = await _cartRepository.GetCartWithItemsAsync(cartId);
        if (cart == null)
            throw new CartNotFoundException(cartId);

        return await MapCartToDtoAsync(cart);
    }

    public async Task ValidateCartAsync(Guid cartId)
    {
        var cart = await _cartRepository.GetCartWithItemsAsync(cartId);
        if (cart == null)
            throw new CartNotFoundException(cartId);

        foreach (var item in cart.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
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
        var dto = new CartDto { Id = cart.Id };

        foreach (var item in cart.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product == null) continue;

            var cartItemDto = new CartItemDto
            {
                Id = item.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                ProductImage = product.Images.FirstOrDefault(x => x.IsPrimary)?.Url
                    ?? product.Images.FirstOrDefault()?.Url,
                Price = product.Price,
                Quantity = item.Quantity,
                Total = product.Price * item.Quantity
            };

            dto.Items.Add(cartItemDto);
        }

        dto.Subtotal = dto.Items.Sum(x => x.Total);
        dto.Total = dto.Subtotal; // Could add tax/shipping calculations here

        return dto;
    }
}

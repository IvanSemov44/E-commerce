using ECommerce.Application.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Results;
using ECommerce.Core.Constants;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace ECommerce.Application.Services;

public class CartService : ICartService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CartService> _logger;

    public CartService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CartService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    private async Task<Cart> GetOrCreateCartEntityAsync(Guid? userId, string? sessionId, CancellationToken cancellationToken = default)
    {
        Cart? cart = null;

        if (userId.HasValue)
        {
            cart = await _unitOfWork.Carts.GetByUserIdAsync(userId.Value, cancellationToken: cancellationToken);
        }
        else if (!string.IsNullOrEmpty(sessionId))
        {
            cart = await _unitOfWork.Carts.GetBySessionIdAsync(sessionId, cancellationToken: cancellationToken);
        }

        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId,
                SessionId = sessionId,
                Items = new List<CartItem>()
            };

            await _unitOfWork.Carts.AddAsync(cart, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        }

        return cart;
    }

    public async Task<Result<CartDto>> GetOrCreateCartAsync(Guid? userId, string? sessionId, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartEntityAsync(userId, sessionId, cancellationToken);
        var dto = await MapCartToDtoAsync(cart, cancellationToken);
        return Result<CartDto>.Ok(dto);
    }

    public async Task<Result<CartDto>> GetCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId, cancellationToken: cancellationToken);
        if (cart == null)
            return Result<CartDto>.Fail(ErrorCodes.CartNotFound, $"Cart not found for user {userId}");

        var dto = await MapCartToDtoAsync(cart, cancellationToken);
        return Result<CartDto>.Ok(dto);
    }

    public async Task<Result<CartDto>> AddToCartAsync(Guid? userId, string? sessionId, Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            return Result<CartDto>.Fail(ErrorCodes.InvalidQuantity, "Quantity must be greater than 0");

        var product = await _unitOfWork.Products.GetByIdAsync(productId, trackChanges: false, cancellationToken: cancellationToken);
        if (product == null)
            return Result<CartDto>.Fail(ErrorCodes.ProductNotFound, $"Product {productId} not found");

        if (product.StockQuantity < quantity)
            return Result<CartDto>.Fail(ErrorCodes.InsufficientStock, $"Insufficient stock for {product.Name}. Available: {product.StockQuantity}, Requested: {quantity}");

        var cart = await GetOrCreateCartEntityAsync(userId, sessionId, cancellationToken);

        var existingItem = cart.Items.FirstOrDefault(x => x.ProductId == productId);

        if (existingItem != null)
        {
            // Check if adding more quantity exceeds stock
            if (existingItem.Quantity + quantity > product.StockQuantity)
                return Result<CartDto>.Fail(ErrorCodes.InsufficientStock, $"Insufficient stock for {product.Name}. Available: {product.StockQuantity}, Requested: {existingItem.Quantity + quantity}");

            existingItem.Quantity += quantity;
            await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
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
            await _unitOfWork.CartItems.AddAsync(cartItem, cancellationToken: cancellationToken);
            
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_CartItems_CartId_ProductId") == true)
            {
                // Race condition: another request already added this product to the cart
                // Detach the failed entity from the change tracker
                _unitOfWork.DetachEntity(cartItem);
                
                // Reload the cart and update the existing item's quantity instead
                var existingCartId = cart.Id;
                cart = await _unitOfWork.Carts.GetCartWithItemsAsync(existingCartId, cancellationToken: cancellationToken);
                if (cart == null)
                    return Result<CartDto>.Fail(ErrorCodes.CartNotFound, $"Cart {existingCartId} not found");
                    
                existingItem = cart.Items.FirstOrDefault(x => x.ProductId == productId);
                if (existingItem != null)
                {
                    if (existingItem.Quantity + quantity > product.StockQuantity)
                        return Result<CartDto>.Fail(ErrorCodes.InsufficientStock, $"Insufficient stock for {product.Name}. Available: {product.StockQuantity}, Requested: {existingItem.Quantity + quantity}");
                    
                    existingItem.Quantity += quantity;
                    await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                }
            }
        }

        // Reload cart to get fresh data
        var cartId = cart.Id;
        cart = await _unitOfWork.Carts.GetCartWithItemsAsync(cartId, cancellationToken: cancellationToken);
        if (cart == null)
            return Result<CartDto>.Fail(ErrorCodes.CartNotFound, $"Cart {cartId} not found");

        var resultDto = await MapCartToDtoAsync(cart, cancellationToken);
        return Result<CartDto>.Ok(resultDto);
    }

    public async Task<Result<CartDto>> UpdateCartItemAsync(Guid? userId, string? sessionId, Guid cartItemId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity < 0)
            return Result<CartDto>.Fail(ErrorCodes.InvalidQuantity, "Quantity cannot be negative");

        var cart = await GetOrCreateCartEntityAsync(userId, sessionId, cancellationToken);
        var cartItem = cart.Items.FirstOrDefault(x => x.Id == cartItemId);
        if (cartItem == null)
            return Result<CartDto>.Fail(ErrorCodes.CartItemNotFound, $"Cart item {cartItemId} not found");

        var product = await _unitOfWork.Products.GetByIdAsync(cartItem.ProductId, trackChanges: false, cancellationToken: cancellationToken);
        if (product == null)
            return Result<CartDto>.Fail(ErrorCodes.ProductNotFound, $"Product {cartItem.ProductId} not found");

        if (quantity > product.StockQuantity)
            return Result<CartDto>.Fail(ErrorCodes.InsufficientStock, $"Insufficient stock for {product.Name}. Available: {product.StockQuantity}, Requested: {quantity}");

        if (quantity == 0)
        {
            cart.Items.Remove(cartItem);
        }
        else
        {
            cartItem.Quantity = quantity;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        var dto = await MapCartToDtoAsync(cart, cancellationToken);
        return Result<CartDto>.Ok(dto);
    }

    public async Task<Result<CartDto>> RemoveFromCartAsync(Guid? userId, string? sessionId, Guid cartItemId, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartEntityAsync(userId, sessionId, cancellationToken);
        var cartItem = cart.Items.FirstOrDefault(x => x.Id == cartItemId);
        if (cartItem == null)
            return Result<CartDto>.Fail(ErrorCodes.CartItemNotFound, $"Cart item {cartItemId} not found");

        await _unitOfWork.CartItems.DeleteAsync(cartItem, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        // Reload cart to get fresh data
        var cartId = cart.Id;
        cart = await _unitOfWork.Carts.GetCartWithItemsAsync(cartId, cancellationToken: cancellationToken);
        if (cart == null)
            return Result<CartDto>.Fail(ErrorCodes.CartNotFound, $"Cart {cartId} not found");

        var dto = await MapCartToDtoAsync(cart, cancellationToken);
        return Result<CartDto>.Ok(dto);
    }

    public async Task<Result<CartDto>> ClearCartAsync(Guid? userId, string? sessionId, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartEntityAsync(userId, sessionId, cancellationToken);

        if (cart.Items.Count > 0)
        {
            await _unitOfWork.CartItems.DeleteRangeAsync(cart.Items.ToList(), cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        }

        // Reload cart to get fresh data
        var cartId = cart.Id;
        cart = await _unitOfWork.Carts.GetCartWithItemsAsync(cartId, cancellationToken: cancellationToken);
        if (cart == null)
            return Result<CartDto>.Fail(ErrorCodes.CartNotFound, $"Cart {cartId} not found");

        var dto = await MapCartToDtoAsync(cart, cancellationToken);
        return Result<CartDto>.Ok(dto);
    }

    public async Task<Result<CartDto>> GetCartByIdAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        var cart = await _unitOfWork.Carts.GetCartWithItemsAsync(cartId, cancellationToken: cancellationToken);
        if (cart == null)
            return Result<CartDto>.Fail(ErrorCodes.CartNotFound, $"Cart {cartId} not found");

        var dto = await MapCartToDtoAsync(cart, cancellationToken);
        return Result<CartDto>.Ok(dto);
    }

    public async Task<Result<Unit>> ValidateCartAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        return await ValidateCartAsync(cartId, userId: null, isAdmin: true, cancellationToken);
    }

    public async Task<Result<Unit>> ValidateCartAsync(Guid cartId, Guid? userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var cart = await _unitOfWork.Carts.GetCartWithItemsAsync(cartId, cancellationToken: cancellationToken);
        if (cart == null)
            return Result<Unit>.Fail(ErrorCodes.CartNotFound, $"Cart {cartId} not found");

        if (!isAdmin && cart.UserId.HasValue && cart.UserId != userId)
            return Result<Unit>.Fail(ErrorCodes.Forbidden, "You do not have permission to validate this cart");

        // Batch query: Get all products for cart items in single query
        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await _unitOfWork.Products
            .FindByCondition(p => productIds.Contains(p.Id), trackChanges: false)
            .ToListAsync(cancellationToken);

        var productMap = products.ToDictionary(p => p.Id);

        foreach (var item in cart.Items)
        {
            if (!productMap.TryGetValue(item.ProductId, out var product))
                return Result<Unit>.Fail(ErrorCodes.ProductNotFound, $"Product {item.ProductId} not found");

            if (product.StockQuantity < item.Quantity)
                return Result<Unit>.Fail(ErrorCodes.InsufficientStock, $"Insufficient stock for {product.Name}. Available: {product.StockQuantity}, Required: {item.Quantity}");

            if (!product.IsActive)
                return Result<Unit>.Fail(ErrorCodes.ProductNotAvailable, $"Product {product.Name} is not available");
        }

        return Result<Unit>.Ok(Unit.Value);
    }

    private async Task<CartDto> MapCartToDtoAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        // Use AutoMapper to map cart and its items (CartRepository ensures Product is included)
        var dto = _mapper.Map<CartDto>(cart);

        dto.Subtotal = dto.Items.Sum(x => x.Total);
        dto.Total = dto.Subtotal; // Could add tax/shipping calculations here

        return dto;
    }
}

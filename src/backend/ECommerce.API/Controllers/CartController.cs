using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for shopping cart management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService cartService, ILogger<CartController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return userIdClaim?.Value != null ? Guid.Parse(userIdClaim.Value) : null;
    }

    private string? GetSessionId()
    {
        return Request.Cookies.TryGetValue("sessionId", out var sessionId) ? sessionId : null;
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetCart(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<CartDto>.Error("User not authenticated"));

        var cart = await _cartService.GetCartAsync(userId.Value, cancellationToken: cancellationToken);
        return Ok(ApiResponse<CartDto>.Ok(cart, "Cart retrieved successfully"));
    }

    [HttpPost("get-or-create")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetOrCreateCart(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        var cart = await _cartService.GetOrCreateCartAsync(userId, sessionId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<CartDto>.Ok(cart, "Cart retrieved or created successfully"));
    }

    [HttpPost("add-item")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CartDto>>> AddToCart([FromBody] AddToCartDto dto, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        var cart = await _cartService.AddToCartAsync(userId, sessionId, dto.ProductId, dto.Quantity, cancellationToken: cancellationToken);
        _logger.LogInformation("Item added to cart: ProductId={ProductId}, Quantity={Quantity}", dto.ProductId, dto.Quantity);
        return Ok(ApiResponse<CartDto>.Ok(cart, "Item added to cart successfully"));
    }

    [HttpPut("update-item/{cartItemId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CartDto>>> UpdateCartItem(Guid cartItemId, [FromBody] UpdateCartItemDto dto, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        var cart = await _cartService.UpdateCartItemAsync(userId, sessionId, cartItemId, dto.Quantity, cancellationToken: cancellationToken);
        _logger.LogInformation("Cart item updated: CartItemId={CartItemId}, Quantity={Quantity}", cartItemId, dto.Quantity);
        return Ok(ApiResponse<CartDto>.Ok(cart, "Cart item updated successfully"));
    }

    [HttpDelete("remove-item/{cartItemId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CartDto>>> RemoveFromCart(Guid cartItemId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        var cart = await _cartService.RemoveFromCartAsync(userId, sessionId, cartItemId, cancellationToken: cancellationToken);
        _logger.LogInformation("Item removed from cart: CartItemId={CartItemId}", cartItemId);
        return Ok(ApiResponse<CartDto>.Ok(cart, "Item removed from cart successfully"));
    }

    [HttpPost("clear")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CartDto>>> ClearCart(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        var cart = await _cartService.ClearCartAsync(userId, sessionId, cancellationToken: cancellationToken);
        _logger.LogInformation("Cart cleared");
        return Ok(ApiResponse<CartDto>.Ok(cart, "Cart cleared successfully"));
    }

    [HttpPost("validate/{cartId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> ValidateCart(Guid cartId, CancellationToken cancellationToken)
    {
        await _cartService.ValidateCartAsync(cartId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<object>.Ok(new object(), "Cart is valid"));
    }
}

using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Services;
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
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim?.Value != null ? Guid.Parse(userIdClaim.Value) : null;
    }

    private string? GetSessionId()
    {
        return Request.Cookies.TryGetValue("sessionId", out var sessionId) ? sessionId : null;
    }

    /// <summary>
    /// Retrieves the current user's shopping cart.
    /// </summary>
    /// <returns>The user's cart.</returns>
    /// <response code="200">Cart retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetCart()
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized(ApiResponse<CartDto>.Error("User not authenticated"));

            var cart = await _cartService.GetCartAsync(userId.Value);
            return Ok(ApiResponse<CartDto>.Ok(cart, "Cart retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cart not found for user");
            return Ok(ApiResponse<CartDto>.Ok(new CartDto(), "Cart is empty"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart");
            return StatusCode(500, ApiResponse<CartDto>.Error("An error occurred while retrieving the cart"));
        }
    }

    /// <summary>
    /// Gets or creates a cart (for both authenticated and anonymous users).
    /// </summary>
    /// <returns>The cart.</returns>
    /// <response code="200">Cart retrieved or created successfully.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("get-or-create")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetOrCreateCart()
    {
        try
        {
            var userId = GetUserId();
            var sessionId = GetSessionId();

            var cart = await _cartService.GetOrCreateCartAsync(userId, sessionId);
            return Ok(ApiResponse<CartDto>.Ok(cart, "Cart retrieved or created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating cart");
            return StatusCode(500, ApiResponse<CartDto>.Error("An error occurred while getting or creating the cart"));
        }
    }

    /// <summary>
    /// Adds an item to the cart.
    /// </summary>
    /// <param name="dto">The item to add.</param>
    /// <returns>The updated cart.</returns>
    /// <response code="200">Item added to cart successfully.</response>
    /// <response code="400">Invalid request or insufficient stock.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("add-item")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CartDto>>> AddToCart([FromBody] AddToCartDto dto)
    {
        try
        {
            if (dto.Quantity <= 0)
                return BadRequest(ApiResponse<CartDto>.Error("Quantity must be greater than 0"));

            var userId = GetUserId();
            var sessionId = GetSessionId();

            var cart = await _cartService.AddToCartAsync(userId, sessionId, dto.ProductId, dto.Quantity);
            return Ok(ApiResponse<CartDto>.Ok(cart, "Item added to cart successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error adding item to cart: {Message}", ex.Message);
            return BadRequest(ApiResponse<CartDto>.Error(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument: {Message}", ex.Message);
            return BadRequest(ApiResponse<CartDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart");
            return StatusCode(500, ApiResponse<CartDto>.Error("An error occurred while adding the item to cart"));
        }
    }

    /// <summary>
    /// Updates the quantity of an item in the cart.
    /// </summary>
    /// <param name="cartItemId">The cart item ID.</param>
    /// <param name="dto">The updated quantity.</param>
    /// <returns>The updated cart.</returns>
    /// <response code="200">Cart item updated successfully.</response>
    /// <response code="400">Invalid request or insufficient stock.</response>
    /// <response code="404">Cart item not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPut("update-item/{cartItemId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CartDto>>> UpdateCartItem(Guid cartItemId, [FromBody] UpdateCartItemDto dto)
    {
        try
        {
            if (dto.Quantity < 0)
                return BadRequest(ApiResponse<CartDto>.Error("Quantity cannot be negative"));

            var userId = GetUserId();
            var sessionId = GetSessionId();

            var cart = await _cartService.UpdateCartItemAsync(userId, sessionId, cartItemId, dto.Quantity);
            return Ok(ApiResponse<CartDto>.Ok(cart, "Cart item updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cart item not found or error updating: {Message}", ex.Message);
            return NotFound(ApiResponse<CartDto>.Error(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument: {Message}", ex.Message);
            return BadRequest(ApiResponse<CartDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item");
            return StatusCode(500, ApiResponse<CartDto>.Error("An error occurred while updating the cart item"));
        }
    }

    /// <summary>
    /// Removes an item from the cart.
    /// </summary>
    /// <param name="cartItemId">The cart item ID to remove.</param>
    /// <returns>The updated cart.</returns>
    /// <response code="200">Item removed from cart successfully.</response>
    /// <response code="404">Cart item not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpDelete("remove-item/{cartItemId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CartDto>>> RemoveFromCart(Guid cartItemId)
    {
        try
        {
            var userId = GetUserId();
            var sessionId = GetSessionId();

            var cart = await _cartService.RemoveFromCartAsync(userId, sessionId, cartItemId);
            return Ok(ApiResponse<CartDto>.Ok(cart, "Item removed from cart successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cart item not found: {Message}", ex.Message);
            return NotFound(ApiResponse<CartDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from cart");
            return StatusCode(500, ApiResponse<CartDto>.Error("An error occurred while removing the item from cart"));
        }
    }

    /// <summary>
    /// Clears all items from the cart.
    /// </summary>
    /// <returns>The cleared cart.</returns>
    /// <response code="200">Cart cleared successfully.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("clear")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CartDto>>> ClearCart()
    {
        try
        {
            var userId = GetUserId();
            var sessionId = GetSessionId();

            var cart = await _cartService.ClearCartAsync(userId, sessionId);
            return Ok(ApiResponse<CartDto>.Ok(cart, "Cart cleared successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return StatusCode(500, ApiResponse<CartDto>.Error("An error occurred while clearing the cart"));
        }
    }

    /// <summary>
    /// Validates all items in the cart are still available and in stock.
    /// </summary>
    /// <param name="cartId">The cart ID.</param>
    /// <returns>Validation result.</returns>
    /// <response code="200">Cart is valid.</response>
    /// <response code="400">Cart has validation errors.</response>
    /// <response code="404">Cart not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("validate/{cartId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> ValidateCart(Guid cartId)
    {
        try
        {
            await _cartService.ValidateCartAsync(cartId);
            return Ok(ApiResponse<bool>.Ok(true, "Cart is valid"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cart validation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<bool>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating cart");
            return StatusCode(500, ApiResponse<bool>.Error("An error occurred while validating the cart"));
        }
    }
}

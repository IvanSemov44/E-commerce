using ECommerce.API.ActionFilters;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService cartService, ICurrentUserService currentUser, ILogger<CartController> logger)
    {
        _cartService = cartService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the authenticated user's shopping cart.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's shopping cart with all items and totals.</returns>
    /// <response code="200">Cart retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Cart not found.</response>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetCart(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<CartDto>.Error("User not authenticated"));

        var cart = await _cartService.GetCartAsync(userId.Value, cancellationToken: cancellationToken);
        return Ok(ApiResponse<CartDto>.Ok(cart, "Cart retrieved successfully"));
    }

    /// <summary>
    /// Retrieves the cart for the current user or session, creating a new cart if one doesn't exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's or session's shopping cart.</returns>
    /// <response code="200">Cart retrieved or created successfully.</response>
    [HttpPost("get-or-create")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetOrCreateCart(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        var sessionId = _currentUser.SessionId;

        var cart = await _cartService.GetOrCreateCartAsync(userId, sessionId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<CartDto>.Ok(cart, "Cart retrieved or created successfully"));
    }

    /// <summary>
    /// Adds a product to the shopping cart or increments its quantity if already present.
    /// </summary>
    /// <param name="dto">The product and quantity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated shopping cart.</returns>
    /// <response code="200">Item added to cart successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="404">Product not found or insufficient stock.</response>
    [HttpPost("add-item")]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CartDto>>> AddToCart([FromBody] AddToCartDto dto, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        var sessionId = _currentUser.SessionId;

        var cart = await _cartService.AddToCartAsync(userId, sessionId, dto.ProductId, dto.Quantity, cancellationToken: cancellationToken);
        _logger.LogInformation("Item added to cart: ProductId={ProductId}, Quantity={Quantity}", dto.ProductId, dto.Quantity);
        return Ok(ApiResponse<CartDto>.Ok(cart, "Item added to cart successfully"));
    }

    /// <summary>
    /// Updates the quantity of a specific item in the shopping cart.
    /// </summary>
    /// <param name="cartItemId">The cart item ID.</param>
    /// <param name="dto">The updated quantity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated shopping cart.</returns>
    /// <response code="200">Cart item updated successfully.</response>
    /// <response code="400">Invalid quantity or insufficient stock.</response>
    /// <response code="404">Cart item not found.</response>
    [HttpPut("update-item/{cartItemId:guid}")]
    [HttpPut("items/{cartItemId:guid}")]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CartDto>>> UpdateCartItem(Guid cartItemId, [FromBody] UpdateCartItemDto dto, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        var sessionId = _currentUser.SessionId;

        var cart = await _cartService.UpdateCartItemAsync(userId, sessionId, cartItemId, dto.Quantity, cancellationToken: cancellationToken);
        _logger.LogInformation("Cart item updated: CartItemId={CartItemId}, Quantity={Quantity}", cartItemId, dto.Quantity);
        return Ok(ApiResponse<CartDto>.Ok(cart, "Cart item updated successfully"));
    }

    /// <summary>
    /// Removes a specific item from the shopping cart.
    /// </summary>
    /// <param name="cartItemId">The cart item ID to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated shopping cart.</returns>
    /// <response code="200">Item removed from cart successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">Cart item not found.</response>
    [HttpDelete("remove-item/{cartItemId:guid}")]
    [HttpDelete("items/{cartItemId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CartDto>>> RemoveFromCart(Guid cartItemId, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        var sessionId = _currentUser.SessionId;

        var cart = await _cartService.RemoveFromCartAsync(userId, sessionId, cartItemId, cancellationToken: cancellationToken);
        _logger.LogInformation("Item removed from cart: CartItemId={CartItemId}", cartItemId);
        return Ok(ApiResponse<CartDto>.Ok(cart, "Item removed from cart successfully"));
    }

    /// <summary>
    /// Removes all items from the shopping cart.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The emptied shopping cart.</returns>
    /// <response code="200">Cart cleared successfully.</response>
    [HttpPost("clear")]
    [HttpDelete]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CartDto>>> ClearCart(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        var sessionId = _currentUser.SessionId;

        var cart = await _cartService.ClearCartAsync(userId, sessionId, cancellationToken: cancellationToken);
        _logger.LogInformation("Cart cleared");
        return Ok(ApiResponse<CartDto>.Ok(cart, "Cart cleared successfully"));
    }

    /// <summary>
    /// Validates a cart to ensure all items are available and stock is sufficient for checkout.
    /// </summary>
    /// <param name="cartId">The cart ID to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    /// <response code="200">Cart is valid and ready for checkout.</response>
    /// <response code="400">Cart validation failed due to stock issues or unavailable items.</response>
    /// <response code="404">Cart not found.</response>
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

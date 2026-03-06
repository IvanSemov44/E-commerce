using ECommerce.API.ActionFilters;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using ECommerce.Core.Results;
using ECommerce.Core.Constants;
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
    /// Retrieves the authenticated user's shopping cart, creating an empty cart if one doesn't exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's shopping cart with all items and totals.</returns>
    /// <response code="200">Cart retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetCart(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<CartDto>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        var result = await _cartService.GetOrCreateCartAsync(userId, sessionId: null, cancellationToken: cancellationToken);
        
        if (result is Result<CartDto>.Success success)
        {
            return Ok(ApiResponse<CartDto>.Ok(success.Data, "Cart retrieved successfully"));
        }
        
        if (result is Result<CartDto>.Failure failure)
        {
            return MapFailureToResponse<CartDto>(failure);
        }

        return BadRequest(ApiResponse<CartDto>.Failure("Unknown error occurred", "UNKNOWN_ERROR"));
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetOrCreateCart(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        var sessionId = _currentUser.SessionId;

        var result = await _cartService.GetOrCreateCartAsync(userId, sessionId, cancellationToken: cancellationToken);
        
        if (result is Result<CartDto>.Success success)
        {
            return Ok(ApiResponse<CartDto>.Ok(success.Data, "Cart retrieved or created successfully"));
        }
        
        if (result is Result<CartDto>.Failure failure)
        {
            return MapFailureToResponse<CartDto>(failure);
        }

        return BadRequest(ApiResponse<CartDto>.Failure("Unknown error occurred", "UNKNOWN_ERROR"));
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CartDto>>> AddToCart([FromBody] AddToCartDto dto, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        var sessionId = _currentUser.SessionId;

        var result = await _cartService.AddToCartAsync(userId, sessionId, dto.ProductId, dto.Quantity, cancellationToken: cancellationToken);
        
        if (result is Result<CartDto>.Success success)
        {
            _logger.LogInformation("Item added to cart: ProductId={ProductId}, Quantity={Quantity}", dto.ProductId, dto.Quantity);
            return Ok(ApiResponse<CartDto>.Ok(success.Data, "Item added to cart successfully"));
        }
        
        if (result is Result<CartDto>.Failure failure)
        {
            return MapFailureToResponse<CartDto>(failure);
        }

        return BadRequest(ApiResponse<CartDto>.Failure("Unknown error occurred", "UNKNOWN_ERROR"));
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CartDto>>> UpdateCartItem(Guid cartItemId, [FromBody] UpdateCartItemDto dto, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        var sessionId = _currentUser.SessionId;

        var result = await _cartService.UpdateCartItemAsync(userId, sessionId, cartItemId, dto.Quantity, cancellationToken: cancellationToken);
        
        if (result is Result<CartDto>.Success success)
        {
            _logger.LogInformation("Cart item updated: CartItemId={CartItemId}, Quantity={Quantity}", cartItemId, dto.Quantity);
            return Ok(ApiResponse<CartDto>.Ok(success.Data, "Cart item updated successfully"));
        }
        
        if (result is Result<CartDto>.Failure failure)
        {
            return MapFailureToResponse<CartDto>(failure);
        }

        return BadRequest(ApiResponse<CartDto>.Failure("Unknown error occurred", "UNKNOWN_ERROR"));
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CartDto>>> RemoveFromCart(Guid cartItemId, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        var sessionId = _currentUser.SessionId;

        var result = await _cartService.RemoveFromCartAsync(userId, sessionId, cartItemId, cancellationToken: cancellationToken);
        
        if (result is Result<CartDto>.Success success)
        {
            _logger.LogInformation("Item removed from cart: CartItemId={CartItemId}", cartItemId);
            return Ok(ApiResponse<CartDto>.Ok(success.Data, "Item removed from cart successfully"));
        }
        
        if (result is Result<CartDto>.Failure failure)
        {
            return MapFailureToResponse<CartDto>(failure);
        }

        return BadRequest(ApiResponse<CartDto>.Failure("Unknown error occurred", "UNKNOWN_ERROR"));
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CartDto>>> ClearCart(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        var sessionId = _currentUser.SessionId;

        var result = await _cartService.ClearCartAsync(userId, sessionId, cancellationToken: cancellationToken);
        
        if (result is Result<CartDto>.Success success)
        {
            _logger.LogInformation("Cart cleared");
            return Ok(ApiResponse<CartDto>.Ok(success.Data, "Cart cleared successfully"));
        }
        
        if (result is Result<CartDto>.Failure failure)
        {
            return MapFailureToResponse<CartDto>(failure);
        }

        return BadRequest(ApiResponse<CartDto>.Failure("Unknown error occurred", "UNKNOWN_ERROR"));
    }

    /// <summary>
    /// Validates a cart to ensure all items are available and stock is sufficient for checkout.
    /// </summary>
    /// <param name="cartId">The cart ID to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    /// <response code="200">Cart is valid and ready for checkout.</response>
    /// <response code="400">Cart validation failed due to stock issues or unavailable items.</response>
    /// <response code="403">User does not have permission to validate this cart.</response>
    /// <response code="404">Cart not found.</response>
    [HttpPost("validate/{cartId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> ValidateCart(Guid cartId, CancellationToken cancellationToken)
    {
        var cartResult = await _cartService.GetCartByIdAsync(cartId, cancellationToken: cancellationToken);
        
        if (cartResult is not Result<CartDto>.Success cartSuccess)
        {
            if (cartResult is Result<CartDto>.Failure cartFailure)
            {
                return MapCartFailureToResponse(cartFailure);
            }
            return BadRequest(ApiResponse<object>.Failure("Unknown error occurred", "UNKNOWN_ERROR"));
        }

        var cart = cartSuccess.Data;

        // Check ownership: if cart belongs to a user, verify the current user owns it or is admin
        if (cart.UserId.HasValue)
        {
            var currentUserId = _currentUser.UserIdOrNull;
            var isAdmin = _currentUser.IsAuthenticated &&
                         (_currentUser.Role == Core.Enums.UserRole.Admin || _currentUser.Role == Core.Enums.UserRole.SuperAdmin);

            if (!isAdmin && cart.UserId != currentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to validate cart {CartId} belonging to {CartOwnerId}",
                    currentUserId, cartId, cart.UserId);
                return StatusCode(403, ApiResponse<object>.Failure("You do not have permission to validate this cart", "INSUFFICIENT_PERMISSIONS"));
            }
        }

        var validateResult = await _cartService.ValidateCartAsync(cartId, cancellationToken: cancellationToken);
        
        if (validateResult is Result<Unit>.Success)
        {
            return Ok(ApiResponse<object>.Ok(new object(), "Cart is valid"));
        }
        
        if (validateResult is Result<Unit>.Failure validateFailure)
        {
            return MapValidateFailureToResponse(validateFailure);
        }

        return BadRequest(ApiResponse<object>.Failure("Unknown error occurred", "UNKNOWN_ERROR"));
    }

    /// <summary>
    /// Maps failure results to appropriate HTTP responses for CartDto operations.
    /// </summary>
    private ActionResult<ApiResponse<T>> MapFailureToResponse<T>(Result<T>.Failure failure)
    {
        return failure.Code switch
        {
            ErrorCodes.CartNotFound => NotFound(ApiResponse<T>.Failure(failure.Message, failure.Code)),
            ErrorCodes.CartItemNotFound => NotFound(ApiResponse<T>.Failure(failure.Message, failure.Code)),
            ErrorCodes.ProductNotFound => NotFound(ApiResponse<T>.Failure(failure.Message, failure.Code)),
            ErrorCodes.ProductNotAvailable => BadRequest(ApiResponse<T>.Failure(failure.Message, failure.Code)),
            ErrorCodes.InsufficientStock => BadRequest(ApiResponse<T>.Failure(failure.Message, failure.Code)),
            ErrorCodes.InvalidQuantity => BadRequest(ApiResponse<T>.Failure(failure.Message, failure.Code)),
            _ => BadRequest(ApiResponse<T>.Failure(failure.Message, failure.Code))
        };
    }

    /// <summary>
    /// Maps failure results to appropriate HTTP responses for CartDto failures.
    /// </summary>
    private ActionResult<ApiResponse<object>> MapCartFailureToResponse(Result<CartDto>.Failure failure)
    {
        return failure.Code switch
        {
            ErrorCodes.CartNotFound => NotFound(ApiResponse<object>.Failure(failure.Message, failure.Code)),
            ErrorCodes.CartItemNotFound => NotFound(ApiResponse<object>.Failure(failure.Message, failure.Code)),
            ErrorCodes.ProductNotFound => NotFound(ApiResponse<object>.Failure(failure.Message, failure.Code)),
            ErrorCodes.ProductNotAvailable => BadRequest(ApiResponse<object>.Failure(failure.Message, failure.Code)),
            ErrorCodes.InsufficientStock => BadRequest(ApiResponse<object>.Failure(failure.Message, failure.Code)),
            ErrorCodes.InvalidQuantity => BadRequest(ApiResponse<object>.Failure(failure.Message, failure.Code)),
            _ => BadRequest(ApiResponse<object>.Failure(failure.Message, failure.Code))
        };
    }

    /// <summary>
    /// Maps failure results to appropriate HTTP responses for validation failures.
    /// </summary>
    private ActionResult<ApiResponse<object>> MapValidateFailureToResponse(Result<Unit>.Failure failure)
    {
        return failure.Code switch
        {
            ErrorCodes.CartNotFound => NotFound(ApiResponse<object>.Failure(failure.Message, failure.Code)),
            ErrorCodes.ProductNotFound => NotFound(ApiResponse<object>.Failure(failure.Message, failure.Code)),
            ErrorCodes.ProductNotAvailable => BadRequest(ApiResponse<object>.Failure(failure.Message, failure.Code)),
            ErrorCodes.InsufficientStock => BadRequest(ApiResponse<object>.Failure(failure.Message, failure.Code)),
            _ => BadRequest(ApiResponse<object>.Failure(failure.Message, failure.Code))
        };
    }
}


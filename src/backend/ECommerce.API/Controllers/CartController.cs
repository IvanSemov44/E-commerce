using ECommerce.API.ActionFilters;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Interfaces;
using ECommerce.Shopping.Application.Commands.AddToCart;
using ECommerce.Shopping.Application.Commands.UpdateCartItemQuantity;
using ECommerce.Shopping.Application.Commands.RemoveFromCart;
using ECommerce.Shopping.Application.Commands.ClearCart;
using ECommerce.Shopping.Application.Queries.GetCart;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for shopping cart management operations. Uses MediatR CQRS pattern.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Cart")]
public class CartController(IMediator mediator, ICurrentUserService currentUser, ILogger<CartController> logger) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<CartController> _logger = logger;

    /// <summary>
    /// Retrieves the authenticated user's shopping cart, creating an empty cart if one doesn't exist.
    /// </summary>
    /// <param name="cancellationToken"></param>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetCart(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<CartDto>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        var result = await _mediator.Send(new GetCartQuery(userId, null), cancellationToken);
        if (!result.IsSuccess)
            return MapShoppingError<CartDto>(result.GetErrorOrThrow());

        return Ok(ApiResponse<CartDto>.Ok(MapShoppingCartDtoToApiDto(result.GetDataOrThrow()), "Cart retrieved successfully"));
    }

    /// <summary>
    /// Retrieves the cart for the current user or session, creating a new cart if one doesn't exist.
    /// </summary>
    [HttpPost("get-or-create")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetOrCreateCart(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        var sessionId = _currentUser.SessionId;

        var result = await _mediator.Send(new GetCartQuery(userId, sessionId), cancellationToken);
        if (!result.IsSuccess)
            return MapShoppingError<CartDto>(result.GetErrorOrThrow());

        return Ok(ApiResponse<CartDto>.Ok(MapShoppingCartDtoToApiDto(result.GetDataOrThrow()), "Cart retrieved or created successfully"));
    }

    /// <summary>
    /// Adds a product to the shopping cart or increments its quantity if already present.
    /// </summary>
    [HttpPost("add-item")]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CartDto>>> AddToCart([FromBody] AddToCartDto dto, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        var sessionId = _currentUser.SessionId;

        var result = await _mediator.Send(
            new AddToCartCommand(userId, sessionId, dto.ProductId, dto.Quantity),
            cancellationToken);

        if (!result.IsSuccess)
            return MapShoppingError<CartDto>(result.GetErrorOrThrow());

        _logger.LogInformation("Item added to cart: ProductId={ProductId}, Quantity={Quantity}", dto.ProductId, dto.Quantity);
        return Ok(ApiResponse<CartDto>.Ok(MapShoppingCartDtoToApiDto(result.GetDataOrThrow()), "Item added to cart successfully"));
    }

    /// <summary>
    /// Updates the quantity of a specific item in the shopping cart.
    /// </summary>
    [HttpPut("update-item/{cartItemId:guid}")]
    [HttpPut("items/{cartItemId:guid}")]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CartDto>>> UpdateCartItem(Guid cartItemId, [FromBody] UpdateCartItemDto dto, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        var sessionId = _currentUser.SessionId;

        var result = await _mediator.Send(
            new UpdateCartItemQuantityCommand(userId, sessionId, cartItemId, dto.Quantity),
            cancellationToken);

        if (!result.IsSuccess)
            return MapShoppingError<CartDto>(result.GetErrorOrThrow());

        _logger.LogInformation("Cart item updated: CartItemId={CartItemId}, Quantity={Quantity}", cartItemId, dto.Quantity);
        return Ok(ApiResponse<CartDto>.Ok(MapShoppingCartDtoToApiDto(result.GetDataOrThrow()), "Cart item updated successfully"));
    }

    /// <summary>
    /// Removes a specific item from the shopping cart.
    /// </summary>
    [HttpDelete("remove-item/{cartItemId:guid}")]
    [HttpDelete("items/{cartItemId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CartDto>>> RemoveFromCart(Guid cartItemId, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        var sessionId = _currentUser.SessionId;

        var result = await _mediator.Send(
            new RemoveFromCartCommand(userId, sessionId, cartItemId),
            cancellationToken);

        if (!result.IsSuccess)
            return MapShoppingError<CartDto>(result.GetErrorOrThrow());

        _logger.LogInformation("Item removed from cart: CartItemId={CartItemId}", cartItemId);
        return Ok(ApiResponse<CartDto>.Ok(MapShoppingCartDtoToApiDto(result.GetDataOrThrow()), "Item removed from cart successfully"));
    }

    /// <summary>
    /// Removes all items from the shopping cart.
    /// </summary>
    [HttpPost("clear")]
    [HttpDelete]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CartDto>>> ClearCart(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        var sessionId = _currentUser.SessionId;

        var result = await _mediator.Send(
            new ClearCartCommand(userId, sessionId),
            cancellationToken);

        if (!result.IsSuccess)
            return MapShoppingError<CartDto>(result.GetErrorOrThrow());

        _logger.LogInformation("Cart cleared");
        return Ok(ApiResponse<CartDto>.Ok(MapShoppingCartDtoToApiDto(result.GetDataOrThrow()), "Cart cleared successfully"));
    }

    /// <summary>
    /// Maps Shopping domain errors to HTTP responses.
    /// </summary>
    private ActionResult<ApiResponse<T>> MapShoppingError<T>(DomainError error) => error.Code switch
    {
        "CART_NOT_FOUND" or "CART_ITEM_NOT_FOUND" or "PRODUCT_NOT_FOUND"
            => NotFound(ApiResponse<T>.Failure(error.Message, error.Code)),
        "QUANTITY_INVALID" or "PRODUCT_NOT_AVAILABLE" or "INSUFFICIENT_STOCK" or "VALIDATION_FAILED"
            => BadRequest(ApiResponse<T>.Failure(error.Message, error.Code)),
        "FORBIDDEN"
            => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<T>.Failure(error.Message, error.Code)),
        _ => StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<T>.Failure(error.Message, error.Code))
    };

    /// <summary>
    /// Converts Shopping application CartDto to API CartDto.
    /// </summary>
    private static CartDto MapShoppingCartDtoToApiDto(ECommerce.Shopping.Application.DTOs.CartDto shoppingCart)
    {
        return new CartDto
        {
            Id = shoppingCart.Id,
            UserId = shoppingCart.UserId == Guid.Empty ? null : shoppingCart.UserId,
            Items = shoppingCart.Items.Select(i => new CartItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = "", // Shopping DTO doesn't provide product name
                Quantity = i.Quantity,
                Price = i.UnitPrice,
                Total = i.LineTotal
            }).ToList(),
            Subtotal = shoppingCart.Subtotal,
            Total = shoppingCart.Subtotal // Shopping DTO only has Subtotal
        };
    }
}

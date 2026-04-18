using ECommerce.API.ActionFilters;
using ECommerce.Contracts.DTOs.Common;
using ECommerce.Contracts.DTOs.Cart;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.Commands.AddToCart;
using ECommerce.Shopping.Application.Commands.UpdateCartItemQuantity;
using ECommerce.Shopping.Application.Commands.RemoveFromCart;
using ECommerce.Shopping.Application.Commands.ClearCart;
using ECommerce.Shopping.Application.Queries.GetCart;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Features.Shopping.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Cart")]
public class CartController(IMediator _mediator, ICurrentUserService _currentUser, ILogger<CartController> _logger) : ControllerBase
{
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<CartDto>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        var result = await _mediator.Send(new GetCartQuery(userId, null), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.GetErrorOrThrow());

        return Ok(ApiResponse<CartDto>.Ok(MapToApiDto(result.GetDataOrThrow()), "Cart retrieved successfully"));
    }

    [HttpPost("get-or-create")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrCreateCart(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCartQuery(_currentUser.UserIdOrNull, _currentUser.SessionId), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.GetErrorOrThrow());

        return Ok(ApiResponse<CartDto>.Ok(MapToApiDto(result.GetDataOrThrow()), "Cart retrieved successfully"));
    }

    [HttpPost("add-item")]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddToCartCommand(_currentUser.UserIdOrNull, _currentUser.SessionId, dto.ProductId, dto.Quantity),
            cancellationToken);

        if (!result.IsSuccess)
            return MapError(result.GetErrorOrThrow());

        _logger.LogInformation("Item added to cart: ProductId={ProductId}, Quantity={Quantity}", dto.ProductId, dto.Quantity);
        return NoContent();
    }

    [HttpPut("items/{cartItemId:guid}")]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCartItem(Guid cartItemId, [FromBody] UpdateCartItemDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateCartItemQuantityCommand(_currentUser.UserIdOrNull, _currentUser.SessionId, cartItemId, dto.Quantity),
            cancellationToken);

        if (!result.IsSuccess)
            return MapError(result.GetErrorOrThrow());

        _logger.LogInformation("Cart item updated: CartItemId={CartItemId}, Quantity={Quantity}", cartItemId, dto.Quantity);
        return NoContent();
    }

    [HttpDelete("items/{cartItemId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromCart(Guid cartItemId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RemoveFromCartCommand(_currentUser.UserIdOrNull, _currentUser.SessionId, cartItemId),
            cancellationToken);

        if (!result.IsSuccess)
            return MapError(result.GetErrorOrThrow());

        _logger.LogInformation("Item removed from cart: CartItemId={CartItemId}", cartItemId);
        return NoContent();
    }

    [HttpDelete]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        await _mediator.Send(new ClearCartCommand(_currentUser.UserIdOrNull, _currentUser.SessionId), cancellationToken);
        return NoContent();
    }

    private ObjectResult MapError(DomainError error) => error.Code switch
    {
        "CART_NOT_FOUND" or "CART_ITEM_NOT_FOUND" or "PRODUCT_NOT_FOUND"
            => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),
        "QUANTITY_INVALID" or "PRODUCT_NOT_AVAILABLE" or "INSUFFICIENT_STOCK" or "VALIDATION_FAILED"
            => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),
        "FORBIDDEN"
            => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Failure(error.Message, error.Code)),
        _ => StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<object>.Failure(error.Message, error.Code))
    };

    private static CartDto MapToApiDto(ECommerce.Shopping.Application.DTOs.CartDto cart) => new()
    {
        Id = cart.Id,
        UserId = cart.UserId == Guid.Empty ? null : cart.UserId,
        Items = cart.Items.Select(i => new CartItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = "",
            Quantity = i.Quantity,
            Price = i.UnitPrice,
            Total = i.LineTotal
        }).ToList(),
        Subtotal = cart.Subtotal,
        Total = cart.Subtotal
    };
}

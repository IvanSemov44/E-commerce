using ECommerce.API.ActionFilters;
using ECommerce.API.Shared.Extensions;
using ECommerce.Contracts.DTOs.Common;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.Commands.AddToCart;
using ECommerce.Shopping.Application.Commands.UpdateCartItemQuantity;
using ECommerce.Shopping.Application.Commands.RemoveFromCart;
using ECommerce.Shopping.Application.Commands.ClearCart;
using ECommerce.Shopping.Application.Queries.GetCart;
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
        return result.ToActionResult(data =>
            data.Id == Guid.Empty
                ? NotFound(ApiResponse<object>.Failure("Cart not found.", "CART_NOT_FOUND"))
                : Ok(ApiResponse<CartDto>.Ok(data, "Cart retrieved successfully")));
    }

    [HttpPost("get-or-create")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrCreateCart(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCartQuery(_currentUser.UserIdOrNull, _currentUser.SessionId), cancellationToken);
        return result.ToActionResult(
            data => Ok(ApiResponse<CartDto>.Ok(data, "Cart retrieved successfully")));
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

        return result.ToActionResult(() =>
        {
            _logger.LogInformation("Item added to cart: ProductId={ProductId}, Quantity={Quantity}", dto.ProductId, dto.Quantity);
            return NoContent();
        });
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

        return result.ToActionResult(() =>
        {
            _logger.LogInformation("Cart item updated: CartItemId={CartItemId}, Quantity={Quantity}", cartItemId, dto.Quantity);
            return NoContent();
        });
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

        return result.ToActionResult(() =>
        {
            _logger.LogInformation("Item removed from cart: CartItemId={CartItemId}", cartItemId);
            return NoContent();
        });
    }

    [HttpDelete]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        await _mediator.Send(new ClearCartCommand(_currentUser.UserIdOrNull, _currentUser.SessionId), cancellationToken);
        return NoContent();
    }
}

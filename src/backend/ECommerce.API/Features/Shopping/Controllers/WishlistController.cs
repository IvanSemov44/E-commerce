using ECommerce.API.ActionFilters;
using ECommerce.Contracts.DTOs.Common;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Shopping.Application.Commands.AddToWishlist;
using ECommerce.Shopping.Application.Commands.ClearWishlist;
using ECommerce.Shopping.Application.Commands.RemoveFromWishlist;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Queries.GetWishlist;
using ECommerce.Shopping.Application.Queries.IsProductInWishlist;

namespace ECommerce.API.Features.Shopping.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Wishlist")]
[Authorize]
public class WishlistController(IMediator mediator, ICurrentUserService currentUser, ILogger<WishlistController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetWishlist(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<WishlistDto>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        var result = await mediator.Send(new GetWishlistQuery(userId.Value), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<WishlistDto>.Failure(result.GetErrorOrThrow().Message, result.GetErrorOrThrow().Code));

        return Ok(ApiResponse<WishlistDto>.Ok(result.GetDataOrThrow(), "Wishlist retrieved successfully"));
    }

    [HttpPost("add")]
    [ValidationFilter]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistDto dto, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        var result = await mediator.Send(new AddToWishlistCommand(userId.Value, dto.ProductId), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Failure(result.GetErrorOrThrow().Message, result.GetErrorOrThrow().Code));

        logger.LogInformation("Product {ProductId} added to wishlist for user {UserId}", dto.ProductId, userId.Value);
        return NoContent();
    }

    [HttpDelete("remove/{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFromWishlist(Guid productId, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        await mediator.Send(new RemoveFromWishlistCommand(userId.Value, productId), cancellationToken);
        logger.LogInformation("Product {ProductId} removed from wishlist for user {UserId}", productId, userId.Value);
        return NoContent();
    }

    [HttpGet("contains/{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IsProductInWishlist(Guid productId, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<bool>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        var result = await mediator.Send(new IsProductInWishlistQuery(userId.Value, productId), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<bool>.Failure(result.GetErrorOrThrow().Message, result.GetErrorOrThrow().Code));

        return Ok(ApiResponse<bool>.Ok(result.GetDataOrThrow(), "Check completed successfully"));
    }

    [HttpPost("clear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClearWishlist(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        await mediator.Send(new ClearWishlistCommand(userId.Value), cancellationToken);
        logger.LogInformation("Wishlist cleared for user {UserId}", userId.Value);
        return NoContent();
    }
}

using ECommerce.API.ActionFilters;
using ECommerce.API.Features.Wishlist;
using ECommerce.Application.DTOs.Wishlist;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using ECommerce.Core.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for managing user wishlists.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Wishlist")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<WishlistController> _logger;

    public WishlistController(IMediator mediator, ICurrentUserService currentUser, ILogger<WishlistController> logger)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the authenticated user's wishlist with all saved products.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's wishlist.</returns>
    /// <response code="200">Wishlist retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Wishlist not found.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWishlist(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<WishlistDto>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        _logger.LogInformation("Retrieving wishlist for user {UserId}", userId.Value);

        var result = await _mediator.Send(new GetWishlistQuery(userId.Value), cancellationToken);
        return result is Result<WishlistDto>.Success success
            ? Ok(ApiResponse<WishlistDto>.Ok(success.Data, "Wishlist retrieved successfully"))
            : result is Result<WishlistDto>.Failure failure
                ? BadRequest(ApiResponse<WishlistDto>.Failure(failure.Message, failure.Code))
                : BadRequest(ApiResponse<WishlistDto>.Failure("An error occurred", "UNKNOWN_ERROR"));
    }

    /// <summary>
    /// Adds a product to the user's wishlist for future reference.
    /// </summary>
    /// <param name="dto">The product to add to the wishlist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated wishlist.</returns>
    /// <response code="200">Product added to wishlist successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="409">Product is already in the wishlist.</response>
    [HttpPost("add")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistDto dto, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<WishlistDto>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        _logger.LogInformation("Adding product {ProductId} to wishlist for user {UserId}", dto.ProductId, userId.Value);

        var result = await _mediator.Send(new AddToWishlistCommand(userId.Value, dto.ProductId), cancellationToken);
        return result is Result<WishlistDto>.Success success
            ? Ok(ApiResponse<WishlistDto>.Ok(success.Data, "Product added to wishlist successfully"))
            : result is Result<WishlistDto>.Failure failure
                ? BadRequest(ApiResponse<WishlistDto>.Failure(failure.Message, failure.Code))
                : BadRequest(ApiResponse<WishlistDto>.Failure("An error occurred", "UNKNOWN_ERROR"));
    }

    /// <summary>
    /// Removes a product from the user's wishlist.
    /// </summary>
    /// <param name="productId">The product ID to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated wishlist.</returns>
    /// <response code="200">Product removed from wishlist successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Product not found in wishlist.</response>
    [HttpDelete("remove/{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromWishlist(Guid productId, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<WishlistDto>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        _logger.LogInformation("Removing product {ProductId} from wishlist for user {UserId}", productId, userId.Value);

        var result = await _mediator.Send(new RemoveFromWishlistCommand(userId.Value, productId), cancellationToken);
        return result is Result<WishlistDto>.Success success
            ? Ok(ApiResponse<WishlistDto>.Ok(success.Data, "Product removed from wishlist successfully"))
            : result is Result<WishlistDto>.Failure failure
                ? BadRequest(ApiResponse<WishlistDto>.Failure(failure.Message, failure.Code))
                : BadRequest(ApiResponse<WishlistDto>.Failure("An error occurred", "UNKNOWN_ERROR"));
    }

    /// <summary>
    /// Checks if a specific product is in the user's wishlist.
    /// </summary>
    /// <param name="productId">The product ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the product is in the wishlist, false otherwise.</returns>
    /// <response code="200">Check completed successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("contains/{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IsProductInWishlist(Guid productId, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<bool>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        _logger.LogInformation("Checking if product {ProductId} is in wishlist for user {UserId}", productId, userId.Value);

        var isInWishlist = await _mediator.Send(new IsProductInWishlistQuery(userId.Value, productId), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(isInWishlist, "Check completed successfully"));
    }

    /// <summary>
    /// Removes all products from the user's wishlist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The emptied wishlist.</returns>
    /// <response code="200">Wishlist cleared successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Wishlist not found.</response>
    [HttpPost("clear")]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearWishlist(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<WishlistDto>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        _logger.LogInformation("Clearing wishlist for user {UserId}", userId.Value);

        var result = await _mediator.Send(new ClearWishlistCommand(userId.Value), cancellationToken);
        return result is Result<WishlistDto>.Success success
            ? Ok(ApiResponse<WishlistDto>.Ok(success.Data, "Wishlist cleared successfully"))
            : result is Result<WishlistDto>.Failure failure
                ? BadRequest(ApiResponse<WishlistDto>.Failure(failure.Message, failure.Code))
                : BadRequest(ApiResponse<WishlistDto>.Failure("An error occurred", "UNKNOWN_ERROR"));
    }
}


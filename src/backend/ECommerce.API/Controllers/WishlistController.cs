using ECommerce.Application.DTOs.Wishlist;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for managing user wishlists.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;
    private readonly ILogger<WishlistController> _logger;

    public WishlistController(IWishlistService wishlistService, ILogger<WishlistController> logger)
    {
        _wishlistService = wishlistService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWishlist(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Retrieving wishlist for user {UserId}", userId);

        var wishlist = await _wishlistService.GetUserWishlistAsync(userId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<WishlistDto>.Ok(wishlist, "Wishlist retrieved successfully"));
    }

    [HttpPost("add")]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Adding product {ProductId} to wishlist for user {UserId}", dto.ProductId, userId);

        var wishlist = await _wishlistService.AddToWishlistAsync(userId, dto.ProductId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<WishlistDto>.Ok(wishlist, "Product added to wishlist successfully"));
    }

    [HttpDelete("remove/{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromWishlist(Guid productId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Removing product {ProductId} from wishlist for user {UserId}", productId, userId);

        var wishlist = await _wishlistService.RemoveFromWishlistAsync(userId, productId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<WishlistDto>.Ok(wishlist, "Product removed from wishlist successfully"));
    }

    [HttpGet("contains/{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> IsProductInWishlist(Guid productId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Checking if product {ProductId} is in wishlist for user {UserId}", productId, userId);

        var isInWishlist = await _wishlistService.IsProductInWishlistAsync(userId, productId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<bool>.Ok(isInWishlist, "Check completed successfully"));
    }

    [HttpPost("clear")]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearWishlist(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Clearing wishlist for user {UserId}", userId);

        var wishlist = await _wishlistService.ClearWishlistAsync(userId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<WishlistDto>.Ok(wishlist, "Wishlist cleared successfully"));
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim?.Value == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}

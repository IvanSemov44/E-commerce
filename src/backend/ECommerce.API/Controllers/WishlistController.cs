using ECommerce.Application.DTOs.Wishlist;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Services;
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

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim?.Value == null)
            throw new UnauthorizedAccessException("User not authenticated");
        return Guid.Parse(userIdClaim.Value);
    }

    /// <summary>
    /// Gets the authenticated user's wishlist.
    /// </summary>
    /// <returns>The user's wishlist.</returns>
    /// <response code="200">Wishlist retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WishlistDto>>> GetWishlist()
    {
        try
        {
            var userId = GetUserId();
            var wishlist = await _wishlistService.GetUserWishlistAsync(userId);
            return Ok(ApiResponse<WishlistDto>.Ok(wishlist, "Wishlist retrieved successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(ApiResponse<WishlistDto>.Error(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error: {Message}", ex.Message);
            return BadRequest(ApiResponse<WishlistDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wishlist");
            return StatusCode(500, ApiResponse<WishlistDto>.Error("An error occurred while retrieving the wishlist"));
        }
    }

    /// <summary>
    /// Adds a product to the user's wishlist.
    /// </summary>
    /// <param name="dto">The product to add.</param>
    /// <returns>The updated wishlist.</returns>
    /// <response code="200">Product added to wishlist successfully.</response>
    /// <response code="400">Invalid request or product already in wishlist.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("add")]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WishlistDto>>> AddToWishlist([FromBody] AddToWishlistDto dto)
    {
        try
        {
            if (dto.ProductId == Guid.Empty)
                return BadRequest(ApiResponse<WishlistDto>.Error("Invalid product ID"));

            var userId = GetUserId();
            var wishlist = await _wishlistService.AddToWishlistAsync(userId, dto.ProductId);
            return Ok(ApiResponse<WishlistDto>.Ok(wishlist, "Product added to wishlist successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(ApiResponse<WishlistDto>.Error(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error adding to wishlist: {Message}", ex.Message);
            return BadRequest(ApiResponse<WishlistDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product to wishlist");
            return StatusCode(500, ApiResponse<WishlistDto>.Error("An error occurred while adding the product to the wishlist"));
        }
    }

    /// <summary>
    /// Removes a product from the user's wishlist.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>The updated wishlist.</returns>
    /// <response code="200">Product removed from wishlist successfully.</response>
    /// <response code="400">Product not in wishlist.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="500">Internal server error.</response>
    [HttpDelete("remove/{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WishlistDto>>> RemoveFromWishlist(Guid productId)
    {
        try
        {
            var userId = GetUserId();
            var wishlist = await _wishlistService.RemoveFromWishlistAsync(userId, productId);
            return Ok(ApiResponse<WishlistDto>.Ok(wishlist, "Product removed from wishlist successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(ApiResponse<WishlistDto>.Error(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error removing from wishlist: {Message}", ex.Message);
            return BadRequest(ApiResponse<WishlistDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing product from wishlist");
            return StatusCode(500, ApiResponse<WishlistDto>.Error("An error occurred while removing the product from the wishlist"));
        }
    }

    /// <summary>
    /// Checks if a product is in the user's wishlist.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>True if product is in wishlist, false otherwise.</returns>
    /// <response code="200">Check completed successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("contains/{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> IsProductInWishlist(Guid productId)
    {
        try
        {
            var userId = GetUserId();
            var isInWishlist = await _wishlistService.IsProductInWishlistAsync(userId, productId);
            return Ok(ApiResponse<bool>.Ok(isInWishlist, "Check completed successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(ApiResponse<bool>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking wishlist");
            return StatusCode(500, ApiResponse<bool>.Error("An error occurred while checking the wishlist"));
        }
    }

    /// <summary>
    /// Clears all items from the user's wishlist.
    /// </summary>
    /// <returns>The cleared wishlist.</returns>
    /// <response code="200">Wishlist cleared successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("clear")]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<WishlistDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WishlistDto>>> ClearWishlist()
    {
        try
        {
            var userId = GetUserId();
            var wishlist = await _wishlistService.ClearWishlistAsync(userId);
            return Ok(ApiResponse<WishlistDto>.Ok(wishlist, "Wishlist cleared successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(ApiResponse<WishlistDto>.Error(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error clearing wishlist: {Message}", ex.Message);
            return BadRequest(ApiResponse<WishlistDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing wishlist");
            return StatusCode(500, ApiResponse<WishlistDto>.Error("An error occurred while clearing the wishlist"));
        }
    }
}

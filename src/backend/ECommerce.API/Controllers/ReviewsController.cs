using ECommerce.Application.DTOs.Reviews;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Services;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for managing product reviews.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(IReviewService reviewService, ILogger<ReviewsController> logger)
    {
        _reviewService = reviewService;
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
    /// Gets all approved reviews for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>List of approved reviews.</returns>
    /// <response code="200">Reviews retrieved successfully.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("product/{productId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDto>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ReviewDto>>>> GetProductReviews(Guid productId)
    {
        try
        {
            var reviews = await _reviewService.GetProductReviewsAsync(productId);
            return Ok(ApiResponse<IEnumerable<ReviewDto>>.Ok(reviews, "Reviews retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Product not found: {Message}", ex.Message);
            return NotFound(ApiResponse<IEnumerable<ReviewDto>>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product reviews");
            return StatusCode(500, ApiResponse<IEnumerable<ReviewDto>>.Error("An error occurred while retrieving reviews"));
        }
    }

    /// <summary>
    /// Gets average rating for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>Average rating.</returns>
    /// <response code="200">Rating retrieved successfully.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("product/{productId:guid}/rating")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<decimal>>> GetProductAverageRating(Guid productId)
    {
        try
        {
            var rating = await _reviewService.GetProductAverageRatingAsync(productId);
            return Ok(ApiResponse<decimal>.Ok(rating, "Average rating retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product average rating");
            return StatusCode(500, ApiResponse<decimal>.Error("An error occurred while retrieving the average rating"));
        }
    }

    /// <summary>
    /// Gets all reviews by authenticated user.
    /// </summary>
    /// <returns>List of user's reviews.</returns>
    /// <response code="200">Reviews retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("my-reviews")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDetailDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDetailDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ReviewDetailDto>>>> GetMyReviews()
    {
        try
        {
            var userId = GetUserId();
            var reviews = await _reviewService.GetUserReviewsAsync(userId);
            return Ok(ApiResponse<IEnumerable<ReviewDetailDto>>.Ok(reviews, "Your reviews retrieved successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(ApiResponse<IEnumerable<ReviewDetailDto>>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user reviews");
            return StatusCode(500, ApiResponse<IEnumerable<ReviewDetailDto>>.Error("An error occurred while retrieving your reviews"));
        }
    }

    /// <summary>
    /// Gets a specific review by ID.
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <returns>The review details.</returns>
    /// <response code="200">Review retrieved successfully.</response>
    /// <response code="404">Review not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{reviewId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ReviewDetailDto>>> GetReviewById(Guid reviewId)
    {
        try
        {
            var review = await _reviewService.GetReviewByIdAsync(reviewId);
            return Ok(ApiResponse<ReviewDetailDto>.Ok(review, "Review retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Review not found");
            return NotFound(ApiResponse<ReviewDetailDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving review");
            return StatusCode(500, ApiResponse<ReviewDetailDto>.Error("An error occurred while retrieving the review"));
        }
    }

    /// <summary>
    /// Creates a new review for a product.
    /// </summary>
    /// <param name="dto">The review data.</param>
    /// <returns>The created review.</returns>
    /// <response code="201">Review created successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ReviewDetailDto>>> CreateReview([FromBody] CreateReviewDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<ReviewDetailDto>.Error("Validation failed", errors));
            }

            var userId = GetUserId();
            var review = await _reviewService.CreateReviewAsync(userId, dto);
            return CreatedAtAction(nameof(GetReviewById), new { reviewId = review.Id },
                ApiResponse<ReviewDetailDto>.Ok(review, "Review created successfully. It will be visible after admin approval."));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid review data: {Message}", ex.Message);
            return BadRequest(ApiResponse<ReviewDetailDto>.Error(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Review creation error: {Message}", ex.Message);
            return BadRequest(ApiResponse<ReviewDetailDto>.Error(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized");
            return Unauthorized(ApiResponse<ReviewDetailDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review");
            return StatusCode(500, ApiResponse<ReviewDetailDto>.Error("An error occurred while creating the review"));
        }
    }

    /// <summary>
    /// Updates an existing review.
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <param name="dto">The updated review data.</param>
    /// <returns>The updated review.</returns>
    /// <response code="200">Review updated successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User not authenticated or unauthorized.</response>
    /// <response code="404">Review not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPut("{reviewId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ReviewDetailDto>>> UpdateReview(Guid reviewId, [FromBody] UpdateReviewDto dto)
    {
        try
        {
            var userId = GetUserId();
            var review = await _reviewService.UpdateReviewAsync(userId, reviewId, dto);
            return Ok(ApiResponse<ReviewDetailDto>.Ok(review, "Review updated successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid review data: {Message}", ex.Message);
            return BadRequest(ApiResponse<ReviewDetailDto>.Error(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Review update error: {Message}", ex.Message);
            return NotFound(ApiResponse<ReviewDetailDto>.Error(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to review");
            return Unauthorized(ApiResponse<ReviewDetailDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating review");
            return StatusCode(500, ApiResponse<ReviewDetailDto>.Error("An error occurred while updating the review"));
        }
    }

    /// <summary>
    /// Deletes a review.
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <returns>Success status.</returns>
    /// <response code="200">Review deleted successfully.</response>
    /// <response code="401">User not authenticated or unauthorized.</response>
    /// <response code="404">Review not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpDelete("{reviewId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteReview(Guid reviewId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _reviewService.DeleteReviewAsync(userId, reviewId);
            if (!result)
            {
                return NotFound(ApiResponse<bool>.Error("Review not found"));
            }
            return Ok(ApiResponse<bool>.Ok(true, "Review deleted successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to review");
            return Unauthorized(ApiResponse<bool>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review");
            return StatusCode(500, ApiResponse<bool>.Error("An error occurred while deleting the review"));
        }
    }

    /// <summary>
    /// Gets all pending reviews awaiting admin approval.
    /// </summary>
    /// <returns>List of pending reviews.</returns>
    /// <response code="200">Pending reviews retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User is not admin.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("admin/pending")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDetailDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDetailDto>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDetailDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ReviewDetailDto>>>> GetPendingReviews()
    {
        try
        {
            var reviews = await _reviewService.GetPendingReviewsAsync();
            return Ok(ApiResponse<IEnumerable<ReviewDetailDto>>.Ok(reviews, "Pending reviews retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending reviews");
            return StatusCode(500, ApiResponse<IEnumerable<ReviewDetailDto>>.Error("An error occurred while retrieving pending reviews"));
        }
    }

    /// <summary>
    /// Approves a review for display.
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <returns>The approved review.</returns>
    /// <response code="200">Review approved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User is not admin.</response>
    /// <response code="404">Review not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("{reviewId:guid}/approve")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ReviewDetailDto>>> ApproveReview(Guid reviewId)
    {
        try
        {
            var review = await _reviewService.ApproveReviewAsync(reviewId);
            return Ok(ApiResponse<ReviewDetailDto>.Ok(review, "Review approved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Review not found");
            return NotFound(ApiResponse<ReviewDetailDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving review");
            return StatusCode(500, ApiResponse<ReviewDetailDto>.Error("An error occurred while approving the review"));
        }
    }

    /// <summary>
    /// Rejects a review (deletes it).
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <returns>The rejected review.</returns>
    /// <response code="200">Review rejected successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User is not admin.</response>
    /// <response code="404">Review not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("{reviewId:guid}/reject")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ReviewDetailDto>>> RejectReview(Guid reviewId)
    {
        try
        {
            var review = await _reviewService.RejectReviewAsync(reviewId);
            return Ok(ApiResponse<ReviewDetailDto>.Ok(review, "Review rejected and deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Review not found");
            return NotFound(ApiResponse<ReviewDetailDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting review");
            return StatusCode(500, ApiResponse<ReviewDetailDto>.Error("An error occurred while rejecting the review"));
        }
    }
}

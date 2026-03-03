using ECommerce.API.ActionFilters;
using ECommerce.Application.DTOs.Reviews;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(IReviewService reviewService, ICurrentUserService currentUser, ILogger<ReviewsController> logger)
    {
        _reviewService = reviewService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all approved reviews for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of approved reviews for the product.</returns>
    /// <response code="200">Reviews retrieved successfully.</response>
    /// <response code="404">Product not found.</response>
    [HttpGet("product/{productId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductReviews(Guid productId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving reviews for product {ProductId}", productId);
        var reviews = await _reviewService.GetProductReviewsAsync(productId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<IEnumerable<ReviewDto>>.Ok(reviews, "Reviews retrieved successfully"));
    }

    /// <summary>
    /// Retrieves the average rating for a specific product based on approved reviews.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The average rating (0-5 stars).</returns>
    /// <response code="200">Average rating retrieved successfully.</response>
    [HttpGet("product/{productId:guid}/rating")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductAverageRating(Guid productId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving average rating for product {ProductId}", productId);
        var rating = await _reviewService.GetProductAverageRatingAsync(productId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<decimal>.Ok(rating, "Average rating retrieved successfully"));
    }

    /// <summary>
    /// Retrieves all reviews created by the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of the user's reviews including pending and approved ones.</returns>
    /// <response code="200">Reviews retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("my-reviews")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyReviews(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("Retrieving reviews for user {UserId}", userId);
        var reviews = await _reviewService.GetUserReviewsAsync(userId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<IEnumerable<ReviewDetailDto>>.Ok(reviews, "Your reviews retrieved successfully"));
    }

    /// <summary>
    /// Retrieves a specific review by its ID.
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The review details.</returns>
    /// <response code="200">Review retrieved successfully.</response>
    /// <response code="404">Review not found.</response>
    [HttpGet("{reviewId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReviewById(Guid reviewId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving review {ReviewId}", reviewId);
        var review = await _reviewService.GetReviewByIdAsync(reviewId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<ReviewDetailDto>.Ok(review, "Review retrieved successfully"));
    }

    /// <summary>
    /// Creates a new product review. Reviews require admin approval before being displayed.
    /// </summary>
    /// <param name="dto">The review details including rating and comment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created review in pending status.</returns>
    /// <response code="201">Review created successfully and awaiting approval.</response>
    /// <response code="400">Invalid review data or rating out of range.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="409">User has already reviewed this product.</response>
    [HttpPost]
    [Authorize]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("Creating review for product {ProductId} by user {UserId}", dto.ProductId, userId);

        var review = await _reviewService.CreateReviewAsync(userId, dto, cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetReviewById), new { reviewId = review.Id },
            ApiResponse<ReviewDetailDto>.Ok(review, "Review created successfully. It will be visible after admin approval."));
    }

    /// <summary>
    /// Updates an existing review. Users can only update their own reviews.
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <param name="dto">The updated review details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated review.</returns>
    /// <response code="200">Review updated successfully.</response>
    /// <response code="400">Invalid review data or rating out of range.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Review not found or user does not have permission to update this review.</response>
    [HttpPut("{reviewId:guid}")]
    [Authorize]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateReview(Guid reviewId, [FromBody] UpdateReviewDto dto, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("Updating review {ReviewId} by user {UserId}", reviewId, userId);

        // Get review to check ownership
        var existingReview = await _reviewService.GetReviewByIdAsync(reviewId, cancellationToken: cancellationToken);
        if (existingReview == null)
        {
            return NotFound(ApiResponse<ReviewDetailDto>.Failure("Review not found", "REVIEW_NOT_FOUND"));
        }

        // Ownership check: only review owner or admin can update
        var isAdmin = _currentUser.Role == Core.Enums.UserRole.Admin || _currentUser.Role == Core.Enums.UserRole.SuperAdmin;
        if (!isAdmin && existingReview.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to update review {ReviewId} belonging to {ReviewOwnerId}",
                userId, reviewId, existingReview.UserId);
            return Forbid();
        }

        var review = await _reviewService.UpdateReviewAsync(userId, reviewId, dto, cancellationToken: cancellationToken);
        return Ok(ApiResponse<ReviewDetailDto>.Ok(review, "Review updated successfully"));
    }

    /// <summary>
    /// Deletes a review. Users can only delete their own reviews.
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deletion result.</returns>
    /// <response code="200">Review deleted successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Review not found or user does not have permission to delete this review.</response>
    [HttpDelete("{reviewId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReview(Guid reviewId, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("Deleting review {ReviewId} by user {UserId}", reviewId, userId);

        // Get review to check ownership
        var existingReview = await _reviewService.GetReviewByIdAsync(reviewId, cancellationToken: cancellationToken);
        if (existingReview == null)
        {
            return NotFound(ApiResponse<object>.Failure("Review not found", "REVIEW_NOT_FOUND"));
        }

        // Ownership check: only review owner or admin can delete
        var isAdmin = _currentUser.Role == Core.Enums.UserRole.Admin || _currentUser.Role == Core.Enums.UserRole.SuperAdmin;
        if (!isAdmin && existingReview.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to delete review {ReviewId} belonging to {ReviewOwnerId}",
                userId, reviewId, existingReview.UserId);
            return Forbid();
        }

        await _reviewService.DeleteReviewAsync(userId, reviewId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<object>.Ok(new object(), "Review deleted successfully"));
    }

    /// <summary>
    /// Retrieves all reviews awaiting admin approval (admin only).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of pending reviews.</returns>
    /// <response code="200">Pending reviews retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view pending reviews.</response>
    [HttpGet("admin/pending")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDetailDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingReviews(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving pending reviews");
        var reviews = await _reviewService.GetPendingReviewsAsync(cancellationToken: cancellationToken);
        return Ok(ApiResponse<IEnumerable<ReviewDetailDto>>.Ok(reviews, "Pending reviews retrieved successfully"));
    }

    /// <summary>
    /// Approves a pending review, making it visible to all users (admin only).
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The approved review.</returns>
    /// <response code="200">Review approved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to approve reviews.</response>
    /// <response code="404">Review not found.</response>
    [HttpPost("{reviewId:guid}/approve")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveReview(Guid reviewId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Approving review {ReviewId}", reviewId);
        var review = await _reviewService.ApproveReviewAsync(reviewId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<ReviewDetailDto>.Ok(review, "Review approved successfully"));
    }

    /// <summary>
    /// Rejects a pending review and removes it from the system (admin only).
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rejection result.</returns>
    /// <response code="200">Review rejected and deleted successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to reject reviews.</response>
    /// <response code="404">Review not found.</response>
    [HttpPost("{reviewId:guid}/reject")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectReview(Guid reviewId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rejecting review {ReviewId}", reviewId);
        var review = await _reviewService.RejectReviewAsync(reviewId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<ReviewDetailDto>.Ok(review, "Review rejected and deleted successfully"));
    }
}


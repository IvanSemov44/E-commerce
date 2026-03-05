using ECommerce.API.ActionFilters;
using ECommerce.Application.DTOs.Reviews;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using ECommerce.Core.Results;
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
        var result = await _reviewService.GetProductReviewsAsync(productId, cancellationToken: cancellationToken);
        return result is Result<IEnumerable<ReviewDto>>.Success success
            ? Ok(ApiResponse<IEnumerable<ReviewDto>>.Ok(success.Data, "Reviews retrieved successfully"))
            : result is Result<IEnumerable<ReviewDto>>.Failure failure
                ? BadRequest(ApiResponse<IEnumerable<ReviewDto>>.Failure(failure.Message, failure.Code))
                : BadRequest(ApiResponse<IEnumerable<ReviewDto>>.Failure("An error occurred", "UNKNOWN_ERROR"));
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
        var result = await _reviewService.GetUserReviewsAsync(userId, cancellationToken: cancellationToken);
        return result is Result<IEnumerable<ReviewDetailDto>>.Success success
            ? Ok(ApiResponse<IEnumerable<ReviewDetailDto>>.Ok(success.Data, "Your reviews retrieved successfully"))
            : result is Result<IEnumerable<ReviewDetailDto>>.Failure failure
                ? BadRequest(ApiResponse<IEnumerable<ReviewDetailDto>>.Failure(failure.Message, failure.Code))
                : BadRequest(ApiResponse<IEnumerable<ReviewDetailDto>>.Failure("An error occurred", "UNKNOWN_ERROR"));
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
        var result = await _reviewService.GetReviewByIdAsync(reviewId, cancellationToken: cancellationToken);
        return result is Result<ReviewDetailDto>.Success success
            ? Ok(ApiResponse<ReviewDetailDto>.Ok(success.Data, "Review retrieved successfully"))
            : result is Result<ReviewDetailDto>.Failure failure
                ? BadRequest(ApiResponse<ReviewDetailDto>.Failure(failure.Message, failure.Code))
                : BadRequest(ApiResponse<ReviewDetailDto>.Failure("An error occurred", "UNKNOWN_ERROR"));
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

        var result = await _reviewService.CreateReviewAsync(userId, dto, cancellationToken: cancellationToken);
        return result is Result<ReviewDetailDto>.Success success
            ? CreatedAtAction(
                nameof(GetReviewById),
                new { reviewId = success.Data.Id },
                ApiResponse<ReviewDetailDto>.Ok(success.Data, "Review created successfully. It will be visible after admin approval."))
            : result is Result<ReviewDetailDto>.Failure failure
                ? BadRequest(ApiResponse<ReviewDetailDto>.Failure(failure.Message, failure.Code))
                : BadRequest(ApiResponse<ReviewDetailDto>.Failure("An error occurred", "UNKNOWN_ERROR"));
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
        var existingResult = await _reviewService.GetReviewByIdAsync(reviewId, cancellationToken: cancellationToken);
        return await existingResult.Match(
            async existingReview =>
            {
                // Ownership check: only review owner or admin can update
                var isAdmin = _currentUser.Role == Core.Enums.UserRole.Admin || _currentUser.Role == Core.Enums.UserRole.SuperAdmin;
                if (!isAdmin && existingReview.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to update review {ReviewId} belonging to {ReviewOwnerId}",
                        userId, reviewId, existingReview.UserId);
                    return Forbid();
                }

                var updateResult = await _reviewService.UpdateReviewAsync(userId, reviewId, dto, cancellationToken: cancellationToken);
                return updateResult is Result<ReviewDetailDto>.Success success
                    ? (IActionResult)Ok(ApiResponse<ReviewDetailDto>.Ok(success.Data, "Review updated successfully"))
                    : updateResult is Result<ReviewDetailDto>.Failure failure
                        ? (IActionResult)BadRequest(ApiResponse<ReviewDetailDto>.Failure(failure.Message, failure.Code))
                        : (IActionResult)BadRequest(ApiResponse<ReviewDetailDto>.Failure("An error occurred", "UNKNOWN_ERROR"));
            },
            _ => Task.FromResult((IActionResult)NotFound(ApiResponse<ReviewDetailDto>.Failure("Review not found", "REVIEW_NOT_FOUND"))));
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
        var existingResult = await _reviewService.GetReviewByIdAsync(reviewId, cancellationToken: cancellationToken);
        return await existingResult.Match(
            async existingReview =>
            {
                // Ownership check: only review owner or admin can delete
                var isAdmin = _currentUser.Role == Core.Enums.UserRole.Admin || _currentUser.Role == Core.Enums.UserRole.SuperAdmin;
                if (!isAdmin && existingReview.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to delete review {ReviewId} belonging to {ReviewOwnerId}",
                        userId, reviewId, existingReview.UserId);
                    return Forbid();
                }

                var deleteResult = await _reviewService.DeleteReviewAsync(userId, reviewId, cancellationToken: cancellationToken);
                return deleteResult is Result<Unit>.Success
                    ? (IActionResult)Ok(ApiResponse<object>.Ok(new object(), "Review deleted successfully"))
                    : deleteResult is Result<Unit>.Failure failure
                        ? (IActionResult)BadRequest(ApiResponse<object>.Failure(failure.Message, failure.Code))
                        : (IActionResult)BadRequest(ApiResponse<object>.Failure("An error occurred", "UNKNOWN_ERROR"));
            },
            _ => Task.FromResult((IActionResult)NotFound(ApiResponse<object>.Failure("Review not found", "REVIEW_NOT_FOUND"))));
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
        var result = await _reviewService.ApproveReviewAsync(reviewId, cancellationToken: cancellationToken);
        return result is Result<ReviewDetailDto>.Success success
            ? Ok(ApiResponse<ReviewDetailDto>.Ok(success.Data, "Review updated successfully"))
            : result is Result<ReviewDetailDto>.Failure failure
                ? BadRequest(ApiResponse<ReviewDetailDto>.Failure(failure.Message, failure.Code))
                : BadRequest(ApiResponse<ReviewDetailDto>.Failure("An error occurred", "UNKNOWN_ERROR"));
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
        var result = await _reviewService.RejectReviewAsync(reviewId, cancellationToken: cancellationToken);
        return result is Result<ReviewDetailDto>.Success success
            ? Ok(ApiResponse<ReviewDetailDto>.Ok(success.Data, "Review rejected and deleted successfully"))
            : result is Result<ReviewDetailDto>.Failure failure
                ? BadRequest(ApiResponse<ReviewDetailDto>.Failure(failure.Message, failure.Code))
                : BadRequest(ApiResponse<ReviewDetailDto>.Failure("An error occurred", "UNKNOWN_ERROR"));
    }
}


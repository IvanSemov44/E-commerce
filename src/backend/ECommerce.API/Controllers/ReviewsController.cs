using ECommerce.API.ActionFilters;
using ECommerce.Application.DTOs.Reviews;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.DTOs.Common;
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

    [HttpGet("product/{productId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductReviews(Guid productId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving reviews for product {ProductId}", productId);
        var reviews = await _reviewService.GetProductReviewsAsync(productId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<IEnumerable<ReviewDto>>.Ok(reviews, "Reviews retrieved successfully"));
    }

    [HttpGet("product/{productId:guid}/rating")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductAverageRating(Guid productId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving average rating for product {ProductId}", productId);
        var rating = await _reviewService.GetProductAverageRatingAsync(productId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<decimal>.Ok(rating, "Average rating retrieved successfully"));
    }

    [HttpGet("my-reviews")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyReviews(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Retrieving reviews for user {UserId}", userId);
        var reviews = await _reviewService.GetUserReviewsAsync(userId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<IEnumerable<ReviewDetailDto>>.Ok(reviews, "Your reviews retrieved successfully"));
    }

    [HttpGet("{reviewId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReviewById(Guid reviewId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving review {ReviewId}", reviewId);
        var review = await _reviewService.GetReviewByIdAsync(reviewId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<ReviewDetailDto>.Ok(review, "Review retrieved successfully"));
    }

    [HttpPost]
    [Authorize]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Creating review for product {ProductId} by user {UserId}", dto.ProductId, userId);

        var review = await _reviewService.CreateReviewAsync(userId, dto, cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetReviewById), new { reviewId = review.Id },
            ApiResponse<ReviewDetailDto>.Ok(review, "Review created successfully. It will be visible after admin approval."));
    }

    [HttpPut("{reviewId:guid}")]
    [Authorize]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateReview(Guid reviewId, [FromBody] UpdateReviewDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Updating review {ReviewId} by user {UserId}", reviewId, userId);

        var review = await _reviewService.UpdateReviewAsync(userId, reviewId, dto, cancellationToken: cancellationToken);
        return Ok(ApiResponse<ReviewDetailDto>.Ok(review, "Review updated successfully"));
    }

    [HttpDelete("{reviewId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReview(Guid reviewId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Deleting review {ReviewId} by user {UserId}", reviewId, userId);

        await _reviewService.DeleteReviewAsync(userId, reviewId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<object>.Ok(new object(), "Review deleted successfully"));
    }

    [HttpGet("admin/pending")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDetailDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingReviews(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving pending reviews");
        var reviews = await _reviewService.GetPendingReviewsAsync(cancellationToken: cancellationToken);
        return Ok(ApiResponse<IEnumerable<ReviewDetailDto>>.Ok(reviews, "Pending reviews retrieved successfully"));
    }

    [HttpPost("{reviewId:guid}/approve")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveReview(Guid reviewId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Approving review {ReviewId}", reviewId);
        var review = await _reviewService.ApproveReviewAsync(reviewId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<ReviewDetailDto>.Ok(review, "Review approved successfully"));
    }

    [HttpPost("{reviewId:guid}/reject")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ReviewDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectReview(Guid reviewId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rejecting review {ReviewId}", reviewId);
        var review = await _reviewService.RejectReviewAsync(reviewId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<ReviewDetailDto>.Ok(review, "Review rejected and deleted successfully"));
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

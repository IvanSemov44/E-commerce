using System.Collections.Frozen;
using ECommerce.API.ActionFilters;
using ECommerce.API.Common.Extensions;
using ECommerce.API.Common.Helpers;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Contracts.DTOs.Common;
using ECommerce.Reviews.Application.Commands.ApproveReview;
using ECommerce.Reviews.Application.Commands.CreateReview;
using ECommerce.Reviews.Application.Commands.DeleteReview;
using ECommerce.Reviews.Application.Commands.RejectReview;
using ECommerce.Reviews.Application.Commands.UpdateReview;
using ECommerce.Reviews.Application.DTOs;
using ECommerce.SharedKernel.Enums;
using ECommerce.Reviews.Application.Queries;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CreateReviewRequestDto = ECommerce.Reviews.Application.DTOs.CreateReviewRequestDto;
using UpdateReviewRequestDto = ECommerce.Reviews.Application.DTOs.UpdateReviewRequestDto;

namespace ECommerce.API.Features.Reviews.Controllers;

/// <summary>
/// Controller for managing product reviews.
/// </summary>
[ApiController]
[Route("api/reviews")]
[Produces("application/json")]
[Tags("Reviews")]
public class ReviewsController(IMediator mediator, ICurrentUserService currentUser, ILogger<ReviewsController> logger) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<ReviewsController> _logger = logger;

    private static readonly FrozenSet<string> _notFound = FrozenSet.Create(
        "PRODUCT_NOT_FOUND", "REVIEW_NOT_FOUND", "USER_NOT_FOUND");

    private static readonly FrozenSet<string> _conflict = FrozenSet.Create(
        "DUPLICATE_REVIEW", "CONCURRENCY_CONFLICT");

    private static readonly FrozenSet<string> _unprocessable = FrozenSet.Create(
        "RATING_RANGE",
        "REVIEW_TITLE_EMPTY", "REVIEW_TITLE_LONG",
        "REVIEW_BODY_EMPTY", "REVIEW_BODY_SHORT", "REVIEW_BODY_LONG",
        "REVIEW_ALREADY_APPROVED", "REVIEW_UPDATE_EXPIRED");

    private IActionResult MapError(DomainError error)
    {
        var body = ApiResponse<object>.Failure(error.Message, error.Code);
        if (_notFound.Contains(error.Code))       return NotFound(body);
        if (_conflict.Contains(error.Code))       return Conflict(body);
        if (_unprocessable.Contains(error.Code))  return UnprocessableEntity(body);
        if (error.Code == "UNAUTHORIZED")        return StatusCode(StatusCodes.Status403Forbidden, body);
        return BadRequest(body);
    }

    /// <summary>
    /// Retrieves all approved reviews for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="page">Page number (minimum 1).</param>
    /// <param name="pageSize">Page size (minimum 1, maximum 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of approved reviews for the product.</returns>
    /// <response code="200">Reviews retrieved successfully.</response>
    /// <response code="404">Product not found.</response>
    [HttpGet("product/{productId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ECommerce.Reviews.Application.DTOs.Common.PaginatedResult<ReviewDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProductReviews(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);

        _logger.LogInformation("Retrieving reviews for product {ProductId}", productId);
        var result = await _mediator.Send(new GetProductReviewsQuery(productId, page, pageSize), cancellationToken);
        return result.ToActionResult(
            data => Ok(ApiResponse<ECommerce.Reviews.Application.DTOs.Common.PaginatedResult<ReviewDto>>.Ok(data, "Reviews retrieved successfully")),
            MapError);
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProductAverageRating(Guid productId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving average rating for product {ProductId}", productId);
        var result = await _mediator.Send(new GetProductAverageRatingQuery(productId), cancellationToken);
        return result.ToActionResult(
            rating => Ok(ApiResponse<decimal>.Ok(rating, "Average rating retrieved successfully")),
            MapError);
    }

    /// <summary>
    /// Retrieves all reviews created by the authenticated user.
    /// </summary>
    /// <param name="page">Page number (minimum 1).</param>
    /// <param name="pageSize">Page size (minimum 1, maximum 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of the user's reviews including pending and approved ones.</returns>
    /// <response code="200">Reviews retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("my-reviews")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ECommerce.Reviews.Application.DTOs.Common.PaginatedResult<ReviewDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);

        var userId = _currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<ECommerce.Reviews.Application.DTOs.Common.PaginatedResult<ReviewDetailDto>>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        _logger.LogInformation("Retrieving reviews for user {UserId}", userId.Value);
        var result = await _mediator.Send(new GetUserReviewsQuery(userId.Value, page, pageSize), cancellationToken);
        return result.ToActionResult(
            data => Ok(ApiResponse<ECommerce.Reviews.Application.DTOs.Common.PaginatedResult<ReviewDetailDto>>.Ok(data, "Your reviews retrieved successfully")),
            MapError);
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetReviewById(Guid reviewId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving review {ReviewId}", reviewId);
        var result = await _mediator.Send(new GetReviewByIdQuery(reviewId), cancellationToken);
        return result.ToActionResult(
            data => Ok(ApiResponse<ReviewDetailDto>.Ok(data, "Review retrieved successfully")),
            MapError);
    }

    /// <summary>
    /// Retrieves all reviews for admin moderation and reporting.
    /// </summary>
    /// <param name="page">Page number (minimum 1).</param>
    /// <param name="pageSize">Page size (minimum 1, maximum 100).</param>
    /// <param name="search">Optional search text.</param>
    /// <param name="status">Optional review status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of all reviews.</returns>
    /// <response code="200">Reviews retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view all reviews.</response>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ECommerce.Reviews.Application.DTOs.Common.PaginatedResult<ReviewDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);

        _logger.LogInformation("Retrieving all reviews for admin moderation");
        var result = await _mediator.Send(new GetAllReviewsQuery(page, pageSize, search, status), cancellationToken);
        return result.ToActionResult(
            data => Ok(ApiResponse<ECommerce.Reviews.Application.DTOs.Common.PaginatedResult<ReviewDto>>.Ok(data, "Reviews retrieved successfully")),
            MapError);
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequestDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<ReviewDetailDto>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        _logger.LogInformation("Creating review for product {ProductId} by user {UserId}", dto.ProductId, userId.Value);

        var result = await _mediator.Send(
            new CreateReviewCommand(dto.ProductId, userId.Value, dto.Rating, dto.Title, dto.Comment),
            cancellationToken);

        return result.ToActionResult(
            success => CreatedAtAction(
                nameof(GetReviewById),
                new { reviewId = success.Id },
                ApiResponse<ReviewDetailDto>.Ok(success, "Review created successfully. It will be visible after admin approval.")),
            MapError);
    }

    /// <summary>
    /// Updates an existing review. Users can only update their own reviews.
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <param name="dto">The updated review details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Review updated successfully.</response>
    /// <response code="400">Invalid review data or rating out of range.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Review not found or user does not have permission to update this review.</response>
    [HttpPut("{reviewId:guid}")]
    [Authorize]
    [ValidationFilter]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateReview(Guid reviewId, [FromBody] UpdateReviewRequestDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        var role = _currentUser.RoleOrNull;
        var isAdmin = role == UserRole.Admin || role == UserRole.SuperAdmin;
        _logger.LogInformation("Updating review {ReviewId} by user {UserId}", reviewId, userId.Value);

        var result = await _mediator.Send(
            new UpdateReviewCommand(reviewId, userId.Value, isAdmin, dto.Rating, dto.Title, dto.Comment),
            cancellationToken);

        return result.ToActionResult(NoContent, MapError);
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteReview(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        var role = _currentUser.RoleOrNull;
        var isAdmin = role == UserRole.Admin || role == UserRole.SuperAdmin;
        _logger.LogInformation("Deleting review {ReviewId} by user {UserId}", reviewId, userId.Value);

        var result = await _mediator.Send(new DeleteReviewCommand(reviewId, userId.Value, isAdmin), cancellationToken);
        return result.ToActionResult(
            () => Ok(ApiResponse<object>.Ok(new object(), "Review deleted successfully")),
            MapError);
    }

    /// <summary>
    /// Retrieves all reviews awaiting admin approval (admin only).
    /// </summary>
    /// <param name="page">Page number (minimum 1).</param>
    /// <param name="pageSize">Page size (minimum 1, maximum 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of pending reviews.</returns>
    /// <response code="200">Pending reviews retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view pending reviews.</response>
    [HttpGet("admin/pending")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ECommerce.Reviews.Application.DTOs.Common.PaginatedResult<ReviewDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPendingReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);

        _logger.LogInformation("Retrieving pending reviews");
        var result = await _mediator.Send(new GetPendingReviewsQuery(page, pageSize), cancellationToken);
        return result.ToActionResult(
            reviews => Ok(ApiResponse<ECommerce.Reviews.Application.DTOs.Common.PaginatedResult<ReviewDetailDto>>.Ok(reviews, "Pending reviews retrieved successfully")),
            MapError);
    }

    /// <summary>
    /// Retrieves flagged reviews for admin moderation.
    /// </summary>
    /// <param name="page">Page number (minimum 1).</param>
    /// <param name="pageSize">Page size (minimum 1, maximum 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of flagged reviews.</returns>
    /// <response code="200">Flagged reviews retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view flagged reviews.</response>
    [HttpGet("admin/flagged")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ECommerce.Reviews.Application.DTOs.Common.PaginatedResult<ReviewDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFlaggedReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);

        _logger.LogInformation("Retrieving flagged reviews");
        var result = await _mediator.Send(new GetFlaggedReviewsQuery(page, pageSize), cancellationToken);
        return result.ToActionResult(
            reviews => Ok(ApiResponse<ECommerce.Reviews.Application.DTOs.Common.PaginatedResult<ReviewDto>>.Ok(reviews, "Flagged reviews retrieved successfully")),
            MapError);
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ApproveReview(Guid reviewId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Approving review {ReviewId}", reviewId);
        var result = await _mediator.Send(new ApproveReviewCommand(reviewId), cancellationToken);
        return result.ToActionResult(
            () => Ok(ApiResponse<object>.Ok(new object(), "Review approved successfully")),
            MapError);
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RejectReview(Guid reviewId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rejecting review {ReviewId}", reviewId);
        var result = await _mediator.Send(new RejectReviewCommand(reviewId), cancellationToken);
        return result.ToActionResult(
            () => Ok(ApiResponse<object>.Ok(new object(), "Review rejected successfully")),
            MapError);
    }
}






# Phase 6, Step 4: Cutover

**Prerequisite**: Steps 1–3 complete. Characterization and E2E tests passing against the OLD service.

Rewrite `ReviewsController` to dispatch via MediatR, then delete the old service, interface, and DTOs.

---

## Task 1: Pre-cutover verification

```bash
cd src/backend
dotnet test ECommerce.Tests --filter "FullyQualifiedName~ReviewsCharacterizationTests" --logger "console;verbosity=normal"
# All must be green before proceeding
```

---

## Task 2: Rewrite ReviewsController

Replace the contents of `src/backend/ECommerce.API/Controllers/ReviewsController.cs` entirely:

```csharp
using ECommerce.API.ActionFilters;
using ECommerce.API.Helpers;
using ECommerce.Application.DTOs.Common;
using ECommerce.Reviews.Application.Commands;
using ECommerce.Reviews.Application.DTOs;
using ECommerce.Reviews.Application.Queries;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/reviews")]
[Produces("application/json")]
[Tags("Reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(IMediator mediator, ILogger<ReviewsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>Get approved reviews for a product (Public).</summary>
    [HttpGet("/api/products/{productId}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductReviews(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);
        var result = await _mediator.Send(new GetProductReviewsQuery(productId, page, pageSize), cancellationToken);
        return Ok(ApiResponse<PaginatedResult<ReviewDto>>.Ok(result.Value!, "Product reviews retrieved successfully"));
    }

    /// <summary>Get all reviews with filters (Admin only).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAllReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);
        var result = await _mediator.Send(new GetAllReviewsQuery(page, pageSize, search, status), cancellationToken);
        return Ok(ApiResponse<PaginatedResult<ReviewDto>>.Ok(result.Value!, "Reviews retrieved successfully"));
    }

    /// <summary>Get review by ID (Admin only).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetReviewById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReviewByIdQuery(id), cancellationToken);
        return MapResult(result, d => Ok(ApiResponse<ReviewDetailDto>.Ok(d, "Review retrieved successfully")));
    }

    /// <summary>Create a new review (Public — supports guest or authenticated).</summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidationFilter]
    public async Task<IActionResult> CreateReview(
        [FromBody] CreateReviewRequestDto dto,
        CancellationToken cancellationToken)
    {
        var cmd = new CreateReviewCommand(
            dto.ProductId,
            dto.AuthorId,
            dto.AuthorName,
            dto.Rating,
            dto.Text);

        var result = await _mediator.Send(cmd, cancellationToken);

        if (!result.IsSuccess) return MapError(result.Error!);

        return CreatedAtAction(
            nameof(GetReviewById),
            new { id = result.Value!.Id },
            ApiResponse<ReviewDetailDto>.Ok(result.Value, "Review created successfully"));
    }

    /// <summary>Update an existing review (Author or Admin).</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ValidationFilter]
    public async Task<IActionResult> UpdateReview(
        Guid id,
        [FromBody] UpdateReviewRequestDto dto,
        CancellationToken cancellationToken)
    {
        var cmd = new UpdateReviewCommand(id, dto.Rating, dto.Text);
        var result = await _mediator.Send(cmd, cancellationToken);
        return MapResult(result, d => Ok(ApiResponse<ReviewDetailDto>.Ok(d, "Review updated successfully")));
    }

    /// <summary>Delete a review (Author or Admin).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteReviewCommand(id), cancellationToken);
        return MapResult(result, _ => Ok(ApiResponse<object>.Ok(new object(), "Review deleted successfully")));
    }

    /// <summary>Mark a review as helpful (Public).</summary>
    [HttpPost("{id:guid}/mark-helpful")]
    [AllowAnonymous]
    public async Task<IActionResult> MarkAsHelpful(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MarkReviewHelpfulCommand(id), cancellationToken);
        return MapResult(result, _ => Ok(ApiResponse<object>.Ok(new object(), "Review marked as helpful")));
    }

    /// <summary>Flag a review for moderation (Public).</summary>
    [HttpPost("{id:guid}/flag")]
    [AllowAnonymous]
    public async Task<IActionResult> FlagReview(
        Guid id,
        [FromBody] FlagReviewRequestDto? dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FlagReviewCommand(id, dto?.Reason), cancellationToken);
        return MapResult(result, _ => Ok(ApiResponse<object>.Ok(new object(), "Review flagged successfully")));
    }

    /// <summary>Approve a review (Admin only).</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ApproveReview(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ApproveReviewCommand(id), cancellationToken);
        return MapResult(result, _ => Ok(ApiResponse<object>.Ok(new object(), "Review approved successfully")));
    }

    /// <summary>Reject a review (Admin only).</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> RejectReview(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RejectReviewCommand(id), cancellationToken);
        return MapResult(result, _ => Ok(ApiResponse<object>.Ok(new object(), "Review rejected successfully")));
    }

    /// <summary>Get pending reviews for moderation (Admin only).</summary>
    [HttpGet("admin/pending")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetPendingReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);
        var result = await _mediator.Send(new GetPendingReviewsQuery(page, pageSize), cancellationToken);
        return Ok(ApiResponse<PaginatedResult<ReviewDto>>.Ok(result.Value!, "Pending reviews retrieved successfully"));
    }

    /// <summary>Get flagged reviews for moderation (Admin only).</summary>
    [HttpGet("admin/flagged")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetFlaggedReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);
        var result = await _mediator.Send(new GetFlaggedReviewsQuery(page, pageSize), cancellationToken);
        return Ok(ApiResponse<PaginatedResult<ReviewDto>>.Ok(result.Value!, "Flagged reviews retrieved successfully"));
    }

    // ──────────────────────────────────────────────────────────
    // Error mapping
    // ──────────────────────────────────────────────────────────

    private IActionResult MapResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess) return onSuccess(result.Value!);
        return MapError(result.Error!);
    }

    private IActionResult MapResult(Result result, Func<Unit, IActionResult> onSuccess)
    {
        if (result.IsSuccess) return onSuccess(Unit.Value);
        return MapError(result.Error!);
    }

    private IActionResult MapError(DomainError error) => error.Code switch
    {
        "REVIEW_NOT_FOUND"              => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),
        "DUPLICATE_REVIEW"              => Conflict(ApiResponse<object>.Failure(error.Message, error.Code)),
        "CONCURRENCY_CONFLICT"          => Conflict(ApiResponse<object>.Failure(error.Message, error.Code)),
        "INVALID_RATING"                => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),
        "REVIEW_TEXT_EMPTY"             => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),
        "REVIEW_TEXT_TOO_LONG"          => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),
        "USER_CANNOT_REVIEW_OWN_PRODUCT" => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),
        "REVIEW_ALREADY_APPROVED"       => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),
        _                               => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code))
    };
}
```

### Request DTOs

Add to `ECommerce.API/Models/` or inline:

```csharp
// CreateReviewRequestDto.cs
public class CreateReviewRequestDto
{
    public Guid ProductId { get; set; }
    public Guid AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public int Rating { get; set; }
    public string Text { get; set; } = null!;
}

// UpdateReviewRequestDto.cs
public class UpdateReviewRequestDto
{
    public int? Rating { get; set; }
    public string? Text { get; set; }
}

// FlagReviewRequestDto.cs
public class FlagReviewRequestDto
{
    public string? Reason { get; set; }
}
```

**Note on validators**: Create FluentValidation validators for the new request DTOs.

---

## Task 3: Delete old files

After characterization tests pass with the new controller:

```bash
# Remove old service and interface
rm src/backend/ECommerce.Application/Services/ReviewService.cs
rm src/backend/ECommerce.Application/Interfaces/IReviewService.cs

# Remove old DTOs (the new ones live in Reviews.Application)
rm src/backend/ECommerce.Application/DTOs/Reviews/ReviewDto.cs
rm src/backend/ECommerce.Application/DTOs/Reviews/ReviewDetailDto.cs
# Remove old validators
find src/backend/ECommerce.Application/Validators/Reviews -name "*.cs" -delete
```

Remove the DI registration from `Program.cs`:
```csharp
// Remove this line:
builder.Services.AddScoped<IReviewService, ReviewService>();
```

---

## Task 4: Post-cutover verification

```bash
cd src/backend
dotnet build
# Must compile with zero errors

dotnet test ECommerce.Tests --filter "FullyQualifiedName~ReviewsCharacterizationTests" --logger "console;verbosity=normal"
# All characterization tests must still pass

dotnet test ECommerce.Tests --filter "FullyQualifiedName~ReviewsControllerTests" --logger "console;verbosity=normal"
# Existing tests must still pass
```

---

## Error code mapping reference

| Domain Error Code        | HTTP Status | Scenario |
|--------------------------|-------------|----------|
| `REVIEW_NOT_FOUND`                   | 404 Not Found | GetById, Update, Delete with unknown id |
| `DUPLICATE_REVIEW`                   | 409 Conflict | User already reviewed this product |
| `CONCURRENCY_CONFLICT`               | 409 Conflict | Two concurrent updates on same review |
| `INVALID_RATING`                     | 400 Bad Request | Rating not 1-5 |
| `REVIEW_TEXT_EMPTY`                  | 400 Bad Request | Empty review text |
| `REVIEW_TEXT_TOO_LONG`               | 400 Bad Request | Text exceeds 1000 chars |
| `USER_CANNOT_REVIEW_OWN_PRODUCT`     | 422 Unprocessable | User reviewing their own product |
| `REVIEW_ALREADY_APPROVED`            | 422 Unprocessable | Attempting to approve already-approved review |
| *(anything else)*                    | 400 Bad Request | Fallthrough |

---

## Acceptance Criteria

- [ ] Controller compiles with zero errors
- [ ] All characterization tests pass against the new MediatR handlers
- [ ] All existing `ReviewsControllerTests` pass
- [ ] `POST /api/reviews` returns 201 Created with Location header
- [ ] `GET /api/products/{productId}/reviews` returns 200 with approved reviews only
- [ ] Duplicate review returns 409 Conflict
- [ ] `REVIEW_NOT_FOUND` returns 404
- [ ] Invalid rating returns 400 with `INVALID_RATING`
- [ ] Admin actions require `[Authorize(Roles = "Admin,SuperAdmin")]`
- [ ] `IReviewService` and `ReviewService` deleted
- [ ] All old DTOs and validators deleted
- [ ] `dotnet build` produces zero errors across the entire solution

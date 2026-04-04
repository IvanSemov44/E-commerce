# Phase 6, Step 1: Domain Project

**Prerequisite**: Steps 0 and 0b characterization tests pass.

Create `ECommerce.Promotions.Domain` — aggregate root, value objects, enums, errors, and repository interface for the Reviews bounded context.

---

## Task 1: Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Reviews.Domain -o ECommerce.Reviews.Domain
dotnet sln ECommerce.sln add ECommerce.Reviews.Domain/ECommerce.Reviews.Domain.csproj
dotnet add ECommerce.Reviews.Domain/ECommerce.Reviews.Domain.csproj reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
rm ECommerce.Reviews.Domain/Class1.cs
```

---

## Task 2: Define Enums and Errors

**File: `ECommerce.Reviews.Domain/Enums/ReviewStatus.cs`**

```csharp
namespace ECommerce.Reviews.Domain.Enums;

public enum ReviewStatus
{
    Pending = 0,      // New review, awaiting admin approval
    Approved = 1,     // Admin approved, shown publicly
    Rejected = 2,     // Admin rejected, hidden from view
    Flagged = 3       // User-flagged for moderation, awaiting admin review
}
```

**File: `ECommerce.Reviews.Domain/ReviewsErrors.cs`**

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Reviews.Domain;

public static class ReviewsErrors
{
    public static readonly DomainError ReviewNotFound = 
        new DomainError("REVIEW_NOT_FOUND", "Review does not exist");

    public static readonly DomainError DuplicateReview = 
        new DomainError("DUPLICATE_REVIEW", "User has already reviewed this product");

    public static readonly DomainError InvalidRating = 
        new DomainError("INVALID_RATING", "Rating must be between 1 and 5");

    public static readonly DomainError ReviewTextEmpty = 
        new DomainError("REVIEW_TEXT_EMPTY", "Review text cannot be empty");

    public static readonly DomainError ReviewTextTooLong = 
        new DomainError("REVIEW_TEXT_TOO_LONG", "Review text must not exceed 1000 characters");

    public static readonly DomainError UserCannotReviewOwnProduct = 
        new DomainError("USER_CANNOT_REVIEW_OWN_PRODUCT", "Sellers cannot review their own products");

    public static readonly DomainError ReviewAlreadyApproved = 
        new DomainError("REVIEW_ALREADY_APPROVED", "Review is already approved");

    public static readonly DomainError ConcurrencyConflict = 
        new DomainError("CONCURRENCY_CONFLICT", "Review was modified by another user");
}
```

---

## Task 3: Define Value Objects

**File: `ECommerce.Reviews.Domain/ValueObjects/Rating.cs`**

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Reviews.Domain.ValueObjects;

public record Rating
{
    public int Value { get; }

    private Rating(int value) => Value = value;

    public static Result<Rating> Create(int value)
    {
        if (value < 1 || value > 5)
            return Result<Rating>.Failure(ReviewsErrors.InvalidRating);

        return Result<Rating>.Ok(new Rating(value));
    }

    public static Rating Reconstitute(int value) => new(value);

    public override string ToString() => Value.ToString();
}
```

**File: `ECommerce.Reviews.Domain/ValueObjects/ReviewText.cs`**

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Reviews.Domain.ValueObjects;

public record ReviewText
{
    private const int MaxLength = 1000;

    public string Value { get; }

    private ReviewText(string value) => Value = value;

    public static Result<ReviewText> Create(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result<ReviewText>.Failure(ReviewsErrors.ReviewTextEmpty);

        if (text.Length > MaxLength)
            return Result<ReviewText>.Failure(ReviewsErrors.ReviewTextTooLong);

        return Result<ReviewText>.Ok(new ReviewText(text.Trim()));
    }

    public static ReviewText Reconstitute(string value) => new(value);

    public override string ToString() => Value;
}
```

---

## Task 4: Define the Aggregate

**File: `ECommerce.Reviews.Domain/Aggregates/Review/Review.cs`**

```csharp
using ECommerce.Reviews.Domain.Enums;
using ECommerce.Reviews.Domain.ValueObjects;
using ECommerce.SharedKernel;

namespace ECommerce.Reviews.Domain.Aggregates.Review;

public class Review : AggregateRoot
{
    public Guid ProductId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string? AuthorName { get; private set; }
    public Rating Rating { get; private set; } = null!;
    public ReviewText Text { get; private set; } = null!;
    public ReviewStatus Status { get; private set; }
    public int HelpfulCount { get; private set; }
    public int FlagCount { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private Review() { } // For EF

    internal Review(
        Guid id,
        Guid productId,
        Guid authorId,
        string? authorName,
        Rating rating,
        ReviewText text)
    {
        Id = id;
        ProductId = productId;
        AuthorId = authorId;
        AuthorName = authorName;
        Rating = rating;
        Text = text;
        Status = ReviewStatus.Pending; // New reviews start as Pending
        HelpfulCount = 0;
        FlagCount = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory: Create a new review (internal — application layer uses factory command)
    /// </summary>
    public static Result<Review> Create(
        Guid productId,
        Guid authorId,
        string? authorName,
        Rating rating,
        ReviewText text)
    {
        var review = new Review(Guid.NewGuid(), productId, authorId, authorName, rating, text);
        review.RaiseDomainEvent(new ReviewCreatedEvent(review.Id, productId, authorId));
        return Result<Review>.Ok(review);
    }

    /// <summary>
    /// Update the review text and rating
    /// </summary>
    public Result Update(Rating? newRating, ReviewText? newText)
    {
        if (newRating is not null)
            Rating = newRating;

        if (newText is not null)
            Text = newText;

        UpdatedAt = DateTime.UtcNow;
        return Result.Ok();
    }

    /// <summary>
    /// Mark the review as helpful (increments count)
    /// </summary>
    public void MarkAsHelpful()
    {
        HelpfulCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Flag the review for moderation
    /// </summary>
    public void Flag()
    {
        FlagCount++;
        if (FlagCount >= 3) // Threshold: 3 flags moves to Flagged status
            Status = ReviewStatus.Flagged;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Approve the review (admin action)
    /// </summary>
    public Result Approve()
    {
        if (Status == ReviewStatus.Approved)
            return Result.Failure(ReviewsErrors.ReviewAlreadyApproved);

        Status = ReviewStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ReviewApprovedEvent(Id));
        return Result.Ok();
    }

    /// <summary>
    /// Reject the review (admin action)
    /// </summary>
    public void Reject()
    {
        Status = ReviewStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ReviewRejectedEvent(Id));
    }

    /// <summary>
    /// Deactivate a flagged review (admin action after moderation)
    /// </summary>
    public void RemoveFlag()
    {
        FlagCount = 0;
        Status = ReviewStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

---

## Task 5: Define Domain Events

**File: `ECommerce.Reviews.Domain/Aggregates/Review/ReviewCreatedEvent.cs`**

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Reviews.Domain.Aggregates.Review;

public record ReviewCreatedEvent(Guid ReviewId, Guid ProductId, Guid AuthorId) : DomainEvent;
```

**File: `ECommerce.Reviews.Domain/Aggregates/Review/ReviewApprovedEvent.cs`**

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Reviews.Domain.Aggregates.Review;

public record ReviewApprovedEvent(Guid ReviewId) : DomainEvent;
```

**File: `ECommerce.Reviews.Domain/Aggregates/Review/ReviewRejectedEvent.cs`**

```csharp
using ECommerce.SharedKernel;

namespace ECommerce.Reviews.Domain.Aggregates.Review;

public record ReviewRejectedEvent(Guid ReviewId) : DomainEvent;
```

---

## Task 6: Define Repository Interface

**File: `ECommerce.Reviews.Domain/Interfaces/IReviewRepository.cs`**

```csharp
using ECommerce.Reviews.Domain.Aggregates.Review;

namespace ECommerce.Reviews.Domain.Interfaces;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Review?> GetByProductAndAuthorAsync(Guid productId, Guid authorId, CancellationToken ct = default);

    Task<(List<Review> Items, int TotalCount)> GetByProductAsync(
        Guid productId, int page, int pageSize, CancellationToken ct = default);

    Task<(List<Review> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? search, string? status, CancellationToken ct = default);

    Task<(List<Review> Items, int TotalCount)> GetPendingAsync(
        int page, int pageSize, CancellationToken ct = default);

    Task<(List<Review> Items, int TotalCount)> GetFlaggedAsync(
        int page, int pageSize, CancellationToken ct = default);

    Task UpsertAsync(Review review, CancellationToken ct = default);

    Task DeleteAsync(Review review, CancellationToken ct = default);
}
```

---

## Task 7: Assembly Visibility

Add to `ECommerce.Reviews.Domain/Properties/AssemblyInfo.cs` (create if not exists):

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ECommerce.Reviews.Application")]
[assembly: InternalsVisibleTo("ECommerce.Reviews.Infrastructure")]
[assembly: InternalsVisibleTo("ECommerce.Tests")]
```

---

## Acceptance Criteria

- [ ] Project builds with zero errors
- [ ] `ReviewStatus` enum has: Pending, Approved, Rejected, Flagged
- [ ] `ReviewsErrors` static class defines all error codes matching characterization tests
- [ ] `Rating` value object validates 1-5 range, has `Reconstitute`
- [ ] `ReviewText` value object validates non-empty and max 1000 chars, has `Reconstitute`
- [ ] `Review` aggregate has: ProductId, AuthorId, Rating, Text, Status, HelpfulCount, FlagCount, RowVersion
- [ ] `Create()` factory returns `Result<Review>` and raises `ReviewCreatedEvent`
- [ ] `Update(rating, text)` accepts nullable parameters
- [ ] `MarkAsHelpful()` increments count
- [ ] `Flag()` increments FlagCount, moves to Flagged status when count ≥ 3
- [ ] `Approve()` returns error if already approved
- [ ] `Reject()` raises `ReviewRejectedEvent`
- [ ] `RemoveFlag()` clears FlagCount and resets to Approved status
- [ ] `IReviewRepository` interface defined with 8 methods
- [ ] `AssemblyInfo` includes visibility grants for Application, Infrastructure, Tests

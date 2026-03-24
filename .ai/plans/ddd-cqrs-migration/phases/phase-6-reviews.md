# Phase 6: Reviews Bounded Context

**Prerequisite**: Phase 5 complete.

**Learn**: Cross-context references by ID only, Anti-Corruption Layer, temporal invariants (edit window), and why Reviews is its own context despite being "just" a review.

---

## What's New in This Phase

Reviews are interesting because they reference THREE other contexts: Catalog (ProductId), Identity (UserId), and Ordering (OrderId for verified purchase). The Review aggregate holds all three as plain GUIDs — no navigation properties, no FKs in the DDD sense.

The new concept is the **Anti-Corruption Layer (ACL)**: when the Reviews context needs to validate that a ProductId exists, it calls an interface that translates between the Catalog context's "language" and the Reviews context's "language." The Reviews context doesn't depend on Catalog domain objects directly.

---

## Old Service → New Handler Mapping

| Old Method | New Handler |
|-----------|-------------|
| `ReviewService.GetReviewsForProductAsync(productId)` | `GetProductReviewsQuery` |
| `ReviewService.GetReviewByIdAsync(id)` | `GetReviewByIdQuery` |
| `ReviewService.CreateReviewAsync(dto)` | `CreateReviewCommand` |
| `ReviewService.EditReviewAsync(id, dto)` | `EditReviewCommand` |
| `ReviewService.DeleteReviewAsync(id)` | `DeleteReviewCommand` (admin or owner) |
| `ReviewService.ApproveReviewAsync(id)` | `ApproveReviewCommand` (admin) |
| *(via OrderService)* | `MarkReviewVerifiedOnOrderDeliveredHandler` (event handler) |

---

## Step 1: Domain Project

### Value Objects

```csharp
// ValueObjects/Rating.cs
public record Rating
{
    public int Value { get; }

    private Rating(int value) => Value = value;

    public static Rating Create(int value)
    {
        if (value < 1 || value > 5)
            throw new ReviewsDomainException("RATING_RANGE", "Rating must be between 1 and 5.");
        return new Rating(value);
    }
}

// ValueObjects/ReviewContent.cs
public record ReviewContent
{
    public string Title { get; }
    public string Body { get; }

    private ReviewContent(string title, string body) { Title = title; Body = body; }

    public static ReviewContent Create(string title, string body)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ReviewsDomainException("REVIEW_TITLE_EMPTY", "Review title cannot be empty.");
        if (title.Trim().Length > 100)
            throw new ReviewsDomainException("REVIEW_TITLE_LONG", "Review title cannot exceed 100 characters.");
        if (string.IsNullOrWhiteSpace(body))
            throw new ReviewsDomainException("REVIEW_BODY_EMPTY", "Review body cannot be empty.");
        if (body.Trim().Length < 10)
            throw new ReviewsDomainException("REVIEW_BODY_SHORT", "Review must be at least 10 characters.");
        if (body.Trim().Length > 2000)
            throw new ReviewsDomainException("REVIEW_BODY_LONG", "Review cannot exceed 2000 characters.");

        return new ReviewContent(title.Trim(), body.Trim());
    }
}
```

### Review aggregate

```csharp
// Aggregates/Review/Review.cs
public class Review : AggregateRoot
{
    public Guid ProductId { get; private set; }  // Reference by ID to Catalog context
    public Guid UserId { get; private set; }     // Reference by ID to Identity context
    public Guid? OrderId { get; private set; }   // Reference by ID to Ordering context (optional)

    public Rating Rating { get; private set; } = null!;
    public ReviewContent Content { get; private set; } = null!;
    public ReviewStatus Status { get; private set; }
    public bool IsVerifiedPurchase { get; private set; }

    private static readonly TimeSpan EditWindow = TimeSpan.FromDays(30);

    private Review() { }

    public static Review Create(
        Guid productId,
        Guid userId,
        Rating rating,
        ReviewContent content,
        Guid? orderId = null)
    {
        return new Review
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            UserId = userId,
            OrderId = orderId,
            Rating = rating,
            Content = content,
            Status = ReviewStatus.Pending,
            IsVerifiedPurchase = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Edit(Rating newRating, ReviewContent newContent, DateTime now)
    {
        if (Status == ReviewStatus.Approved)
            throw new ReviewsDomainException("REVIEW_APPROVED", "Cannot edit an approved review.");

        var editDeadline = CreatedAt.Add(EditWindow);
        if (now > editDeadline)
            throw new ReviewsDomainException(
                "REVIEW_EDIT_WINDOW_EXPIRED",
                $"Reviews can only be edited within {EditWindow.Days} days of creation.");

        Rating = newRating;
        Content = newContent;
    }

    public void Approve()
    {
        if (Status == ReviewStatus.Approved) return;
        Status = ReviewStatus.Approved;
        AddDomainEvent(new ReviewApprovedEvent(Id, ProductId));
    }

    public void MarkAsVerifiedPurchase()
    {
        IsVerifiedPurchase = true;
    }
}
```

**The temporal invariant**: `Edit()` receives `DateTime now` as a parameter (Rule 7 — no service injection). The handler passes `DateTime.UtcNow`. This makes the aggregate testable without mocking a clock.

---

## Step 2: Anti-Corruption Layer

The Reviews context needs to check that a product exists before creating a review. But it must NOT depend on `ECommerce.Catalog.Domain` — that would create a hard dependency between bounded contexts.

The ACL is an interface in the Reviews Application project, with an implementation that translates:

```csharp
// Interfaces/ICatalogService.cs (in Reviews.Application)
// This is the ACL — it speaks in Reviews context language
public interface ICatalogService
{
    Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct = default);
}
```

```csharp
// In Reviews.Infrastructure/Services/CatalogService.cs
// Implementation queries the shared DB — during Phase 8 this becomes an HTTP call
public class CatalogService : ICatalogService
{
    private readonly AppDbContext _db;
    public CatalogService(AppDbContext db) => _db = db;

    public Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct) =>
        _db.Products.AnyAsync(p => p.Id == productId && !p.IsDeleted, ct);
}
```

**Why is this an Anti-Corruption Layer?** The ACL prevents the Reviews context from importing Catalog's domain model. If Catalog changes (renames Product to Item, adds a new field), only the `CatalogService` implementation needs to update — not the Review aggregate or its handlers.

In Phase 8, `CatalogService` implementation changes from a DB query to an HTTP call. The rest of Reviews is unaffected.

---

## Step 3: Handling Cross-Context Events

When an order is delivered, reviews for products in that order become "verified purchases":

```csharp
// EventHandlers/MarkReviewVerifiedOnOrderDeliveredHandler.cs
// Stub for Phase 7 — OrderDeliveredEvent doesn't exist yet
// public class MarkReviewVerifiedOnOrderDeliveredHandler
//     : INotificationHandler<OrderDeliveredEvent>
// {
//     public async Task Handle(OrderDeliveredEvent notification, CancellationToken ct)
//     {
//         // Find reviews for UserId + ProductIds from the order
//         // Mark each as verified purchase
//     }
// }
```

---

## Step 4: One Review Per User Per Product

This invariant cannot be enforced inside the aggregate (it requires a database query). Enforce in the handler:

```csharp
// Commands/CreateReview/CreateReviewCommandHandler.cs
public async Task<Result<ReviewDto>> Handle(CreateReviewCommand command, CancellationToken ct)
{
    var userId = _currentUser.UserId ?? return Result<ReviewDto>.Unauthorized();

    // ACL: check product exists in Catalog
    if (!await _catalog.ProductExistsAsync(command.ProductId, ct))
        return Result<ReviewDto>.Fail(ErrorCodes.Reviews.ProductNotFound, "Product not found.");

    // Uniqueness: one review per user per product
    if (await _reviews.ExistsAsync(command.ProductId, userId, ct))
        return Result<ReviewDto>.Fail(ErrorCodes.Reviews.AlreadyReviewed, "You have already reviewed this product.");

    var review = Review.Create(
        command.ProductId,
        userId,
        Rating.Create(command.Rating),
        ReviewContent.Create(command.Title, command.Body),
        command.OrderId);

    await _reviews.AddAsync(review, ct);
    await _uow.SaveChangesAsync(ct);

    return Result<ReviewDto>.Ok(review.ToDto());
}
```

---

## Definition of Done

- [ ] Characterization tests written against old ReviewService
- [ ] `Review` aggregate with `Edit` (with edit window), `Approve`, `MarkAsVerifiedPurchase`
- [ ] `Rating` and `ReviewContent` value objects with validation
- [ ] `Edit()` receives `DateTime now` as parameter (no clock injection)
- [ ] `ICatalogService` ACL interface in Application, implementation in Infrastructure
- [ ] One-review-per-user-per-product enforced in handler (not aggregate)
- [ ] Event handler stub for `MarkReviewVerifiedOnOrderDeliveredHandler`
- [ ] Old `ReviewService` deleted after tests pass

## What You Learned in Phase 6

- References to other contexts are always by ID (Guid) — never navigation properties across context boundaries
- The Anti-Corruption Layer (ACL) isolates a context from other contexts' domain models
- Temporal invariants: pass `DateTime now` as a parameter to keep aggregates testable (no clock mocking)
- Cross-aggregate uniqueness constraints (one review per user/product) are enforced in handlers
- Event handler stubs document future cross-context wiring without requiring it to be implemented now

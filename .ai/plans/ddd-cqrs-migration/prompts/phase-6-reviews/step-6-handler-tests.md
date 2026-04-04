# Phase 6, Step 6: Handler Tests

**Prerequisite**: Step 5 (Domain tests) complete and passing.

Write comprehensive tests for command and query handlers — testing CQRS layer integration with domain and repository.

---

## Task: Write handler tests

File: `src/backend/ECommerce.Tests/Application/Reviews/ReviewsHandlerTests.cs`

```csharp
using ECommerce.Reviews.Application.Commands;
using ECommerce.Reviews.Application.CommandHandlers;
using ECommerce.Reviews.Application.DTOs;
using ECommerce.Reviews.Application.Queries;
using ECommerce.Reviews.Application.QueryHandlers;
using ECommerce.Reviews.Domain.Aggregates.Review;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.Reviews.Domain.ValueObjects;
using ECommerce.SharedKernel;
using Xunit;

namespace ECommerce.Tests.Application.Reviews;

// Fake repository for testing
public class FakeReviewRepository : IReviewRepository
{
    private readonly Dictionary<Guid, Review> _reviews = new();

    public Task<Review?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_reviews.FirstOrDefault(r => r.Key == id).Value);

    public Task<Review?> GetByProductAndAuthorAsync(Guid productId, Guid authorId, CancellationToken ct = default)
        => Task.FromResult(_reviews.Values.FirstOrDefault(r => r.ProductId == productId && r.AuthorId == authorId));

    public async Task<(List<Review> Items, int TotalCount)> GetByProductAsync(
        Guid productId, int page, int pageSize, CancellationToken ct = default)
    {
        var items = _reviews.Values
            .Where(r => r.ProductId == productId && r.Status.ToString() == "Approved")
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var total = _reviews.Values.Count(r => r.ProductId == productId && r.Status.ToString() == "Approved");
        return (items, total);
    }

    public async Task<(List<Review> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? search, string? status, CancellationToken ct = default)
    {
        var query = _reviews.Values.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.Text.Value.Contains(search) ||
                                     (r.AuthorName ?? "").Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status.ToString() == status);
        }

        var total = query.Count();
        var items = query.OrderByDescending(r => r.CreatedAt)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

        return (items, total);
    }

    public async Task<(List<Review> Items, int TotalCount)> GetPendingAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var items = _reviews.Values
            .Where(r => r.Status.ToString() == "Pending")
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var total = _reviews.Values.Count(r => r.Status.ToString() == "Pending");
        return (items, total);
    }

    public async Task<(List<Review> Items, int TotalCount)> GetFlaggedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var items = _reviews.Values
            .Where(r => r.Status.ToString() == "Flagged")
            .OrderByDescending(r => r.FlagCount)
            .ThenByDescending(r => r.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var total = _reviews.Values.Count(r => r.Status.ToString() == "Flagged");
        return (items, total);
    }

    public Task UpsertAsync(Review review, CancellationToken ct = default)
    {
        _reviews[review.Id] = review;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Review review, CancellationToken ct = default)
    {
        _reviews.Remove(review.Id);
        return Task.CompletedTask;
    }

    public void Seed(Review review) => _reviews[review.Id] = review;
}

// Fake UnitOfWork for testing
public class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

public class CreateReviewCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesReviewAndReturns201()
    {
        var repo = new FakeReviewRepository();
        var uow = new FakeUnitOfWork();
        var handler = new CreateReviewCommandHandler(repo, uow);

        var cmd = new CreateReviewCommand(
            productId: Guid.NewGuid(),
            authorId: Guid.NewGuid(),
            authorName: "Alice",
            rating: 5,
            text: "Great product!");

        var result = await handler.Handle(cmd, default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(5, result.Value!.Rating);
        Assert.Equal("Great product!", result.Value.Text);
        Assert.Equal("Pending", result.Value.Status);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Handle_DuplicateReview_ReturnsFailed()
    {
        var productId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        var existingReview = Review.Create(
            productId, authorId, "Alice",
            Rating.Create(5).Value!,
            ReviewText.Create("Existing review").Value!)
            .Value!;

        var repo = new FakeReviewRepository();
        repo.Seed(existingReview);
        var uow = new FakeUnitOfWork();
        var handler = new CreateReviewCommandHandler(repo, uow);

        var cmd = new CreateReviewCommand(productId, authorId, "Alice", 4, "Another review");

        var result = await handler.Handle(cmd, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("DUPLICATE_REVIEW", result.Error!.Code);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task Handle_InvalidRating_ReturnsFailed()
    {
        var repo = new FakeReviewRepository();
        var uow = new FakeUnitOfWork();
        var handler = new CreateReviewCommandHandler(repo, uow);

        var cmd = new CreateReviewCommand(Guid.NewGuid(), Guid.NewGuid(), "Alice", 6, "Bad rating");

        var result = await handler.Handle(cmd, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_RATING", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_EmptyText_ReturnsFailed()
    {
        var repo = new FakeReviewRepository();
        var uow = new FakeUnitOfWork();
        var handler = new CreateReviewCommandHandler(repo, uow);

        var cmd = new CreateReviewCommand(Guid.NewGuid(), Guid.NewGuid(), "Alice", 5, "");

        var result = await handler.Handle(cmd, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("REVIEW_TEXT_EMPTY", result.Error!.Code);
    }
}

public class UpdateReviewCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidUpdate_UpdatesAndReturns200()
    {
        var review = Review.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Alice",
            Rating.Create(5).Value!,
            ReviewText.Create("Original review").Value!)
            .Value!;

        var repo = new FakeReviewRepository();
        repo.Seed(review);
        var uow = new FakeUnitOfWork();
        var handler = new UpdateReviewCommandHandler(repo, uow);

        var cmd = new UpdateReviewCommand(review.Id, 3, "Updated review");

        var result = await handler.Handle(cmd, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Rating);
        Assert.Equal("Updated review", result.Value.Text);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsFailed()
    {
        var repo = new FakeReviewRepository();
        var uow = new FakeUnitOfWork();
        var handler = new UpdateReviewCommandHandler(repo, uow);

        var cmd = new UpdateReviewCommand(Guid.NewGuid(), 4, "New text");

        var result = await handler.Handle(cmd, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("REVIEW_NOT_FOUND", result.Error!.Code);
    }
}

public class DeleteReviewCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidId_DeletesAndReturnsOk()
    {
        var review = Review.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Alice",
            Rating.Create(5).Value!,
            ReviewText.Create("To delete").Value!)
            .Value!;

        var repo = new FakeReviewRepository();
        repo.Seed(review);
        var uow = new FakeUnitOfWork();
        var handler = new DeleteReviewCommandHandler(repo, uow);

        var cmd = new DeleteReviewCommand(review.Id);

        var result = await handler.Handle(cmd, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsFailed()
    {
        var repo = new FakeReviewRepository();
        var uow = new FakeUnitOfWork();
        var handler = new DeleteReviewCommandHandler(repo, uow);

        var cmd = new DeleteReviewCommand(Guid.NewGuid());

        var result = await handler.Handle(cmd, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("REVIEW_NOT_FOUND", result.Error!.Code);
    }
}

public class ApproveReviewCommandHandlerTests
{
    [Fact]
    public async Task Handle_PendingReview_ApprovesAndRaisesEvent()
    {
        var review = Review.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Alice",
            Rating.Create(5).Value!,
            ReviewText.Create("Review").Value!)
            .Value!;

        var repo = new FakeReviewRepository();
        repo.Seed(review);
        var uow = new FakeUnitOfWork();
        var handler = new ApproveReviewCommandHandler(repo, uow);

        var cmd = new ApproveReviewCommand(review.Id);

        var result = await handler.Handle(cmd, default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Approved", (await repo.GetByIdAsync(review.Id))!.Status.ToString());
        Assert.Equal(1, uow.SaveCount);
    }
}

public class FlagReviewCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidId_FlagsAndIncrementsCount()
    {
        var review = Review.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Alice",
            Rating.Create(5).Value!,
            ReviewText.Create("Review").Value!)
            .Value!;

        var repo = new FakeReviewRepository();
        repo.Seed(review);
        var uow = new FakeUnitOfWork();
        var handler = new FlagReviewCommandHandler(repo, uow);

        var cmd = new FlagReviewCommand(review.Id, "Inappropriate");

        var result = await handler.Handle(cmd, default);

        Assert.True(result.IsSuccess);
        var updated = await repo.GetByIdAsync(review.Id);
        Assert.Equal(1, updated!.FlagCount);
        Assert.Equal(1, uow.SaveCount);
    }
}

public class MarkReviewHelpfulCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidId_IncrementsHelpfulCount()
    {
        var review = Review.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Alice",
            Rating.Create(5).Value!,
            ReviewText.Create("Review").Value!)
            .Value!;

        var repo = new FakeReviewRepository();
        repo.Seed(review);
        var uow = new FakeUnitOfWork();
        var handler = new MarkReviewHelpfulCommandHandler(repo, uow);

        var cmd = new MarkReviewHelpfulCommand(review.Id);

        var result = await handler.Handle(cmd, default);

        Assert.True(result.IsSuccess);
        var updated = await repo.GetByIdAsync(review.Id);
        Assert.Equal(1, updated!.HelpfulCount);
        Assert.Equal(1, uow.SaveCount);
    }
}

public class GetProductReviewsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyApprovedReviews()
    {
        var productId = Guid.NewGuid();

        // Create mix of approved and pending reviews
        var approvedReview = Review.Create(
            productId, Guid.NewGuid(), "Alice",
            Rating.Create(5).Value!,
            ReviewText.Create("Approved").Value!)
            .Value!;
        approvedReview.Approve();

        var pendingReview = Review.Create(
            productId, Guid.NewGuid(), "Bob",
            Rating.Create(4).Value!,
            ReviewText.Create("Pending").Value!)
            .Value!;

        var repo = new FakeReviewRepository();
        repo.Seed(approvedReview);
        repo.Seed(pendingReview);

        var handler = new GetProductReviewsQueryHandler(repo);
        var query = new GetProductReviewsQuery(productId, 1, 10);

        var result = await handler.Handle(query, default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Alice", result.Value.Items[0].AuthorName);
    }
}

public class GetPendingReviewsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPendingReviewsOnly()
    {
        var pending1 = Review.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Alice",
            Rating.Create(5).Value!,
            ReviewText.Create("Pending 1").Value!)
            .Value!;

        var approved = Review.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Bob",
            Rating.Create(4).Value!,
            ReviewText.Create("Approved").Value!)
            .Value!;
        approved.Approve();

        var repo = new FakeReviewRepository();
        repo.Seed(pending1);
        repo.Seed(approved);

        var handler = new GetPendingReviewsQueryHandler(repo);
        var query = new GetPendingReviewsQuery(1, 10);

        var result = await handler.Handle(query, default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Alice", result.Value.Items[0].AuthorName);
    }
}

public class GetFlaggedReviewsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsFlaggedReviewsOrderedByFlagCount()
    {
        var flagged1 = Review.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Alice",
            Rating.Create(5).Value!,
            ReviewText.Create("Flagged 1").Value!)
            .Value!;
        flagged1.Flag();
        flagged1.Flag();

        var flagged2 = Review.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Bob",
            Rating.Create(4).Value!,
            ReviewText.Create("Flagged 2").Value!)
            .Value!;
        flagged2.Flag();

        var repo = new FakeReviewRepository();
        repo.Seed(flagged1);
        repo.Seed(flagged2);

        var handler = new GetFlaggedReviewsQueryHandler(repo);
        var query = new GetFlaggedReviewsQuery(1, 10);

        var result = await handler.Handle(query, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
        // First should be the one with 2 flags
        Assert.Equal(2, result.Value.Items[0].FlagCount);
    }
}
```

---

## Key Test Scenarios

| Handler | Scenario | Expected Result |
|---------|----------|-----------------|
| CreateReview | Valid data | Returns 201 DTO, IsSuccess=true |
| CreateReview | Duplicate product+author | Returns DUPLICATE_REVIEW error |
| CreateReview | Invalid rating (6) | Returns INVALID_RATING error |
| UpdateReview | Valid update | Returns updated DTO, saves |
| UpdateReview | Unknown ID | Returns REVIEW_NOT_FOUND error |
| ApproveReview | Pending review | Changes status to Approved, raises event |
| FlagReview | Valid ID | Increments FlagCount |
| GetProductReviews | Mixed status | Returns approved only |
| GetPendingReviews | Admin list | Returns pending only |
| GetFlaggedReviews | Ordered by flags | Returns flagged ordered by flag count |

---

## Acceptance Criteria

- [ ] `FakeReviewRepository` implements all 8 methods for in-memory testing
- [ ] `FakeUnitOfWork` tracks SaveCount for transaction verification
- [ ] All command handler tests pass
- [ ] All query handler tests pass
- [ ] Duplicate review check works
- [ ] Invalid rating validation works
- [ ] Empty text validation works
- [ ] Approval state transitions work
- [ ] Flagging logic works
- [ ] Query filtering by status works
- [ ] No database access during handler tests (all in-memory)

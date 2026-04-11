using ECommerce.Reviews.Application.CommandHandlers;
using ECommerce.Reviews.Application.Commands;
using ECommerce.Reviews.Application.DTOs;
using ECommerce.Reviews.Application.DTOs.Common;
using ECommerce.Reviews.Application.Interfaces;
using ECommerce.Reviews.Application.QueryHandlers;
using ECommerce.Reviews.Application.Queries;
using ECommerce.Reviews.Domain.Aggregates.Review;
using ECommerce.Reviews.Domain.Enums;
using ECommerce.Reviews.Domain.Errors;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.Reviews.Domain.ValueObjects;
using ECommerce.SharedKernel.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Reviews.Tests.Application.Reviews;

[TestClass]
public class ReviewsHandlerTests
{
    [TestMethod]
    public async Task CreateReview_ValidRequest_CreatesReview()
    {
        var repository = new FakeReviewRepository();
        var catalog = new FakeCatalogService();
        var handler = new CreateReviewCommandHandler(repository, catalog);

        Guid productId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        catalog.AddProduct(productId);

        var result = await handler.Handle(
            new CreateReviewCommand(productId, userId, null, 5, "Nice", "Great product overall!"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.GetDataOrThrow().Rating.ShouldBe(5);
        repository.Reviews.Where(review => review.ProductId == productId && review.UserId == userId).ShouldHaveSingleItem();
    }

    [TestMethod]
    public async Task CreateReview_ProductMissing_ReturnsProductNotFound()
    {
        var repository = new FakeReviewRepository();
        var catalog = new FakeCatalogService();
        var handler = new CreateReviewCommandHandler(repository, catalog);

        var result = await handler.Handle(
            new CreateReviewCommand(Guid.NewGuid(), Guid.NewGuid(), null, 5, "Nice", "Great product overall!"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ReviewsErrors.ProductNotFound.Code);
    }

    [TestMethod]
    public async Task CreateReview_DuplicateReview_ReturnsDuplicateReview()
    {
        var repository = new FakeReviewRepository();
        var catalog = new FakeCatalogService();
        var handler = new CreateReviewCommandHandler(repository, catalog);

        Guid productId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        catalog.AddProduct(productId);
        repository.Seed(CreateReview(productId, userId));

        var result = await handler.Handle(
            new CreateReviewCommand(productId, userId, null, 4, "Nice", "Great product overall!"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ReviewsErrors.DuplicateReview.Code);
    }

    [TestMethod]
    public async Task UpdateReview_OwnerCanUpdate()
    {
        var repository = new FakeReviewRepository();
        var review = CreateReview();
        repository.Seed(review);
        var handler = new UpdateReviewCommandHandler(repository);

        var result = await handler.Handle(
            new UpdateReviewCommand(review.Id, review.UserId!.Value, false, 4, "Updated", "Updated review text"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.GetDataOrThrow().Rating.ShouldBe(4);
        result.GetDataOrThrow().Title.ShouldBe("Updated");
    }

    [TestMethod]
    public async Task UpdateReview_NonOwnerNonAdmin_ReturnsUnauthorized()
    {
        var repository = new FakeReviewRepository();
        var review = CreateReview();
        repository.Seed(review);
        var handler = new UpdateReviewCommandHandler(repository);

        var result = await handler.Handle(
            new UpdateReviewCommand(review.Id, Guid.NewGuid(), false, 4, "Updated", "Updated review text"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ReviewsErrors.Unauthorized.Code);
    }

    [TestMethod]
    public async Task UpdateReview_ApprovedReview_ReturnsReviewAlreadyApproved()
    {
        var repository = new FakeReviewRepository();
        var review = CreateReview();
        review.Approve(DateTime.UtcNow);
        repository.Seed(review);
        var handler = new UpdateReviewCommandHandler(repository);

        var result = await handler.Handle(
            new UpdateReviewCommand(review.Id, review.UserId!.Value, false, 4, "Updated", "Updated review text"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ReviewsErrors.ReviewAlreadyApproved.Code);
    }

    [TestMethod]
    public async Task DeleteReview_OwnerCanDelete()
    {
        var repository = new FakeReviewRepository();
        var review = CreateReview();
        repository.Seed(review);
        var handler = new DeleteReviewCommandHandler(repository);

        var result = await handler.Handle(
            new DeleteReviewCommand(review.Id, review.UserId!.Value, false),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        repository.Reviews.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task DeleteReview_NonOwnerNonAdmin_ReturnsUnauthorized()
    {
        var repository = new FakeReviewRepository();
        var review = CreateReview();
        repository.Seed(review);
        var handler = new DeleteReviewCommandHandler(repository);

        var result = await handler.Handle(
            new DeleteReviewCommand(review.Id, Guid.NewGuid(), false),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ReviewsErrors.Unauthorized.Code);
    }

    [TestMethod]
    public async Task GetReviewById_ReturnsReview()
    {
        var repository = new FakeReviewRepository();
        var review = CreateReview();
        repository.Seed(review);
        var handler = new GetReviewByIdQueryHandler(repository);

        var result = await handler.Handle(new GetReviewByIdQuery(review.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.GetDataOrThrow().Id.ShouldBe(review.Id);
    }

    [TestMethod]
    public async Task GetReviewById_MissingReview_ReturnsNotFound()
    {
        var repository = new FakeReviewRepository();
        var handler = new GetReviewByIdQueryHandler(repository);

        var result = await handler.Handle(new GetReviewByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ReviewsErrors.ReviewNotFound.Code);
    }

    [TestMethod]
    public async Task GetProductReviews_ReturnsApprovedReviewsOnly()
    {
        var repository = new FakeReviewRepository();
        var catalog = new FakeCatalogService();
        var productId = Guid.NewGuid();
        catalog.AddProduct(productId);

        var approved = CreateReview(productId, Guid.NewGuid(), approved: true, rating: 5, comment: "Great product overall!");
        var pending = CreateReview(productId, Guid.NewGuid(), approved: false, rating: 4, comment: "Another great product overall!");
        repository.Seed(approved);
        repository.Seed(pending);

        var handler = new GetProductReviewsQueryHandler(repository, catalog);
        var result = await handler.Handle(new GetProductReviewsQuery(productId, 1, 10), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var data = result.GetDataOrThrow();
        data.TotalCount.ShouldBe(1);
        data.Items.Where(item => item.Id == approved.Id).ShouldHaveSingleItem();
    }

    [TestMethod]
    public async Task GetProductAverageRating_ReturnsAverage()
    {
        var repository = new FakeReviewRepository();
        var catalog = new FakeCatalogService();
        var productId = Guid.NewGuid();
        catalog.AddProduct(productId);

        repository.Seed(CreateReview(productId, Guid.NewGuid(), approved: true, rating: 5, comment: "Great product overall!"));
        repository.Seed(CreateReview(productId, Guid.NewGuid(), approved: true, rating: 3, comment: "Good product overall!"));
        var handler = new GetProductAverageRatingQueryHandler(repository, catalog);

        var result = await handler.Handle(new GetProductAverageRatingQuery(productId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.GetDataOrThrow().ShouldBe(4m);
    }

    [TestMethod]
    public async Task GetPendingReviews_ReturnsPendingReviews()
    {
        var repository = new FakeReviewRepository();
        var pending = CreateReview();
        var approved = CreateReview(approved: true);
        repository.Seed(pending);
        repository.Seed(approved);

        var handler = new GetPendingReviewsQueryHandler(repository);
        var result = await handler.Handle(new GetPendingReviewsQuery(1, 10), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.GetDataOrThrow().TotalCount.ShouldBe(1);
        result.GetDataOrThrow().Items.Where(item => item.Id == pending.Id).ShouldHaveSingleItem();
    }

    [TestMethod]
    public async Task GetFlaggedReviews_ReturnsFlaggedReviews()
    {
        var repository = new FakeReviewRepository();
        var flagged = CreateReview(flagged: true);
        var approved = CreateReview(approved: true);
        repository.Seed(flagged);
        repository.Seed(approved);

        var handler = new GetFlaggedReviewsQueryHandler(repository);
        var result = await handler.Handle(new GetFlaggedReviewsQuery(1, 10), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.GetDataOrThrow().TotalCount.ShouldBe(1);
        result.GetDataOrThrow().Items.Where(item => item.Id == flagged.Id).ShouldHaveSingleItem();
    }

    [TestMethod]
    public async Task GetAllReviews_FiltersByStatusAndSearch()
    {
        var repository = new FakeReviewRepository();
        var matching = CreateReview(title: "Laptop", comment: "Great laptop overall!", approved: true);
        var nonMatching = CreateReview(title: "Phone", comment: "Solid phone overall!", approved: false);
        repository.Seed(matching);
        repository.Seed(nonMatching);

        var handler = new GetAllReviewsQueryHandler(repository);
        var result = await handler.Handle(new GetAllReviewsQuery(1, 10, "Laptop", "Approved"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.GetDataOrThrow().TotalCount.ShouldBe(1);
        result.GetDataOrThrow().Items.Where(item => item.Id == matching.Id).ShouldHaveSingleItem();
    }

    [TestMethod]
    public async Task GetUserReviews_ReturnsOnlyUserReviews()
    {
        var repository = new FakeReviewRepository();
        Guid userId = Guid.NewGuid();
        var userReview = CreateReview(userId: userId, comment: "Great product overall!");
        var otherReview = CreateReview(userId: Guid.NewGuid(), comment: "Another great product overall!");
        repository.Seed(userReview);
        repository.Seed(otherReview);

        var handler = new GetUserReviewsQueryHandler(repository);
        var result = await handler.Handle(new GetUserReviewsQuery(userId, 1, 10), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.GetDataOrThrow().TotalCount.ShouldBe(1);
        result.GetDataOrThrow().Items.Where(item => item.UserId == userId).ShouldHaveSingleItem();
    }

    private static Review CreateReview(
        Guid? productId = null,
        Guid? userId = null,
        bool approved = false,
        bool flagged = false,
        int rating = 5,
        string title = "Nice",
        string comment = "Great product overall!")
    {
        Rating ratingValue = Rating.Create(rating).GetDataOrThrow();
        ReviewContent content = ReviewContent.Create(title, comment).GetDataOrThrow();
        Review review = Review.Create(productId ?? Guid.NewGuid(), userId ?? Guid.NewGuid(), ratingValue, content, null);

        if (approved)
            review.Approve(DateTime.UtcNow.AddMinutes(-10));

        if (flagged)
            review.Flag(DateTime.UtcNow.AddMinutes(-5));

        return review;
    }

    private sealed class FakeReviewRepository : IReviewRepository
    {
        private readonly Dictionary<Guid, Review> _reviews = new();
        public IReadOnlyCollection<Review> Reviews => _reviews.Values.ToList().AsReadOnly();

        public Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_reviews.TryGetValue(id, out Review? review) ? review : null);

        public Task<Review?> GetByProductAndAuthorAsync(Guid productId, Guid authorId, CancellationToken cancellationToken = default)
            => Task.FromResult(_reviews.Values.FirstOrDefault(review => review.ProductId == productId && review.UserId == authorId));

        public Task<(IReadOnlyList<Review> Items, int TotalCount)> GetByProductAsync(Guid productId, int page, int pageSize, bool onlyApproved = true, CancellationToken cancellationToken = default)
        {
            IQueryable<Review> query = _reviews.Values.AsQueryable().Where(review => review.ProductId == productId);
            if (onlyApproved)
                query = query.Where(review => review.Status == ReviewStatus.Approved);

            query = query.OrderByDescending(review => review.CreatedAt);
            int totalCount = query.Count();
            IReadOnlyList<Review> items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((items, totalCount));
        }

        public Task<(IReadOnlyList<Review> Items, int TotalCount)> GetByUserAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<Review> query = _reviews.Values.AsQueryable().Where(review => review.UserId == userId).OrderByDescending(review => review.CreatedAt);
            int totalCount = query.Count();
            IReadOnlyList<Review> items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((items, totalCount));
        }

        public Task<(IReadOnlyList<Review> Items, int TotalCount)> GetAllAsync(int page, int pageSize, string? search, string? status, CancellationToken cancellationToken = default)
        {
            IQueryable<Review> query = _reviews.Values.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(review =>
                    (review.Content.Title ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    review.Content.Body.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReviewStatus>(status, true, out ReviewStatus parsedStatus))
                query = query.Where(review => review.Status == parsedStatus);

            query = query.OrderByDescending(review => review.CreatedAt);
            int totalCount = query.Count();
            IReadOnlyList<Review> items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((items, totalCount));
        }

        public Task<(IReadOnlyList<Review> Items, int TotalCount)> GetPendingAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<Review> query = _reviews.Values.AsQueryable().Where(review => review.Status == ReviewStatus.Pending).OrderByDescending(review => review.CreatedAt);
            int totalCount = query.Count();
            IReadOnlyList<Review> items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((items, totalCount));
        }

        public Task<(IReadOnlyList<Review> Items, int TotalCount)> GetFlaggedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<Review> query = _reviews.Values.AsQueryable().Where(review => review.Status == ReviewStatus.Flagged).OrderByDescending(review => review.FlagCount).ThenByDescending(review => review.UpdatedAt);
            int totalCount = query.Count();
            IReadOnlyList<Review> items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((items, totalCount));
        }

        public Task<decimal> GetAverageRatingAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            decimal average = _reviews.Values
                .Where(review => review.ProductId == productId && review.Status == ReviewStatus.Approved)
                .Select(review => (decimal)review.Rating.Value)
                .DefaultIfEmpty(0m)
                .Average();

            return Task.FromResult(average);
        }

        public Task<bool> ExistsAsync(Guid productId, Guid authorId, CancellationToken cancellationToken = default)
            => Task.FromResult(_reviews.Values.Any(review => review.ProductId == productId && review.UserId == authorId));

        public Task AddAsync(Review review, CancellationToken cancellationToken = default)
        {
            _reviews[review.Id] = review;
            return Task.CompletedTask;
        }

        public Task UpsertAsync(Review review, CancellationToken cancellationToken = default)
        {
            _reviews[review.Id] = review;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Review review, CancellationToken cancellationToken = default)
        {
            _reviews.Remove(review.Id);
            return Task.CompletedTask;
        }

        public void Seed(Review review) => _reviews[review.Id] = review;
    }

    private sealed class FakeCatalogService : ICatalogService
    {
        private readonly HashSet<Guid> _productIds = new();

        public void AddProduct(Guid productId) => _productIds.Add(productId);

        public Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(_productIds.Contains(productId));
    }

}

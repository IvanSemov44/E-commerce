using ECommerce.Core.Entities;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Repositories;

/// <summary>
/// Tests for the ReviewRepository class.
/// Tests review-specific repository operations including ratings and approval.
/// </summary>
[TestClass]
public class ReviewRepositoryTests
{
    private AppDbContext _context = null!;
    private ReviewRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new ReviewRepository(_context);

        SeedTestData();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private void SeedTestData()
    {
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Email = "user1@test.com", FirstName = "John", LastName = "Doe" },
            new() { Id = Guid.NewGuid(), Email = "user2@test.com", FirstName = "Jane", LastName = "Smith" },
            new() { Id = Guid.NewGuid(), Email = "user3@test.com", FirstName = "Bob", LastName = "Wilson" }
        };

        _context.Users.AddRange(users);

        var category = new Category { Id = Guid.NewGuid(), Name = "Electronics", Slug = "electronics" };
        _context.Categories.Add(category);

        var products = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Laptop",
                Slug = "laptop",
                Sku = "LAPTOP-001",
                Price = 999.99m,
                StockQuantity = 10,
                CategoryId = category.Id,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Phone",
                Slug = "phone",
                Sku = "PHONE-001",
                Price = 599.99m,
                StockQuantity = 20,
                CategoryId = category.Id,
                IsActive = true
            }
        };

        _context.Products.AddRange(products);

        var reviews = new List<Review>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = products[0].Id,
                UserId = users[0].Id,
                Rating = 5,
                Comment = "Excellent laptop!",
                IsApproved = true,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = products[0].Id,
                UserId = users[1].Id,
                Rating = 4,
                Comment = "Good laptop, but expensive",
                IsApproved = true,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = products[0].Id,
                UserId = users[2].Id,
                Rating = 3,
                Comment = "Average product",
                IsApproved = false, // Pending approval
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = products[1].Id,
                UserId = users[0].Id,
                Rating = 5,
                Comment = "Amazing phone!",
                IsApproved = true,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = products[1].Id,
                UserId = users[1].Id,
                Rating = 2,
                Comment = "Not worth the price",
                IsApproved = false, // Pending approval
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _context.Reviews.AddRange(reviews);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    #region GetByProductIdAsync Tests

    [TestMethod]
    public async Task GetByProductIdAsync_ReturnsApprovedReviews()
    {
        // Arrange
        var laptop = await _context.Products.FirstAsync(p => p.Slug == "laptop");

        // Act
        var result = await _repository.GetByProductIdAsync(laptop.Id, onlyApproved: true);

        // Assert
        result.Should().HaveCount(2);
        result.All(r => r.IsApproved).Should().BeTrue();
    }

    [TestMethod]
    public async Task GetByProductIdAsync_WithUnapproved_ReturnsAllReviews()
    {
        // Arrange
        var laptop = await _context.Products.FirstAsync(p => p.Slug == "laptop");

        // Act
        var result = await _repository.GetByProductIdAsync(laptop.Id, onlyApproved: false);

        // Assert
        result.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task GetByProductIdAsync_IncludesUser()
    {
        // Arrange
        var laptop = await _context.Products.FirstAsync(p => p.Slug == "laptop");

        // Act
        var result = await _repository.GetByProductIdAsync(laptop.Id);

        // Assert
        result.Should().AllSatisfy(r => r.User.Should().NotBeNull());
    }

    [TestMethod]
    public async Task GetByProductIdAsync_ReturnsInDescendingOrder()
    {
        // Arrange
        var laptop = await _context.Products.FirstAsync(p => p.Slug == "laptop");

        // Act
        var result = await _repository.GetByProductIdAsync(laptop.Id);

        // Assert
        result.Should().BeInDescendingOrder(r => r.CreatedAt);
    }

    [TestMethod]
    public async Task GetByProductIdAsync_ProductWithNoReviews_ReturnsEmpty()
    {
        // Arrange
        var newProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "New Product",
            Slug = "new-product",
            Sku = "NEW-001",
            Price = 99.99m,
            StockQuantity = 5,
            CategoryId = (await _context.Categories.FirstAsync()).Id,
            IsActive = true
        };
        _context.Products.Add(newProduct);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByProductIdAsync(newProduct.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetByProductIdAsync_WithTracking_TracksEntities()
    {
        // Arrange
        var laptop = await _context.Products.FirstAsync(p => p.Slug == "laptop");

        // Act
        var result = await _repository.GetByProductIdAsync(laptop.Id, onlyApproved: true, trackChanges: true);

        // Assert
        foreach (var review in result)
        {
            _context.ChangeTracker.Entries<Review>().Should().Contain(e => e.Entity.Id == review.Id);
        }
    }

    [TestMethod]
    public async Task GetByProductIdAsync_WithoutTracking_DoesNotTrackEntities()
    {
        // Arrange
        var laptop = await _context.Products.FirstAsync(p => p.Slug == "laptop");

        // Act
        var result = await _repository.GetByProductIdAsync(laptop.Id, onlyApproved: true, trackChanges: false);

        // Assert
        foreach (var review in result)
        {
            _context.ChangeTracker.Entries<Review>().Should().NotContain(e => e.Entity.Id == review.Id);
        }
    }

    #endregion

    #region GetByUserIdAsync Tests

    [TestMethod]
    public async Task GetByUserIdAsync_ReturnsUserReviews()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetByUserIdAsync(user1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.All(r => r.UserId == user1.Id).Should().BeTrue();
    }

    [TestMethod]
    public async Task GetByUserIdAsync_IncludesProduct()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetByUserIdAsync(user1.Id);

        // Assert
        result.Should().AllSatisfy(r => r.Product.Should().NotBeNull());
    }

    [TestMethod]
    public async Task GetByUserIdAsync_ReturnsInDescendingOrder()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetByUserIdAsync(user1.Id);

        // Assert
        result.Should().BeInDescendingOrder(r => r.CreatedAt);
    }

    [TestMethod]
    public async Task GetByUserIdAsync_UserWithNoReviews_ReturnsEmpty()
    {
        // Arrange
        var newUser = new User { Id = Guid.NewGuid(), Email = "new@test.com", FirstName = "New", LastName = "User" };
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(newUser.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetByUserIdAsync_WithTracking_TracksEntities()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetByUserIdAsync(user1.Id, trackChanges: true);

        // Assert
        foreach (var review in result)
        {
            _context.ChangeTracker.Entries<Review>().Should().Contain(e => e.Entity.Id == review.Id);
        }
    }

    #endregion

    #region GetByIdWithDetailsAsync Tests

    [TestMethod]
    public async Task GetByIdWithDetailsAsync_ExistingReview_ReturnsReviewWithDetails()
    {
        // Arrange
        var review = await _context.Reviews.FirstAsync();

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(review.Id);

        // Assert
        result.Should().NotBeNull();
        result!.User.Should().NotBeNull();
        result.Product.Should().NotBeNull();
    }

    [TestMethod]
    public async Task GetByIdWithDetailsAsync_NonExistingReview_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdWithDetailsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetByIdWithDetailsAsync_WithTracking_TracksEntity()
    {
        // Arrange
        var review = await _context.Reviews.FirstAsync();

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(review.Id, trackChanges: true);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<Review>().Should().Contain(e => e.Entity.Id == result!.Id);
    }

    [TestMethod]
    public async Task GetByIdWithDetailsAsync_WithoutTracking_DoesNotTrackEntity()
    {
        // Arrange
        var review = await _context.Reviews.AsNoTracking().FirstAsync();

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(review.Id, trackChanges: false);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<Review>().Should().NotContain(e => e.Entity.Id == result!.Id);
    }

    #endregion

    #region GetApprovedReviewCountAsync Tests

    [TestMethod]
    public async Task GetApprovedReviewCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var laptop = await _context.Products.FirstAsync(p => p.Slug == "laptop");

        // Act
        var result = await _repository.GetApprovedReviewCountAsync(laptop.Id);

        // Assert
        result.Should().Be(2);
    }

    [TestMethod]
    public async Task GetApprovedReviewCountAsync_ExcludesUnapproved()
    {
        // Arrange
        var laptop = await _context.Products.FirstAsync(p => p.Slug == "laptop");
        var totalReviews = await _context.Reviews.CountAsync(r => r.ProductId == laptop.Id);

        // Act
        var result = await _repository.GetApprovedReviewCountAsync(laptop.Id);

        // Assert
        result.Should().BeLessThan(totalReviews);
    }

    [TestMethod]
    public async Task GetApprovedReviewCountAsync_ProductWithNoReviews_ReturnsZero()
    {
        // Act
        var result = await _repository.GetApprovedReviewCountAsync(Guid.NewGuid());

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region GetAverageRatingAsync Tests

    [TestMethod]
    public async Task GetAverageRatingAsync_ReturnsCorrectAverage()
    {
        // Arrange
        var laptop = await _context.Products.FirstAsync(p => p.Slug == "laptop");
        // Approved reviews: 5 and 4, average = 4.5

        // Act
        var result = await _repository.GetAverageRatingAsync(laptop.Id);

        // Assert
        result.Should().Be(4.5m);
    }

    [TestMethod]
    public async Task GetAverageRatingAsync_ExcludesUnapproved()
    {
        // Arrange
        var laptop = await _context.Products.FirstAsync(p => p.Slug == "laptop");

        // Act
        var result = await _repository.GetAverageRatingAsync(laptop.Id);

        // Assert - Should not include the unapproved rating of 3
        result.Should().Be(4.5m);
    }

    [TestMethod]
    public async Task GetAverageRatingAsync_ProductWithNoReviews_ReturnsZero()
    {
        // Act
        var result = await _repository.GetAverageRatingAsync(Guid.NewGuid());

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region UserHasReviewedAsync Tests

    [TestMethod]
    public async Task UserHasReviewedAsync_UserHasReviewed_ReturnsTrue()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");
        var laptop = await _context.Products.FirstAsync(p => p.Slug == "laptop");

        // Act
        var result = await _repository.UserHasReviewedAsync(user1.Id, laptop.Id);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task UserHasReviewedAsync_UserHasNotReviewed_ReturnsFalse()
    {
        // Arrange
        var user2 = await _context.Users.FirstAsync(u => u.Email == "user3@test.com");
        var phone = await _context.Products.FirstAsync(p => p.Slug == "phone");

        // Act
        var result = await _repository.UserHasReviewedAsync(user2.Id, phone.Id);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task UserHasReviewedAsync_NonExistingUser_ReturnsFalse()
    {
        // Arrange
        var laptop = await _context.Products.FirstAsync(p => p.Slug == "laptop");

        // Act
        var result = await _repository.UserHasReviewedAsync(Guid.NewGuid(), laptop.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetPendingApprovalAsync Tests

    [TestMethod]
    public async Task GetPendingApprovalAsync_ReturnsUnapprovedReviews()
    {
        // Act
        var result = await _repository.GetPendingApprovalAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(r => !r.IsApproved).Should().BeTrue();
    }

    [TestMethod]
    public async Task GetPendingApprovalAsync_IncludesUserAndProduct()
    {
        // Act
        var result = await _repository.GetPendingApprovalAsync();

        // Assert
        result.Should().AllSatisfy(r =>
        {
            r.User.Should().NotBeNull();
            r.Product.Should().NotBeNull();
        });
    }

    [TestMethod]
    public async Task GetPendingApprovalAsync_ReturnsInAscendingOrder()
    {
        // Act
        var result = await _repository.GetPendingApprovalAsync();

        // Assert
        result.Should().BeInAscendingOrder(r => r.CreatedAt);
    }

    [TestMethod]
    public async Task GetPendingApprovalAsync_WithTracking_TracksEntities()
    {
        // Act
        var result = await _repository.GetPendingApprovalAsync(trackChanges: true);

        // Assert
        foreach (var review in result)
        {
            _context.ChangeTracker.Entries<Review>().Should().Contain(e => e.Entity.Id == review.Id);
        }
    }

    [TestMethod]
    public async Task GetPendingApprovalAsync_WithoutTracking_DoesNotTrackEntities()
    {
        // Act
        var result = await _repository.GetPendingApprovalAsync(trackChanges: false);

        // Assert
        foreach (var review in result)
        {
            _context.ChangeTracker.Entries<Review>().Should().NotContain(e => e.Entity.Id == review.Id);
        }
    }

    #endregion

    #region Base Repository Tests

    [TestMethod]
    public async Task GetByIdAsync_ExistingReview_ReturnsReview()
    {
        // Arrange
        var review = await _context.Reviews.FirstAsync();

        // Act
        var result = await _repository.GetByIdAsync(review.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(review.Id);
    }

    [TestMethod]
    public async Task GetByIdAsync_NonExistingReview_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsAllReviews()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(5);
    }

    [TestMethod]
    public async Task AddAsync_AddsReviewToDatabase()
    {
        // Arrange
        var user = await _context.Users.FirstAsync();
        var product = await _context.Products.FirstAsync();
        var newReview = new Review
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            UserId = user.Id,
            Rating = 4,
            Comment = "New review",
            IsApproved = false
        };

        // Act
        await _repository.AddAsync(newReview);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(newReview.Id);
        result.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Delete_RemovesReviewFromDatabase()
    {
        // Arrange
        var review = await _context.Reviews.FirstAsync(r => !r.IsApproved);

        // Act
        _repository.Delete(review);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(review.Id);
        result.Should().BeNull();
    }

    #endregion
}

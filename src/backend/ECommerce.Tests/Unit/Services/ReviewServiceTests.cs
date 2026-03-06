using AutoMapper;
using ECommerce.Application.DTOs.Reviews;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Services;
using ECommerce.Core.Constants;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Results;
using ECommerce.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Unit.Services;

[TestClass]
public class ReviewServiceTests
{
    private Mock<IReviewRepository> _mockReviewRepository = null!;
    private Mock<IProductRepository> _mockProductRepository = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IMapper> _mockMapper = null!;
    private Mock<ILogger<ReviewService>> _mockLogger = null!;
    private ReviewService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockReviewRepository = new Mock<IReviewRepository>();
        _mockProductRepository = new Mock<IProductRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = MockHelpers.CreateMockMapper();
        _mockLogger = new Mock<ILogger<ReviewService>>();

        _mockUnitOfWork.Setup(u => u.Reviews).Returns(_mockReviewRepository.Object);
        _mockUnitOfWork.Setup(u => u.Products).Returns(_mockProductRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

        _service = new ReviewService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    [TestMethod]
    public async Task GetProductReviewsAsync_ExistingProduct_ReturnsReviews()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct();
        var reviews = new List<Review>
        {
            TestDataFactory.CreateReview(Guid.NewGuid(), product.Id),
            TestDataFactory.CreateReview(Guid.NewGuid(), product.Id)
        };

        _mockProductRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<bool>())).ReturnsAsync(product);
        _mockReviewRepository.Setup(r => r.GetByProductIdAsync(product.Id, true)).ReturnsAsync(reviews);
        _mockMapper.Setup(m => m.Map<IEnumerable<ReviewDto>>(It.IsAny<IEnumerable<Review>>()))
            .Returns((IEnumerable<Review> src) => src.Select(r => new ReviewDto { Id = r.Id, Rating = r.Rating }).ToList());

        // Act
        var result = await _service.GetProductReviewsAsync(product.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<IEnumerable<ReviewDto>>.Success success)
        {
            success.Data.Should().HaveCount(2);
        }
        else
        {
            Assert.Fail("Expected Result<IEnumerable<ReviewDto>>.Success");
        }
    }

    [TestMethod]
    public async Task GetProductReviewsAsync_NonExistentProduct_ThrowsProductNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockProductRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<bool>())).ReturnsAsync((Product?)null);

        // Act
        var result = await _service.GetProductReviewsAsync(id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<IEnumerable<ReviewDto>>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.ProductNotFound);
        }
        else
        {
            Assert.Fail("Expected Result<IEnumerable<ReviewDto>>.Failure");
        }
    }

    [TestMethod]
    public async Task GetReviewByIdAsync_Existing_ReturnsReview()
    {
        // Arrange
        var review = TestDataFactory.CreateReview(Guid.NewGuid(), Guid.NewGuid());
        _mockReviewRepository.Setup(r => r.GetByIdWithDetailsAsync(review.Id)).ReturnsAsync(review);
        _mockMapper.Setup(m => m.Map<ReviewDetailDto>(It.IsAny<Review>()))
            .Returns((Review r) => new ReviewDetailDto { Id = r.Id, Rating = r.Rating, Comment = r.Comment });

        // Act
        var result = await _service.GetReviewByIdAsync(review.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<ReviewDetailDto>.Success success)
        {
            success.Data.Id.Should().Be(review.Id);
        }
        else
        {
            Assert.Fail("Expected Result<ReviewDetailDto>.Success");
        }
    }

    [TestMethod]
    public async Task GetReviewByIdAsync_NonExistent_ThrowsReviewNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockReviewRepository.Setup(r => r.GetByIdWithDetailsAsync(id)).ReturnsAsync((Review?)null);

        // Act
        var result = await _service.GetReviewByIdAsync(id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<ReviewDetailDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.ReviewNotFound);
        }
        else
        {
            Assert.Fail("Expected Result<ReviewDetailDto>.Failure");
        }
    }

    [TestMethod]
    public async Task CreateReviewAsync_ValidData_CreatesReview()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var product = TestDataFactory.CreateProduct();
        var dto = new CreateReviewDto { ProductId = product.Id, Rating = 5, Comment = "Great", Title = "Nice" };

        _mockProductRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<bool>())).ReturnsAsync(product);
        _mockUserRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<bool>())).ReturnsAsync(user);
        _mockReviewRepository.Setup(r => r.UserHasReviewedAsync(user.Id, product.Id)).ReturnsAsync(false);
        _mockReviewRepository.Setup(r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
            .Callback<Review, CancellationToken>((r, _) => { if (r.Id == Guid.Empty) r.Id = Guid.NewGuid(); })
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockMapper.Setup(m => m.Map<ReviewDetailDto>(It.IsAny<Review>()))
            .Returns((Review r) => new ReviewDetailDto { Id = r.Id, Rating = r.Rating, Comment = r.Comment });

        // Act
        var result = await _service.CreateReviewAsync(user.Id, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<ReviewDetailDto>.Success success)
        {
            success.Data.Rating.Should().Be(5);
        }
        else
        {
            Assert.Fail("Expected Result<ReviewDetailDto>.Success");
        }
        _mockReviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task CreateReviewAsync_DuplicateReview_ThrowsDuplicateReviewException()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var product = TestDataFactory.CreateProduct();
        var dto = new CreateReviewDto { ProductId = product.Id, Rating = 4, Comment = "ok" };

        _mockProductRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<bool>())).ReturnsAsync(product);
        _mockUserRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<bool>())).ReturnsAsync(user);
        _mockReviewRepository.Setup(r => r.UserHasReviewedAsync(user.Id, product.Id)).ReturnsAsync(true);

        // Act
        var result = await _service.CreateReviewAsync(user.Id, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<ReviewDetailDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.DuplicateReview);
        }
        else
        {
            Assert.Fail("Expected Result<ReviewDetailDto>.Failure");
        }
    }

    [TestMethod]
    public async Task CreateReviewAsync_InvalidRating_ReturnsFailure()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var dto = new CreateReviewDto { ProductId = Guid.NewGuid(), Rating = 10, Comment = "bad" };

        // Act
        var result = await _service.CreateReviewAsync(user.Id, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<ReviewDetailDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.InvalidRating);
        }
        else
        {
            Assert.Fail("Expected Result<ReviewDetailDto>.Failure");
        }
    }

    [TestMethod]
    public async Task CreateReviewAsync_EmptyComment_ThrowsEmptyReviewCommentException()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var dto = new CreateReviewDto { ProductId = Guid.NewGuid(), Rating = 4, Comment = "   " };

        // Act
        var result = await _service.CreateReviewAsync(user.Id, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<ReviewDetailDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.EmptyReviewComment);
        }
        else
        {
            Assert.Fail("Expected Result<ReviewDetailDto>.Failure");
        }
    }

    [TestMethod]
    public async Task UpdateReviewAsync_ValidData_UpdatesReview()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var review = TestDataFactory.CreateReview(user.Id, Guid.NewGuid());
        review.CreatedAt = DateTime.UtcNow;
        var dto = new UpdateReviewDto { Rating = 4, Comment = "Updated" };

        _mockReviewRepository.Setup(r => r.GetByIdWithDetailsAsync(review.Id)).ReturnsAsync(review);
        _mockReviewRepository.Setup(r => r.UpdateAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockMapper.Setup(m => m.Map<ReviewDetailDto>(It.IsAny<Review>())).Returns((Review r) => new ReviewDetailDto { Id = r.Id, Rating = r.Rating, Comment = r.Comment });

        // Act
        var result = await _service.UpdateReviewAsync(user.Id, review.Id, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<ReviewDetailDto>.Success success)
        {
            success.Data.Rating.Should().Be(4);
        }
        else
        {
            Assert.Fail("Expected Result<ReviewDetailDto>.Success");
        }
        _mockReviewRepository.Verify(r => r.UpdateAsync(It.IsAny<Review>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateReviewAsync_NotOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var owner = TestDataFactory.CreateUser();
        var other = TestDataFactory.CreateUser();
        var review = TestDataFactory.CreateReview(owner.Id, Guid.NewGuid());

        _mockReviewRepository.Setup(r => r.GetByIdWithDetailsAsync(review.Id)).ReturnsAsync(review);

        // Act
        var result = await _service.UpdateReviewAsync(other.Id, review.Id, new UpdateReviewDto { Comment = "x" });

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<ReviewDetailDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.Unauthorized);
        }
        else
        {
            Assert.Fail("Expected Result<ReviewDetailDto>.Failure");
        }
    }

    [TestMethod]
    public async Task UpdateReviewAsync_ExpiredTime_ReturnsFailure()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var review = TestDataFactory.CreateReview(user.Id, Guid.NewGuid());
        review.CreatedAt = DateTime.UtcNow.AddDays(-2);

        _mockReviewRepository.Setup(r => r.GetByIdWithDetailsAsync(review.Id)).ReturnsAsync(review);

        // Act
        var result = await _service.UpdateReviewAsync(user.Id, review.Id, new UpdateReviewDto { Comment = "x" });

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<ReviewDetailDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.ReviewUpdateExpired);
        }
        else
        {
            Assert.Fail("Expected Result<ReviewDetailDto>.Failure");
        }
    }

    [TestMethod]
    public async Task DeleteReviewAsync_Existing_RemovesReview()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var review = TestDataFactory.CreateReview(user.Id, Guid.NewGuid());

        _mockReviewRepository.Setup(r => r.GetByIdAsync(review.Id, It.IsAny<bool>())).ReturnsAsync(review);
        _mockReviewRepository.Setup(r => r.DeleteAsync(review, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _service.DeleteReviewAsync(user.Id, review.Id);

        // Assert
        _mockReviewRepository.Verify(r => r.DeleteAsync(It.IsAny<Review>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task DeleteReviewAsync_NotOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var owner = TestDataFactory.CreateUser();
        var other = TestDataFactory.CreateUser();
        var review = TestDataFactory.CreateReview(owner.Id, Guid.NewGuid());

        _mockReviewRepository.Setup(r => r.GetByIdAsync(review.Id, It.IsAny<bool>())).ReturnsAsync(review);

        // Act
        var result = await _service.DeleteReviewAsync(other.Id, review.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<ECommerce.Core.Results.Unit>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.Unauthorized);
        }
        else
        {
            Assert.Fail("Expected Result<Unit>.Failure");
        }
    }

    [TestMethod]
    public async Task GetPendingReviewsAsync_ReturnsPending()
    {
        // Arrange
        var pending = new List<Review>
        {
            TestDataFactory.CreateReview(Guid.NewGuid(), Guid.NewGuid()),
            TestDataFactory.CreateReview(Guid.NewGuid(), Guid.NewGuid())
        };

        _mockReviewRepository.Setup(r => r.GetPendingApprovalAsync()).ReturnsAsync(pending);
        _mockMapper.Setup(m => m.Map<IEnumerable<ReviewDetailDto>>(It.IsAny<IEnumerable<Review>>()))
            .Returns((IEnumerable<Review> src) => src.Select(r => new ReviewDetailDto { Id = r.Id }).ToList());

        // Act
        var result = await _service.GetPendingReviewsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetProductAverageRatingAsync_ReturnsAverage()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockReviewRepository.Setup(r => r.GetAverageRatingAsync(productId)).ReturnsAsync(4.25m);

        // Act
        var result = await _service.GetProductAverageRatingAsync(productId);

        // Assert
        result.Should().Be(4.25m);
    }
}

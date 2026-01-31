using AutoMapper;
using ECommerce.Application.DTOs.Reviews;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Services;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Tests.Helpers;
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
    private ReviewService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockReviewRepository = new Mock<IReviewRepository>();
        _mockProductRepository = new Mock<IProductRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = MockHelpers.CreateMockMapper();

        _mockUnitOfWork.Setup(u => u.Reviews).Returns(_mockReviewRepository.Object);
        _mockUnitOfWork.Setup(u => u.Products).Returns(_mockProductRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

        _service = new ReviewService(
            _mockUnitOfWork.Object,
            _mockMapper.Object);
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
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetProductReviewsAsync_NonExistentProduct_ThrowsProductNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockProductRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<bool>())).ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _service.GetProductReviewsAsync(id);

        // Assert
        await act.Should().ThrowAsync<ProductNotFoundException>();
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
        result.Should().NotBeNull();
        result.Id.Should().Be(review.Id);
    }

    [TestMethod]
    public async Task GetReviewByIdAsync_NonExistent_ThrowsReviewNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockReviewRepository.Setup(r => r.GetByIdWithDetailsAsync(id)).ReturnsAsync((Review?)null);

        // Act
        Func<Task> act = async () => await _service.GetReviewByIdAsync(id);

        // Assert
        await act.Should().ThrowAsync<ReviewNotFoundException>();
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
        _mockReviewRepository.Setup(r => r.AddAsync(It.IsAny<Review>()))
            .Callback<Review>(r => { if (r.Id == Guid.Empty) r.Id = Guid.NewGuid(); })
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _mockMapper.Setup(m => m.Map<ReviewDetailDto>(It.IsAny<Review>()))
            .Returns((Review r) => new ReviewDetailDto { Id = r.Id, Rating = r.Rating, Comment = r.Comment });

        // Act
        var result = await _service.CreateReviewAsync(user.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Rating.Should().Be(5);
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
        Func<Task> act = async () => await _service.CreateReviewAsync(user.Id, dto);

        // Assert
        await act.Should().ThrowAsync<DuplicateReviewException>();
    }

    [TestMethod]
    public async Task CreateReviewAsync_InvalidRating_ThrowsInvalidRatingException()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var dto = new CreateReviewDto { ProductId = Guid.NewGuid(), Rating = 10, Comment = "bad" };

        // Act
        Func<Task> act = async () => await _service.CreateReviewAsync(user.Id, dto);

        // Assert
        await act.Should().ThrowAsync<InvalidRatingException>();
    }

    [TestMethod]
    public async Task CreateReviewAsync_EmptyComment_ThrowsEmptyReviewCommentException()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var dto = new CreateReviewDto { ProductId = Guid.NewGuid(), Rating = 4, Comment = "   " };

        // Act
        Func<Task> act = async () => await _service.CreateReviewAsync(user.Id, dto);

        // Assert
        await act.Should().ThrowAsync<EmptyReviewCommentException>();
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
        _mockReviewRepository.Setup(r => r.UpdateAsync(It.IsAny<Review>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _mockMapper.Setup(m => m.Map<ReviewDetailDto>(It.IsAny<Review>())).Returns((Review r) => new ReviewDetailDto { Id = r.Id, Rating = r.Rating, Comment = r.Comment });

        // Act
        var result = await _service.UpdateReviewAsync(user.Id, review.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Rating.Should().Be(4);
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
        Func<Task> act = async () => await _service.UpdateReviewAsync(other.Id, review.Id, new UpdateReviewDto { Comment = "x" });

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestMethod]
    public async Task UpdateReviewAsync_ExpiredTime_ThrowsReviewUpdateTimeExpiredException()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var review = TestDataFactory.CreateReview(user.Id, Guid.NewGuid());
        review.CreatedAt = DateTime.UtcNow.AddDays(-2);

        _mockReviewRepository.Setup(r => r.GetByIdWithDetailsAsync(review.Id)).ReturnsAsync(review);

        // Act
        Func<Task> act = async () => await _service.UpdateReviewAsync(user.Id, review.Id, new UpdateReviewDto { Comment = "x" });

        // Assert
        await act.Should().ThrowAsync<ReviewUpdateTimeExpiredException>();
    }

    [TestMethod]
    public async Task DeleteReviewAsync_Existing_RemovesReview()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var review = TestDataFactory.CreateReview(user.Id, Guid.NewGuid());

        _mockReviewRepository.Setup(r => r.GetByIdAsync(review.Id, It.IsAny<bool>())).ReturnsAsync(review);
        _mockReviewRepository.Setup(r => r.DeleteAsync(review)).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

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
        Func<Task> act = async () => await _service.DeleteReviewAsync(other.Id, review.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
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

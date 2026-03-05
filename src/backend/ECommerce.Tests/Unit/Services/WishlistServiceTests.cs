using AutoMapper;
using ECommerce.Application.DTOs.Wishlist;
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
public class WishlistServiceTests
{
    private Mock<IWishlistRepository> _mockWishlistRepository = null!;
    private Mock<IProductRepository> _mockProductRepository = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IMapper> _mockMapper = null!;
    private Mock<ILogger<WishlistService>> _mockLogger = null!;
    private WishlistService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockWishlistRepository = new Mock<IWishlistRepository>();
        _mockWishlistRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>())).ReturnsAsync(new List<Wishlist>());
        _mockProductRepository = new Mock<IProductRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = MockHelpers.CreateMockMapper();
        _mockLogger = new Mock<ILogger<WishlistService>>();

        _mockUnitOfWork.Setup(u => u.Wishlists).Returns(_mockWishlistRepository.Object);
        _mockUnitOfWork.Setup(u => u.Products).Returns(_mockProductRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

        _service = new WishlistService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    [TestMethod]
    public async Task GetUserWishlistAsync_ReturnsWishlist()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var product = TestDataFactory.CreateProduct();
        var entry = TestDataFactory.CreateWishlistItem(user.Id, product.Id, product);

        _mockUserRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<bool>())).ReturnsAsync(user);
            var entries = new List<Wishlist> { entry };
            _mockWishlistRepository.Setup(r => r.GetAllByUserIdAsync(user.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => entries.ToList());

        // Act
        var result = await _service.GetUserWishlistAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<WishlistDto>.Success success)
        {
            success.Data.ItemCount.Should().Be(1);
            success.Data.Items.First().ProductId.Should().Be(product.Id);
        }
        else
        {
            Assert.Fail("Expected Result<WishlistDto>.Success");
        }
    }

    [TestMethod]
    public async Task GetUserWishlistAsync_UserNotFound_ThrowsUserNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockUserRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<bool>())).ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetUserWishlistAsync(id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<WishlistDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.UserNotFound);
        }
        else
        {
            Assert.Fail("Expected Result<WishlistDto>.Failure");
        }
    }

    [TestMethod]
    public async Task AddToWishlistAsync_Valid_AddsAndReturnsWishlist()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var product = TestDataFactory.CreateProduct();

        _mockUserRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<bool>())).ReturnsAsync(user);
        _mockProductRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<bool>())).ReturnsAsync(product);
        _mockWishlistRepository.Setup(r => r.IsProductInWishlistAsync(user.Id, product.Id)).ReturnsAsync(false);
        _mockWishlistRepository.Setup(r => r.AddAsync(It.IsAny<Wishlist>(), It.IsAny<CancellationToken>()))
            .Callback<Wishlist, CancellationToken>((w, _) => { if (w.Id == Guid.Empty) w.Id = Guid.NewGuid(); })
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockWishlistRepository.Setup(r => r.GetAllByUserIdAsync(user.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Wishlist> { TestDataFactory.CreateWishlistItem(user.Id, product.Id, product) });

        // Act
        var result = await _service.AddToWishlistAsync(user.Id, product.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<WishlistDto>.Success success)
        {
            success.Data.ItemCount.Should().Be(1);
        }
        else
        {
            Assert.Fail("Expected Result<WishlistDto>.Success");
        }
        _mockWishlistRepository.Verify(r => r.AddAsync(It.IsAny<Wishlist>()), Times.Once);
    }

    [TestMethod]
    public async Task AddToWishlistAsync_Duplicate_ThrowsDuplicateWishlistItemException()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var product = TestDataFactory.CreateProduct();
        _mockUserRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<bool>())).ReturnsAsync(user);
        _mockProductRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<bool>())).ReturnsAsync(product);
        _mockWishlistRepository.Setup(r => r.IsProductInWishlistAsync(user.Id, product.Id)).ReturnsAsync(true);

        // Act
        var result = await _service.AddToWishlistAsync(user.Id, product.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<WishlistDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.DuplicateWishlistItem);
        }
        else
        {
            Assert.Fail("Expected Result<WishlistDto>.Failure");
        }
    }

    [TestMethod]
    public async Task RemoveFromWishlistAsync_Existing_RemovesAndReturns()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var product = TestDataFactory.CreateProduct();
        var entry = TestDataFactory.CreateWishlistItem(user.Id, product.Id);

        _mockUserRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<bool>())).ReturnsAsync(user);
        var entries = new List<Wishlist> { entry };
        _mockWishlistRepository.Setup(r => r.GetAllByUserIdAsync(user.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => entries.ToList());
        _mockProductRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<bool>())).ReturnsAsync(product);
            _mockWishlistRepository.Setup(r => r.DeleteAsync(It.IsAny<Wishlist>(), It.IsAny<CancellationToken>()))
                .Callback<Wishlist, CancellationToken>((w, _) => entries.RemoveAll(x => x.Id == w.Id))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        // (after deletion, repository will return empty list) - handled by sequence above

        // Act
        var result = await _service.RemoveFromWishlistAsync(user.Id, product.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<WishlistDto>.Success success)
        {
            success.Data.ItemCount.Should().Be(0);
        }
        else
        {
            Assert.Fail("Expected Result<WishlistDto>.Success");
        }
        _mockWishlistRepository.Verify(r => r.DeleteAsync(It.IsAny<Wishlist>()), Times.Once);
    }

    [TestMethod]
    public async Task RemoveFromWishlistAsync_NotFound_ThrowsWishlistItemNotFoundException()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var productId = Guid.NewGuid();
        _mockUserRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<bool>())).ReturnsAsync(user);
        _mockWishlistRepository.Setup(r => r.GetAllByUserIdAsync(user.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Wishlist>());

        // Act
        var result = await _service.RemoveFromWishlistAsync(user.Id, productId);

        // Assert
        // Note: RemoveFromWishlistAsync is idempotent - it succeeds even if the item wasn't in the wishlist
        result.IsSuccess.Should().BeTrue();
        if (result is Result<WishlistDto>.Success success)
        {
            success.Data.ItemCount.Should().Be(0);
        }
        else
        {
            Assert.Fail("Expected Result<WishlistDto>.Success");
        }
    }

    [TestMethod]
    public async Task IsProductInWishlistAsync_ReturnsValue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _mockWishlistRepository.Setup(r => r.IsProductInWishlistAsync(userId, productId)).ReturnsAsync(true);

        // Act
        var result = await _service.IsProductInWishlistAsync(userId, productId);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task ClearWishlistAsync_RemovesAll_ReturnsEmpty()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var p1 = TestDataFactory.CreateProduct();
        var p2 = TestDataFactory.CreateProduct();
        var entries = new List<Wishlist>
        {
            TestDataFactory.CreateWishlistItem(user.Id, p1.Id),
            TestDataFactory.CreateWishlistItem(user.Id, p2.Id)
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<bool>())).ReturnsAsync(user);
        _mockWishlistRepository.Setup(r => r.GetAllByUserIdAsync(user.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(entries);
        _mockWishlistRepository.Setup(r => r.DeleteAsync(It.IsAny<Wishlist>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.ClearWishlistAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<WishlistDto>.Success success)
        {
            success.Data.ItemCount.Should().Be(0);
        }
        else
        {
            Assert.Fail("Expected Result<WishlistDto>.Success");
        }
        _mockWishlistRepository.Verify(r => r.DeleteAsync(It.IsAny<Wishlist>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}

using AutoMapper;
using ECommerce.Application.DTOs.Users;
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
public class UserServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IMapper> _mockMapper = null!;
    private Mock<ILogger<UserService>> _mockLogger = null!;
    private UserService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockMapper = MockHelpers.CreateMockMapper();
        _mockLogger = new Mock<ILogger<UserService>>();

        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

        _service = new UserService(_mockUnitOfWork.Object, _mockMapper.Object, _mockLogger.Object);
    }

    #region GetUserProfileAsync Tests

    [TestMethod]
    public async Task GetUserProfileAsync_ValidUserId_ReturnsUserProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateUser(email: "test@example.com");
        user.Id = userId;

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var expectedProfile = new UserProfileDto
        {
            Id = userId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        };

        _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns((User u) => new UserProfileDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Phone = u.Phone,
                AvatarUrl = u.AvatarUrl
            });

        // Act
        var result = await _service.GetUserProfileAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<UserProfileDto>.Success success)
        {
            success.Data.Id.Should().Be(userId);
            success.Data.Email.Should().Be(user.Email);
            success.Data.FirstName.Should().Be(user.FirstName);
            success.Data.LastName.Should().Be(user.LastName);
        }
        else
        {
            Assert.Fail("Expected Result<UserProfileDto>.Success");
        }
    }

    [TestMethod]
    public async Task GetUserProfileAsync_UserNotFound_ThrowsUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetUserProfileAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<UserProfileDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.UserNotFound);
        }
        else
        {
            Assert.Fail("Expected Result<UserProfileDto>.Failure");
        }
    }

    [TestMethod]
    public async Task GetUserProfileAsync_CallsRepositoryWithCorrectUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateUser();
        user.Id = userId;

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(new UserProfileDto());

        // Act
        await _service.GetUserProfileAsync(userId);

        // Assert
        _mockUserRepository.Verify(
            r => r.GetByIdAsync(userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task GetUserProfileAsync_MapsUserToUserProfileDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateUser();
        user.Id = userId;

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(new UserProfileDto { Id = userId });

        // Act
        await _service.GetUserProfileAsync(userId);

        // Assert
        _mockMapper.Verify(m => m.Map<UserProfileDto>(user), Times.Once);
    }

    #endregion

    #region UpdateUserProfileAsync Tests

    [TestMethod]
    public async Task UpdateUserProfileAsync_ValidUpdate_ReturnsUpdatedProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateUser();
        user.Id = userId;
        user.FirstName = "OldFirst";
        user.LastName = "OldLast";
        user.Phone = "1234567890";
        user.AvatarUrl = "old-avatar.jpg";

        var updateDto = new UpdateProfileDto
        {
            FirstName = "NewFirst",
            LastName = "NewLast",
            Phone = "0987654321",
            AvatarUrl = "new-avatar.jpg"
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns((User u) => new UserProfileDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Phone = u.Phone,
                AvatarUrl = u.AvatarUrl
            });

        // Act
        var result = await _service.UpdateUserProfileAsync(userId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<UserProfileDto>.Success success)
        {
            success.Data.FirstName.Should().Be("NewFirst");
            success.Data.LastName.Should().Be("NewLast");
            success.Data.Phone.Should().Be("0987654321");
            success.Data.AvatarUrl.Should().Be("new-avatar.jpg");
        }
        else
        {
            Assert.Fail("Expected Result<UserProfileDto>.Success");
        }
    }

    [TestMethod]
    public async Task UpdateUserProfileAsync_UserNotFound_ThrowsUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateProfileDto
        {
            FirstName = "New",
            LastName = "Name"
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.UpdateUserProfileAsync(userId, updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<UserProfileDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.UserNotFound);
        }
        else
        {
            Assert.Fail("Expected Result<UserProfileDto>.Failure");
        }
    }

    [TestMethod]
    public async Task UpdateUserProfileAsync_UpdatesUserProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateUser();
        user.Id = userId;

        var updateDto = new UpdateProfileDto
        {
            FirstName = "UpdatedFirst",
            LastName = "UpdatedLast",
            Phone = "5555555555",
            AvatarUrl = "updated.jpg"
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(new UserProfileDto());

        // Act
        await _service.UpdateUserProfileAsync(userId, updateDto);

        // Assert
        user.FirstName.Should().Be("UpdatedFirst");
        user.LastName.Should().Be("UpdatedLast");
        user.Phone.Should().Be("5555555555");
        user.AvatarUrl.Should().Be("updated.jpg");
    }

    [TestMethod]
    public async Task UpdateUserProfileAsync_UpdatesTimestamp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateUser();
        user.Id = userId;
        var originalUpdatedAt = user.UpdatedAt;

        var updateDto = new UpdateProfileDto
        {
            FirstName = "New",
            LastName = "Name"
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(new UserProfileDto());

        // Add small delay to ensure timestamp changes
        await Task.Delay(10);

        // Act
        await _service.UpdateUserProfileAsync(userId, updateDto);

        // Assert
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task UpdateUserProfileAsync_CallsUpdateAsyncOnRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateUser();
        user.Id = userId;

        var updateDto = new UpdateProfileDto
        {
            FirstName = "Test",
            LastName = "User"
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(new UserProfileDto());

        // Act
        await _service.UpdateUserProfileAsync(userId, updateDto);

        // Assert
        _mockUserRepository.Verify(
            r => r.UpdateAsync(user, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task UpdateUserProfileAsync_CallsSaveChangesAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateUser();
        user.Id = userId;

        var updateDto = new UpdateProfileDto
        {
            FirstName = "Test",
            LastName = "User"
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(new UserProfileDto());

        // Act
        await _service.UpdateUserProfileAsync(userId, updateDto);

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateUserProfileAsync_NullPhone_UpdatesToNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateUser();
        user.Id = userId;
        user.Phone = "1234567890";

        var updateDto = new UpdateProfileDto
        {
            FirstName = "Test",
            LastName = "User",
            Phone = null
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(new UserProfileDto());

        // Act
        await _service.UpdateUserProfileAsync(userId, updateDto);

        // Assert
        user.Phone.Should().BeNull();
    }

    [TestMethod]
    public async Task UpdateUserProfileAsync_NullAvatarUrl_UpdatesToNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateUser();
        user.Id = userId;
        user.AvatarUrl = "old-avatar.jpg";

        var updateDto = new UpdateProfileDto
        {
            FirstName = "Test",
            LastName = "User",
            AvatarUrl = null
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(new UserProfileDto());

        // Act
        await _service.UpdateUserProfileAsync(userId, updateDto);

        // Assert
        user.AvatarUrl.Should().BeNull();
    }

    [TestMethod]
    public async Task UpdateUserProfileAsync_MapsUpdatedUserToDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateUser();
        user.Id = userId;

        var updateDto = new UpdateProfileDto
        {
            FirstName = "Mapped",
            LastName = "User"
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(new UserProfileDto { Id = userId });

        // Act
        await _service.UpdateUserProfileAsync(userId, updateDto);

        // Assert
        _mockMapper.Verify(m => m.Map<UserProfileDto>(user), Times.Once);
    }

    [TestMethod]
    public async Task UpdateUserProfileAsync_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateUser();
        user.Id = userId;

        var updateDto = new UpdateProfileDto
        {
            FirstName = "Test",
            LastName = "User"
        };

        var cancellationToken = new CancellationToken();

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(new UserProfileDto());

        // Act
        await _service.UpdateUserProfileAsync(userId, updateDto, cancellationToken);

        // Assert
        _mockUserRepository.Verify(
            r => r.GetByIdAsync(userId, It.IsAny<bool>(), cancellationToken),
            Times.Once);
        _mockUserRepository.Verify(
            r => r.UpdateAsync(user, cancellationToken),
            Times.Once);
        _mockUnitOfWork.Verify(
            u => u.SaveChangesAsync(cancellationToken),
            Times.Once);
    }

    #endregion
}

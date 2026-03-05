using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Core.Entities;
using AutoMapper;
using ECommerce.Core.Enums;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Results;
using ECommerce.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.IdentityModel.Tokens.Jwt;

namespace ECommerce.Tests.Unit.Services;

[TestClass]
public class AuthServiceTests
{
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<IEmailService> _mockEmailService = null!;
    private Mock<ILogger<AuthService>> _mockLogger = null!;
    private Mock<IMapper> _mockMapper = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private AuthService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockEmailService = new Mock<IEmailService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        // Setup JWT configuration
        _mockConfiguration.Setup(c => c["Jwt:SecretKey"]).Returns("this-is-a-very-secure-secret-key-for-testing-at-least-32-characters");
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _mockConfiguration.Setup(c => c["Jwt:ExpireMinutes"]).Returns("60");
        _mockConfiguration.Setup(c => c["AppUrl"]).Returns("https://test.com");

        _mockMapper = new Mock<IMapper>();
        _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>()))
            .Returns((User u) => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Phone = u.Phone,
                Role = u.Role.ToString(),
                AvatarUrl = u.AvatarUrl
            });

        _mockLogger = new Mock<ILogger<AuthService>>();
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

        // Mock RefreshTokens repository
        var mockRefreshTokenRepo = new Mock<IRepository<RefreshToken>>();
        mockRefreshTokenRepo.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RefreshTokens).Returns(mockRefreshTokenRepo.Object);

        _service = new AuthService(_mockUnitOfWork.Object, _mockConfiguration.Object, _mockEmailService.Object, _mockMapper.Object, _mockLogger.Object);
    }

    #region RegisterAsync Tests

    [TestMethod]
    public async Task RegisterAsync_ValidData_CreatesUserAndReturnsAuthResponse()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "SecurePassword123!",
            FirstName = "John",
            LastName = "Doe"
        };

        _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email))
            .ReturnsAsync(false);

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => { if (user.Id == Guid.Empty) user.Id = Guid.NewGuid(); })
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.RegisterAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        
        if (result is Result<AuthResponseDto>.Success success)
        {
            success.Data.Should().NotBeNull();
            success.Data.User.Should().NotBeNull();
            success.Data.User!.Email.Should().Be(dto.Email);
            success.Data.User.FirstName.Should().Be(dto.FirstName);
            success.Data.User.LastName.Should().Be(dto.LastName);
            success.Data.User.Role.Should().Be(UserRole.Customer.ToString());
            success.Data.Token.Should().NotBeNullOrEmpty();
        }
        else
        {
            Assert.Fail("Expected Result<AuthResponseDto>.Success");
        }

        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Exactly(2)); // Once for user, once for refresh token
    }

    [TestMethod]
    public async Task RegisterAsync_DuplicateEmail_ReturnsDuplicateEmailFailure()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "existing@example.com",
            Password = "SecurePassword123!",
            FirstName = "John",
            LastName = "Doe"
        };

        _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email))
            .ReturnsAsync(true);

        // Act
        var result = await _service.RegisterAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        
        if (result is Result<AuthResponseDto>.Failure failure)
        {
            failure.Code.Should().Be("DUPLICATE_EMAIL");
            failure.Message.Should().Contain("already registered");
        }
        else
        {
            Assert.Fail("Expected Result<AuthResponseDto>.Failure");
        }

        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [TestMethod]
    public async Task RegisterAsync_ValidData_HashesPassword()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "SecurePassword123!",
            FirstName = "John",
            LastName = "Doe"
        };

        User capturedUser = null!;

        _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email))
            .ReturnsAsync(false);

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) =>
            {
                if (u.Id == Guid.Empty) u.Id = Guid.NewGuid();
                capturedUser = u;
            })
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _service.RegisterAsync(dto);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser.PasswordHash.Should().NotBeNullOrEmpty();
        capturedUser.PasswordHash.Should().NotBe(dto.Password);

        // Verify the hash is a valid BCrypt hash
        BCrypt.Net.BCrypt.Verify(dto.Password, capturedUser.PasswordHash).Should().BeTrue();
    }

    #endregion

    #region LoginAsync Tests

    [TestMethod]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var password = "SecurePassword123!";
        var user = TestDataFactory.CreateUser(email: "test@example.com");
        user.PasswordHash = _service.HashPassword(password);

        var dto = new LoginDto
        {
            Email = user.Email,
            Password = password
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(dto.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _service.LoginAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        
        if (result is Result<AuthResponseDto>.Success success)
        {
            success.Data.Should().NotBeNull();
            success.Data.User.Should().NotBeNull();
            success.Data.User!.Email.Should().Be(user.Email);
            success.Data.Token.Should().NotBeNullOrEmpty();
        }
        else
        {
            Assert.Fail("Expected Result<AuthResponseDto>.Success");
        }
    }

    [TestMethod]
    public async Task LoginAsync_InvalidPassword_ReturnsInvalidCredentialsFailure()
    {
        // Arrange
        var user = TestDataFactory.CreateUser(email: "test@example.com");
        user.PasswordHash = _service.HashPassword("CorrectPassword123!");

        var dto = new LoginDto
        {
            Email = user.Email,
            Password = "WrongPassword123!"
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(dto.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _service.LoginAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        
        if (result is Result<AuthResponseDto>.Failure failure)
        {
            failure.Code.Should().Be("INVALID_CREDENTIALS");
        }
        else
        {
            Assert.Fail("Expected Result<AuthResponseDto>.Failure");
        }
    }

    [TestMethod]
    public async Task LoginAsync_UserNotFound_ReturnsInvalidCredentialsFailure()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword123!"
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(dto.Email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.LoginAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        
        if (result is Result<AuthResponseDto>.Failure failure)
        {
            failure.Code.Should().Be("INVALID_CREDENTIALS");
        }
        else
        {
            Assert.Fail("Expected Result<AuthResponseDto>.Failure");
        }
    }

    #endregion

    #region Password Hashing Tests

    [TestMethod]
    public void HashPassword_ValidPassword_ReturnsHashedPassword()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var hash = _service.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);
        hash.Should().StartWith("$2");  // BCrypt hash starts with $2a or $2b
    }

    [TestMethod]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var hash = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var correctPassword = "CorrectPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hash = _service.HashPassword(correctPassword);

        // Act
        var result = _service.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region JWT Token Tests

    [TestMethod]
    public void GenerateJwtToken_ValidUser_ReturnsValidToken()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Customer.ToString()
        };

        // Act
        var token = _service.GenerateJwtToken(userDto);

        // Assert
        token.Should().NotBeNullOrEmpty();

        // Verify token structure
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == "sub" && c.Value == userDto.Id.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == userDto.Email);
        jwtToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == userDto.Role);
    }

    [TestMethod]
    public async Task ValidateTokenAsync_ValidToken_ReturnsTrue()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Customer.ToString()
        };
        var token = _service.GenerateJwtToken(userDto);

        // Act
        var result = await _service.ValidateTokenAsync(token);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task ValidateTokenAsync_InvalidToken_ReturnsFalse()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var result = await _service.ValidateTokenAsync(invalidToken);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region VerifyEmailAsync Tests

    [TestMethod]
    public async Task VerifyEmailAsync_ValidToken_VerifiesEmail()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        var token = Guid.NewGuid().ToString();
        user.EmailVerificationToken = token;
        user.IsEmailVerified = false;

        _mockUserRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<bool>()))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.VerifyEmailAsync(user.Id, token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.IsEmailVerified.Should().BeTrue();
        user.EmailVerificationToken.Should().BeNull();
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task VerifyEmailAsync_InvalidToken_ReturnsInvalidTokenFailure()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();
        user.EmailVerificationToken = "correct-token";

        _mockUserRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<bool>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.VerifyEmailAsync(user.Id, "wrong-token");

        // Assert
        result.IsSuccess.Should().BeFalse();
        
        if (result is Result<ECommerce.Core.Results.Unit>.Failure failure)
        {
            failure.Code.Should().Be("INVALID_TOKEN");
        }
        else
        {
            Assert.Fail("Expected Result<Unit>.Failure");
        }
        
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [TestMethod]
    public async Task VerifyEmailAsync_UserNotFound_ReturnsUserNotFoundFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<bool>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.VerifyEmailAsync(userId, "some-token");

        // Assert
        result.IsSuccess.Should().BeFalse();
        
        if (result is Result<ECommerce.Core.Results.Unit>.Failure failure)
        {
            failure.Code.Should().Be("USER_NOT_FOUND");
        }
        else
        {
            Assert.Fail("Expected Result<Unit>.Failure");
        }
    }

    #endregion

    #region Password Reset Tests

    [TestMethod]
    public async Task GeneratePasswordResetTokenAsync_ValidEmail_ReturnsToken()
    {
        // Arrange
        var user = TestDataFactory.CreateUser(email: "test@example.com");

        _mockUserRepository.Setup(r => r.GetByEmailAsync(user.Email))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var token = await _service.GeneratePasswordResetTokenAsync(user.Email);

        // Assert
        token.Should().NotBeNull();
        token.Should().NotBe(string.Empty);
        user.PasswordResetToken.Should().NotBeNull();
        user.PasswordResetExpires.Should().NotBeNull();
        user.PasswordResetExpires.Should().BeAfter(DateTime.UtcNow);
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [TestMethod]
    public async Task GeneratePasswordResetTokenAsync_InvalidEmail_ReturnsDummyToken()
    {
        // Arrange
        var email = "nonexistent@example.com";

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync((User?)null);

        // Act
        var token = await _service.GeneratePasswordResetTokenAsync(email);

        // Assert
        token.Should().NotBeNull();
        token.Should().NotBe(string.Empty);
        // Should NOT update or save anything for security (prevents email enumeration)
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [TestMethod]
    public async Task ResetPasswordAsync_ValidToken_ResetsPassword()
    {
        // Arrange
        var user = TestDataFactory.CreateUser(email: "test@example.com");
        var token = Guid.NewGuid().ToString();
        user.PasswordResetToken = token;
        user.PasswordResetExpires = DateTime.UtcNow.AddHours(1);
        var oldPasswordHash = user.PasswordHash;

        _mockUserRepository.Setup(r => r.GetByEmailAsync(user.Email))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.ResetPasswordAsync(user.Email, token, "NewSecurePassword123!");

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().NotBe(oldPasswordHash);
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetExpires.Should().BeNull();
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [TestMethod]
    public async Task ResetPasswordAsync_InvalidToken_ReturnsInvalidTokenFailure()
    {
        // Arrange
        var user = TestDataFactory.CreateUser(email: "test@example.com");
        user.PasswordResetToken = "correct-token";
        user.PasswordResetExpires = DateTime.UtcNow.AddHours(1);

        _mockUserRepository.Setup(r => r.GetByEmailAsync(user.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _service.ResetPasswordAsync(user.Email, "wrong-token", "NewPassword123!");

        // Assert
        result.IsSuccess.Should().BeFalse();
        
        if (result is Result<ECommerce.Core.Results.Unit>.Failure failure)
        {
            failure.Code.Should().Be("INVALID_TOKEN");
        }
        else
        {
            Assert.Fail("Expected Result<Unit>.Failure");
        }
        
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [TestMethod]
    public async Task ResetPasswordAsync_ExpiredToken_ReturnsInvalidTokenFailure()
    {
        // Arrange
        var user = TestDataFactory.CreateUser(email: "test@example.com");
        var token = Guid.NewGuid().ToString();
        user.PasswordResetToken = token;
        user.PasswordResetExpires = DateTime.UtcNow.AddHours(-1); // Expired 1 hour ago

        _mockUserRepository.Setup(r => r.GetByEmailAsync(user.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _service.ResetPasswordAsync(user.Email, token, "NewPassword123!");

        // Assert
        result.IsSuccess.Should().BeFalse();
        
        if (result is Result<ECommerce.Core.Results.Unit>.Failure failure)
        {
            failure.Code.Should().Be("INVALID_TOKEN");
        }
        else
        {
            Assert.Fail("Expected Result<Unit>.Failure");
        }
    }

    [TestMethod]
    public async Task ResetPasswordAsync_UserNotFound_ReturnsUserNotFoundFailure()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.ResetPasswordAsync(email, "some-token", "NewPassword123!");

        // Assert
        result.IsSuccess.Should().BeFalse();
        
        if (result is Result<ECommerce.Core.Results.Unit>.Failure failure)
        {
            failure.Code.Should().Be("USER_NOT_FOUND");
        }
        else
        {
            Assert.Fail("Expected Result<Unit>.Failure");
        }
    }

    #endregion

    #region ChangePasswordAsync Tests

    [TestMethod]
    public async Task ChangePasswordAsync_ValidOldPassword_ChangesPassword()
    {
        // Arrange
        var oldPassword = "OldPassword123!";
        var newPassword = "NewPassword123!";
        var user = TestDataFactory.CreateUser();
        user.PasswordHash = _service.HashPassword(oldPassword);
        var oldHash = user.PasswordHash;

        _mockUserRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<bool>()))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.ChangePasswordAsync(user.Id, oldPassword, newPassword);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().NotBe(oldHash);
        _service.VerifyPassword(newPassword, user.PasswordHash!).Should().BeTrue();
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [TestMethod]
    public async Task ChangePasswordAsync_InvalidOldPassword_ReturnsInvalidCredentialsFailure()
    {
        // Arrange
        var correctPassword = "CorrectPassword123!";
        var wrongOldPassword = "WrongOldPassword123!";
        var newPassword = "NewPassword123!";
        var user = TestDataFactory.CreateUser();
        user.PasswordHash = _service.HashPassword(correctPassword);

        _mockUserRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<bool>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.ChangePasswordAsync(user.Id, wrongOldPassword, newPassword);

        // Assert
        result.IsSuccess.Should().BeFalse();
        
        if (result is Result<ECommerce.Core.Results.Unit>.Failure failure)
        {
            failure.Code.Should().Be("INVALID_CREDENTIALS");
        }
        else
        {
            Assert.Fail("Expected Result<Unit>.Failure");
        }
        
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [TestMethod]
    public async Task ChangePasswordAsync_UserNotFound_ReturnsUserNotFoundFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<bool>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.ChangePasswordAsync(userId, "OldPassword123!", "NewPassword123!");

        // Assert
        result.IsSuccess.Should().BeFalse();
        
        if (result is Result<ECommerce.Core.Results.Unit>.Failure failure)
        {
            failure.Code.Should().Be("USER_NOT_FOUND");
        }
        else
        {
            Assert.Fail("Expected Result<Unit>.Failure");
        }
    }

    #endregion
}

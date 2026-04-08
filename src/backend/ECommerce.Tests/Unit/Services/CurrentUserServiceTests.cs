using ECommerce.Infrastructure.Services;
using ECommerce.SharedKernel.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace ECommerce.Tests.Unit.Services;

/// <summary>
/// Unit tests for CurrentUserService.
/// Tests user context extraction from HTTP claims.
/// </summary>
[TestClass]
public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly DefaultHttpContext _httpContext;
    private readonly ClaimsPrincipal _user;

    public CurrentUserServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpContext = new DefaultHttpContext();
        _user = new ClaimsPrincipal();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _httpContextAccessorMock.Reset();
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithValidHttpContextAccessor_DoesNotThrow()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        // Act & Assert
        var action = () => new CurrentUserService(_httpContextAccessorMock.Object);
        action.Should().NotThrow();
    }

    #endregion

    #region UserId Tests

    [TestMethod]
    public void UserId_WithValidNameIdentifierClaim_ReturnsGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        _user.AddIdentity(identity);
        _httpContext.User = _user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.UserId;

        // Assert
        result.Should().Be(userId);
    }

    [TestMethod]
    public void UserId_WithSubClaim_ReturnsGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("sub", userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        _user.AddIdentity(identity);
        _httpContext.User = _user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.UserId;

        // Assert
        result.Should().Be(userId);
    }

    [TestMethod]
    public void UserId_PrefersNameIdentifierOverSub()
    {
        // Arrange
        var userIdFromNameId = Guid.NewGuid();
        var userIdFromSub = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userIdFromNameId.ToString()),
            new("sub", userIdFromSub.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        _user.AddIdentity(identity);
        _httpContext.User = _user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.UserId;

        // Assert
        result.Should().Be(userIdFromNameId);
    }

    [TestMethod]
    public void UserId_WithNoUserIdClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var identity = new ClaimsIdentity(new List<Claim>(), "test");
        _user.AddIdentity(identity);
        _httpContext.User = _user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act & Assert
        var action = () => service.UserId;
        action.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("User ID not found in token");
    }

    [TestMethod]
    public void UserId_WithInvalidGuidClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "not-a-guid")
        };
        var identity = new ClaimsIdentity(claims, "test");
        _user.AddIdentity(identity);
        _httpContext.User = _user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act & Assert
        var action = () => service.UserId;
        action.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("User ID not found in token");
    }

    [TestMethod]
    public void UserId_WithNullHttpContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act & Assert
        var action = () => service.UserId;
        action.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("User ID not found in token");
    }

    [TestMethod]
    public void UserId_WithNullUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _httpContext.User = null!;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act & Assert
        var action = () => service.UserId;
        action.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("User ID not found in token");
    }

    #endregion

    #region UserIdOrNull Tests

    [TestMethod]
    public void UserIdOrNull_WithValidNameIdentifierClaim_ReturnsGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        _user.AddIdentity(identity);
        _httpContext.User = _user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.UserIdOrNull;

        // Assert
        result.Should().Be(userId);
    }

    [TestMethod]
    public void UserIdOrNull_WithNoUserIdClaim_ReturnsNull()
    {
        // Arrange
        var identity = new ClaimsIdentity(new List<Claim>(), "test");
        _user.AddIdentity(identity);
        _httpContext.User = _user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.UserIdOrNull;

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void UserIdOrNull_WithInvalidGuidClaim_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "not-a-guid")
        };
        var identity = new ClaimsIdentity(claims, "test");
        _user.AddIdentity(identity);
        _httpContext.User = _user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.UserIdOrNull;

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void UserIdOrNull_WithNullHttpContext_ReturnsNull()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.UserIdOrNull;

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SessionId Tests

    [TestMethod]
    public void SessionId_WithSessionCookie_ReturnsSessionId()
    {
        // Arrange
        var sessionId = "test-session-123";
        // Create a mock for IRequestCookieCollection
        var cookiesMock = new Mock<IRequestCookieCollection>();
        cookiesMock.Setup(x => x.TryGetValue("sessionId", out sessionId)).Returns(true);
        _httpContext.Request.Cookies = cookiesMock.Object;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.SessionId;

        // Assert
        result.Should().Be(sessionId);
    }

    [TestMethod]
    public void SessionId_WithNoSessionCookie_ReturnsNull()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.SessionId;

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void SessionId_WithNullHttpContext_ReturnsNull()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.SessionId;

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Email Tests

    [TestMethod]
    public void Email_WithEmailClaim_ReturnsEmail()
    {
        // Arrange
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "test");
        _user.AddIdentity(identity);
        _httpContext.User = _user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.Email;

        // Assert
        result.Should().Be(email);
    }

    [TestMethod]
    public void Email_WithNoEmailClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var identity = new ClaimsIdentity(new List<Claim>(), "test");
        _user.AddIdentity(identity);
        _httpContext.User = _user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act & Assert
        var action = () => service.Email;
        action.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("Email not found in token");
    }

    [TestMethod]
    public void Email_WithNullHttpContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act & Assert
        var action = () => service.Email;
        action.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("Email not found in token");
    }

    #endregion

    #region Role Tests

    [TestMethod]
    public void Role_WithValidRoleClaim_ReturnsRole()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, UserRole.Admin.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        _user.AddIdentity(identity);
        _httpContext.User = _user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.Role;

        // Assert
        result.Should().Be(UserRole.Admin);
    }

    [TestMethod]
    public void Role_WithCustomerRole_ReturnsCustomerRole()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, UserRole.Customer.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        _user.AddIdentity(identity);
        _httpContext.User = _user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.Role;

        // Assert
        result.Should().Be(UserRole.Customer);
    }

    [TestMethod]
    public void Role_WithNoRoleClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var identity = new ClaimsIdentity(new List<Claim>(), "test");
        _user.AddIdentity(identity);
        _httpContext.User = _user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act & Assert
        var action = () => service.Role;
        action.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("Role not found in token");
    }

    [TestMethod]
    public void Role_WithInvalidRoleValue_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "InvalidRole")
        };
        var identity = new ClaimsIdentity(claims, "test");
        _user.AddIdentity(identity);
        _httpContext.User = _user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act & Assert
        var action = () => service.Role;
        action.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("Role not found in token");
    }

    [TestMethod]
    public void Role_WithNullHttpContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act & Assert
        var action = () => service.Role;
        action.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("Role not found in token");
    }

    #endregion

    #region IsAuthenticated Tests

    [TestMethod]
    public void IsAuthenticated_WithAuthenticatedUser_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        _user.AddIdentity(identity);
        _httpContext.User = _user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.IsAuthenticated;

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public void IsAuthenticated_WithUnauthenticatedUser_ReturnsFalse()
    {
        // Arrange
        var identity = new ClaimsIdentity(); // Not authenticated
        _user.AddIdentity(identity);
        _httpContext.User = _user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public void IsAuthenticated_WithNullHttpContext_ReturnsFalse()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public void IsAuthenticated_WithNullUser_ReturnsFalse()
    {
        // Arrange
        _httpContext.User = null!;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public void IsAuthenticated_WithNullIdentity_ReturnsFalse()
    {
        // Arrange
        _httpContext.User = new ClaimsPrincipal(); // No identity
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}


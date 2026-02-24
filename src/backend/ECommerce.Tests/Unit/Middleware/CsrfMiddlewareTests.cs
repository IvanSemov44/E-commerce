using ECommerce.API.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace ECommerce.Tests.Unit.Middleware;

/// <summary>
/// Unit tests for CsrfMiddleware.
/// Tests CSRF protection for authenticated requests.
/// </summary>
[TestClass]
public class CsrfMiddlewareTests
{
    private readonly Mock<IWebHostEnvironment> _environmentMock;
    private readonly Mock<ILogger<CsrfMiddleware>> _loggerMock;
    private readonly Mock<IAntiforgery> _antiforgeryMock;

    public CsrfMiddlewareTests()
    {
        _environmentMock = new Mock<IWebHostEnvironment>();
        _loggerMock = new Mock<ILogger<CsrfMiddleware>>();
        _antiforgeryMock = new Mock<IAntiforgery>();
    }

    [TestInitialize]
    public void Setup()
    {
        _environmentMock.Reset();
        _loggerMock.Reset();
        _antiforgeryMock.Reset();
        
        // Default to Production environment
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Production");
    }

    #region Test Environment Tests

    [TestMethod]
    public async Task InvokeAsync_InTestEnvironment_SkipsCsrfValidation()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Test");
        var wasNextCalled = false;
        var context = CreateHttpContext("/api/products", "POST");
        context.User = CreateAuthenticatedUser();
        
        var middleware = new CsrfMiddleware(
            _ => { wasNextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        wasNextCalled.Should().BeTrue();
        _antiforgeryMock.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    #endregion

    #region Auth Endpoint Tests

    [TestMethod]
    public async Task InvokeAsync_ForLoginEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        var wasNextCalled = false;
        var context = CreateHttpContext("/api/auth/login", "POST");
        
        var middleware = new CsrfMiddleware(
            _ => { wasNextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        wasNextCalled.Should().BeTrue();
        _antiforgeryMock.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [TestMethod]
    public async Task InvokeAsync_ForRegisterEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        var wasNextCalled = false;
        var context = CreateHttpContext("/api/auth/register", "POST");
        
        var middleware = new CsrfMiddleware(
            _ => { wasNextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        wasNextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_ForRefreshTokenEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        var wasNextCalled = false;
        var context = CreateHttpContext("/api/auth/refresh-token", "POST");
        
        var middleware = new CsrfMiddleware(
            _ => { wasNextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        wasNextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_ForLogoutEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        var wasNextCalled = false;
        var context = CreateHttpContext("/api/auth/logout", "POST");
        
        var middleware = new CsrfMiddleware(
            _ => { wasNextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        wasNextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_ForForgotPasswordEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        var wasNextCalled = false;
        var context = CreateHttpContext("/api/auth/forgot-password", "POST");
        
        var middleware = new CsrfMiddleware(
            _ => { wasNextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        wasNextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_ForResetPasswordEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        var wasNextCalled = false;
        var context = CreateHttpContext("/api/auth/reset-password", "POST");
        
        var middleware = new CsrfMiddleware(
            _ => { wasNextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        wasNextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_ForVerifyEmailEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        var wasNextCalled = false;
        var context = CreateHttpContext("/api/auth/verify-email", "POST");
        
        var middleware = new CsrfMiddleware(
            _ => { wasNextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        wasNextCalled.Should().BeTrue();
    }

    #endregion

    #region Excluded Path Tests

    [TestMethod]
    public async Task InvokeAsync_ForHealthEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        var wasNextCalled = false;
        var context = CreateHttpContext("/health", "GET");
        
        var middleware = new CsrfMiddleware(
            _ => { wasNextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        wasNextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_ForSwaggerEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        var wasNextCalled = false;
        var context = CreateHttpContext("/swagger/index.html", "GET");
        
        var middleware = new CsrfMiddleware(
            _ => { wasNextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        wasNextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_ForOpenApiEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        var wasNextCalled = false;
        var context = CreateHttpContext("/openapi/v1.json", "GET");
        
        var middleware = new CsrfMiddleware(
            _ => { wasNextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        wasNextCalled.Should().BeTrue();
    }

    #endregion

    #region Safe Methods (GET, HEAD, OPTIONS, TRACE) Tests

    [TestMethod]
    public async Task InvokeAsync_ForGetRequestWithAuthenticatedUser_GeneratesCsrfToken()
    {
        // Arrange
        var tokens = new AntiforgeryTokenSet("request-token", "cookie-token", "form-field", "header-name");
        _antiforgeryMock.Setup(x => x.GetAndStoreTokens(It.IsAny<HttpContext>()))
            .Returns(tokens);
        
        var wasNextCalled = false;
        var context = CreateHttpContext("/api/products", "GET");
        context.User = CreateAuthenticatedUser();
        
        var middleware = new CsrfMiddleware(
            _ => { wasNextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        wasNextCalled.Should().BeTrue();
        _antiforgeryMock.Verify(x => x.GetAndStoreTokens(context), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_ForHeadRequestWithAuthenticatedUser_GeneratesCsrfToken()
    {
        // Arrange
        var tokens = new AntiforgeryTokenSet("request-token", "cookie-token", "form-field", "header-name");
        _antiforgeryMock.Setup(x => x.GetAndStoreTokens(It.IsAny<HttpContext>()))
            .Returns(tokens);
        
        var context = CreateHttpContext("/api/products", "HEAD");
        context.User = CreateAuthenticatedUser();
        
        var middleware = new CsrfMiddleware(
            _ => Task.CompletedTask,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        _antiforgeryMock.Verify(x => x.GetAndStoreTokens(context), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_ForOptionsRequestWithAuthenticatedUser_GeneratesCsrfToken()
    {
        // Arrange
        var tokens = new AntiforgeryTokenSet("request-token", "cookie-token", "form-field", "header-name");
        _antiforgeryMock.Setup(x => x.GetAndStoreTokens(It.IsAny<HttpContext>()))
            .Returns(tokens);
        
        var context = CreateHttpContext("/api/products", "OPTIONS");
        context.User = CreateAuthenticatedUser();
        
        var middleware = new CsrfMiddleware(
            _ => Task.CompletedTask,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        _antiforgeryMock.Verify(x => x.GetAndStoreTokens(context), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_ForGetRequestWithUnauthenticatedUser_DoesNotGenerateToken()
    {
        // Arrange
        var context = CreateHttpContext("/api/products", "GET");
        context.User = new ClaimsPrincipal(); // Unauthenticated
        
        var middleware = new CsrfMiddleware(
            _ => Task.CompletedTask,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        _antiforgeryMock.Verify(x => x.GetAndStoreTokens(It.IsAny<HttpContext>()), Times.Never);
    }

    #endregion

    #region Unsafe Methods (POST, PUT, DELETE, PATCH) Tests

    [TestMethod]
    public async Task InvokeAsync_ForPostRequestWithValidCsrfToken_PassesThrough()
    {
        // Arrange
        _antiforgeryMock.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);
        
        var wasNextCalled = false;
        var context = CreateHttpContext("/api/products", "POST");
        context.User = CreateAuthenticatedUser();
        
        var middleware = new CsrfMiddleware(
            _ => { wasNextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        wasNextCalled.Should().BeTrue();
        _antiforgeryMock.Verify(x => x.ValidateRequestAsync(context), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_ForPutRequestWithValidCsrfToken_PassesThrough()
    {
        // Arrange
        _antiforgeryMock.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);
        
        var wasNextCalled = false;
        var context = CreateHttpContext("/api/products/123", "PUT");
        context.User = CreateAuthenticatedUser();
        
        var middleware = new CsrfMiddleware(
            _ => { wasNextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        wasNextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_ForDeleteRequestWithValidCsrfToken_PassesThrough()
    {
        // Arrange
        _antiforgeryMock.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);
        
        var wasNextCalled = false;
        var context = CreateHttpContext("/api/products/123", "DELETE");
        context.User = CreateAuthenticatedUser();
        
        var middleware = new CsrfMiddleware(
            _ => { wasNextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        wasNextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_ForPatchRequestWithValidCsrfToken_PassesThrough()
    {
        // Arrange
        _antiforgeryMock.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);
        
        var wasNextCalled = false;
        var context = CreateHttpContext("/api/products/123", "PATCH");
        context.User = CreateAuthenticatedUser();
        
        var middleware = new CsrfMiddleware(
            _ => { wasNextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        wasNextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_ForPostRequestWithInvalidCsrfToken_Returns400()
    {
        // Arrange
        _antiforgeryMock.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("Invalid CSRF token"));
        
        var context = CreateHttpContext("/api/products", "POST");
        context.User = CreateAuthenticatedUser();
        context.Response.Body = new MemoryStream();
        
        var middleware = new CsrfMiddleware(
            _ => Task.CompletedTask,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [TestMethod]
    public async Task InvokeAsync_ForPostRequestWithInvalidCsrfToken_ReturnsErrorMessage()
    {
        // Arrange
        _antiforgeryMock.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("Invalid CSRF token"));
        
        var context = CreateHttpContext("/api/products", "POST");
        context.User = CreateAuthenticatedUser();
        context.Response.Body = new MemoryStream();
        
        var middleware = new CsrfMiddleware(
            _ => Task.CompletedTask,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);
        
        // Read response
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        // Assert
        body.Should().Contain("Invalid CSRF token");
    }

    [TestMethod]
    public async Task InvokeAsync_ForPostRequestWithUnauthenticatedUser_DoesNotValidateCsrf()
    {
        // Arrange
        var context = CreateHttpContext("/api/products", "POST");
        context.User = new ClaimsPrincipal(); // Unauthenticated
        
        var middleware = new CsrfMiddleware(
            _ => Task.CompletedTask,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert
        _antiforgeryMock.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    #endregion

    #region XSRF-TOKEN Cookie Tests

    [TestMethod]
    public async Task InvokeAsync_ForGetRequest_SetsXsrfTokenCookie()
    {
        // Arrange
        var tokens = new AntiforgeryTokenSet("test-request-token", "test-cookie-token", "form-field", "header-name");
        _antiforgeryMock.Setup(x => x.GetAndStoreTokens(It.IsAny<HttpContext>()))
            .Returns(tokens);
        
        var context = CreateHttpContext("/api/products", "GET");
        context.User = CreateAuthenticatedUser();
        
        var middleware = new CsrfMiddleware(
            _ => Task.CompletedTask,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context, _antiforgeryMock.Object);

        // Assert - Verify that GetAndStoreTokens was called (which sets cookies)
        _antiforgeryMock.Verify(x => x.GetAndStoreTokens(context), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static HttpContext CreateHttpContext(string path, string method)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost:5000");
        return context;
    }

    private static ClaimsPrincipal CreateAuthenticatedUser()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Role, "Customer")
        };
        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }

    #endregion
}

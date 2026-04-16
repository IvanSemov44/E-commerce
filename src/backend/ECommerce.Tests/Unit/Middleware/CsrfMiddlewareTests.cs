using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using ECommerce.API.Middleware;

namespace ECommerce.Tests.Unit.Middleware;

[TestClass]
public class CsrfMiddlewareTests
{
    private Mock<ILogger<CsrfMiddleware>> _mockLogger = null!;
    private Mock<IHostEnvironment> _mockEnvironment = null!;
    private Mock<IAntiforgery> _mockAntiforgery = null!;
    private CsrfMiddleware _middleware = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<CsrfMiddleware>>();
        _mockEnvironment = new Mock<IHostEnvironment>();
        _mockAntiforgery = new Mock<IAntiforgery>();

        _middleware = new CsrfMiddleware(
            _ => Task.CompletedTask,
            _mockLogger.Object,
            _mockEnvironment.Object
        );
    }

    #region Test Environment Tests

    [TestMethod]
    public async Task InvokeAsync_TestEnvironment_SkipsCsrfValidation()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Test");
        var context = CreateHttpContext("POST", "/api/products");
        context.User = CreateAuthenticatedUser();

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    #endregion

    #region Excluded Path Tests

    [TestMethod]
    public async Task InvokeAsync_HealthEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("POST", "/health");
        context.User = CreateAuthenticatedUser();

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [TestMethod]
    public async Task InvokeAsync_SwaggerEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        var context = CreateHttpContext("GET", "/swagger/index.html");
        context.User = CreateAuthenticatedUser();

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.GetAndStoreTokens(It.IsAny<HttpContext>()), Times.Never);
    }

    [TestMethod]
    public async Task InvokeAsync_OpenApiEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("GET", "/openapi/v1.json");
        context.User = CreateAuthenticatedUser();

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.GetAndStoreTokens(It.IsAny<HttpContext>()), Times.Never);
    }

    #endregion

    #region Auth Endpoint Tests

    [TestMethod]
    public async Task InvokeAsync_LoginEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("POST", "/api/auth/login");

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [TestMethod]
    public async Task InvokeAsync_RegisterEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("POST", "/api/auth/register");

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [TestMethod]
    public async Task InvokeAsync_RefreshTokenEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("POST", "/api/auth/refresh-token");

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [TestMethod]
    public async Task InvokeAsync_LogoutEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("POST", "/api/auth/logout");

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [TestMethod]
    public async Task InvokeAsync_ForgotPasswordEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("POST", "/api/auth/forgot-password");

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [TestMethod]
    public async Task InvokeAsync_ResetPasswordEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("POST", "/api/auth/reset-password");

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [TestMethod]
    public async Task InvokeAsync_VerifyEmailEndpoint_SkipsCsrfValidation()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("POST", "/api/auth/verify-email");

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    #endregion

    #region Safe Methods (GET, HEAD, OPTIONS, TRACE) Tests

    [TestMethod]
    public async Task InvokeAsync_AuthenticatedGetRequest_GeneratesCsrfToken()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("GET", "/api/products");
        context.User = CreateAuthenticatedUser();

        var tokens = new AntiforgeryTokenSet("request-token", "cookie-token", "__RequestVerificationToken", "X-XSRF-TOKEN");
        _mockAntiforgery.Setup(a => a.GetAndStoreTokens(context))
            .Returns(tokens);

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.GetAndStoreTokens(context), Times.Once);
        context.Response.Headers.ContainsKey("Set-Cookie").ShouldBeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_AuthenticatedHeadRequest_GeneratesCsrfToken()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("HEAD", "/api/products");
        context.User = CreateAuthenticatedUser();

        var tokens = new AntiforgeryTokenSet("request-token", "cookie-token", "__RequestVerificationToken", "X-XSRF-TOKEN");
        _mockAntiforgery.Setup(a => a.GetAndStoreTokens(context))
            .Returns(tokens);

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.GetAndStoreTokens(context), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_AuthenticatedOptionsRequest_GeneratesCsrfToken()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("OPTIONS", "/api/products");
        context.User = CreateAuthenticatedUser();

        var tokens = new AntiforgeryTokenSet("request-token", "cookie-token", "__RequestVerificationToken", "X-XSRF-TOKEN");
        _mockAntiforgery.Setup(a => a.GetAndStoreTokens(context))
            .Returns(tokens);

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.GetAndStoreTokens(context), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_AuthenticatedTraceRequest_GeneratesCsrfToken()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("TRACE", "/api/products");
        context.User = CreateAuthenticatedUser();

        var tokens = new AntiforgeryTokenSet("request-token", "cookie-token", "__RequestVerificationToken", "X-XSRF-TOKEN");
        _mockAntiforgery.Setup(a => a.GetAndStoreTokens(context))
            .Returns(tokens);

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.GetAndStoreTokens(context), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_UnauthenticatedGetRequest_DoesNotGenerateCsrfToken()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("GET", "/api/products");
        context.User = CreateUnauthenticatedUser();

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.GetAndStoreTokens(It.IsAny<HttpContext>()), Times.Never);
    }

    #endregion

    #region Unsafe Methods (POST, PUT, DELETE, PATCH) Tests

    [TestMethod]
    public async Task InvokeAsync_AuthenticatedPostRequest_ValidatesCsrfToken()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("POST", "/api/products");
        context.User = CreateAuthenticatedUser();

        _mockAntiforgery.Setup(a => a.ValidateRequestAsync(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.ValidateRequestAsync(context), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_AuthenticatedPutRequest_ValidatesCsrfToken()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("PUT", "/api/products/1");
        context.User = CreateAuthenticatedUser();

        _mockAntiforgery.Setup(a => a.ValidateRequestAsync(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.ValidateRequestAsync(context), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_AuthenticatedDeleteRequest_ValidatesCsrfToken()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("DELETE", "/api/products/1");
        context.User = CreateAuthenticatedUser();

        _mockAntiforgery.Setup(a => a.ValidateRequestAsync(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.ValidateRequestAsync(context), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_AuthenticatedPatchRequest_ValidatesCsrfToken()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("PATCH", "/api/products/1");
        context.User = CreateAuthenticatedUser();

        _mockAntiforgery.Setup(a => a.ValidateRequestAsync(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.ValidateRequestAsync(context), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_InvalidCsrfToken_Returns400BadRequest()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("POST", "/api/products");
        context.User = CreateAuthenticatedUser();

        _mockAntiforgery.Setup(a => a.ValidateRequestAsync(context))
            .ThrowsAsync(new AntiforgeryValidationException("Invalid CSRF token"));

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        context.Response.ContentType.ShouldStartWith("application/json");
    }

    [TestMethod]
    public async Task InvokeAsync_UnauthenticatedPostRequest_DoesNotValidateCsrfToken()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = CreateHttpContext("POST", "/api/products");
        context.User = CreateUnauthenticatedUser();

        // Act
        await _middleware.InvokeAsync(context, _mockAntiforgery.Object);

        // Assert
        _mockAntiforgery.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    #endregion

    #region Helper Methods

    private static DefaultHttpContext CreateHttpContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Host = new HostString("localhost:5000");
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static ClaimsPrincipal CreateAuthenticatedUser()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, "testuser"),
            new(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal CreateUnauthenticatedUser()
    {
        var identity = new ClaimsIdentity();
        return new ClaimsPrincipal(identity);
    }

    #endregion
}

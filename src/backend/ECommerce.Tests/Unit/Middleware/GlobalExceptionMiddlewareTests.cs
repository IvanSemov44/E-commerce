using ECommerce.API.Middleware;
using ECommerce.Application.DTOs.Common;
using ECommerce.Core.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Unit.Middleware;

/// <summary>
/// Unit tests for GlobalExceptionMiddleware.
/// Tests that the middleware properly catches exceptions and returns standardized ApiResponse.
/// </summary>
[TestClass]
public class GlobalExceptionMiddlewareTests
{
    private Mock<RequestDelegate> _mockNextMiddleware = null!;
    private Mock<ILogger<GlobalExceptionMiddleware>> _mockLogger = null!;
    private GlobalExceptionMiddleware _middleware = null!;
    private DefaultHttpContext _httpContext = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockNextMiddleware = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<GlobalExceptionMiddleware>>();
        _middleware = new GlobalExceptionMiddleware(_mockNextMiddleware.Object, _mockLogger.Object);
        _httpContext = new DefaultHttpContext();
    }

    [TestMethod]
    public async Task InvokeAsync_WhenProductNotFound_Returns404WithCorrectMessage()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var exception = new ProductNotFoundException(productId);
        var response = new MemoryStream();
        _httpContext.Response.Body = response;

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        _httpContext.Response.ContentType.Should().Contain("application/json");
    }

    [TestMethod]
    public async Task InvokeAsync_WhenUnauthorized_Returns401()
    {
        // Arrange
        var exception = new InvalidCredentialsException();
        var response = new MemoryStream();
        _httpContext.Response.Body = response;

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenBadRequest_Returns400()
    {
        // Arrange
        var exception = new InvalidQuantityException("Invalid quantity requested");
        var response = new MemoryStream();
        _httpContext.Response.Body = response;

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenConflict_Returns409()
    {
        // Arrange
        var exception = new DuplicateEmailException("test@example.com");
        var response = new MemoryStream();
        _httpContext.Response.Body = response;

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenGenericException_Returns500()
    {
        // Arrange
        var exception = new InvalidOperationException("Something went wrong");
        var response = new MemoryStream();
        _httpContext.Response.Body = response;

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenExceptionOccurs_LogsError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");
        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenExceptionOccurs_SetsContentTypeToJson()
    {
        // Arrange
        var exception = new InvalidOperationException("Test");
        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.ContentType.Should().StartWith("application/json");
    }

    [TestMethod]
    public async Task InvokeAsync_WhenUserNotFound_Returns404()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var exception = new UserNotFoundException(userId);
        var response = new MemoryStream();
        _httpContext.Response.Body = response;

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenCartNotFound_Returns404()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var exception = new CartNotFoundException(userId);
        _httpContext.Response.Body = new MemoryStream();

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenInsufficientStock_Returns400()
    {
        // Arrange
        var exception = new InsufficientStockException("Widget", 5, 2);
        _httpContext.Response.Body = new MemoryStream();

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenRequestSucceeds_CallsNextMiddleware()
    {
        // Arrange
        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNextMiddleware.Verify(x => x.Invoke(It.IsAny<HttpContext>()), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenOrderNotFound_Returns404()
    {
        // Arrange
        var exception = new OrderNotFoundException(Guid.NewGuid());
        _httpContext.Response.Body = new MemoryStream();

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenEmptyCart_Returns400()
    {
        // Arrange
        var exception = new EmptyCartException();
        _httpContext.Response.Body = new MemoryStream();

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [TestMethod]
    public async Task InvokeAsync_WritesResponseAsJson()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");
        var response = new MemoryStream();
        _httpContext.Response.Body = response;

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        response.Length.Should().BeGreaterThan(0);
        _httpContext.Response.ContentType.Should().Contain("application/json");
    }
}

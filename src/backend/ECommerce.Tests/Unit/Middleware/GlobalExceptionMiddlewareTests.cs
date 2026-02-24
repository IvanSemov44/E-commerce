using ECommerce.API.Middleware;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Exceptions.Base;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;
using System.Text.Json;

namespace ECommerce.Tests.Unit.Middleware;

/// <summary>
/// Unit tests for GlobalExceptionMiddleware.
/// Tests exception handling and mapping to appropriate HTTP responses.
/// </summary>
[TestClass]
public class GlobalExceptionMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionMiddleware>> _loggerMock;

    public GlobalExceptionMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
    }

    [TestInitialize]
    public void Setup()
    {
        _loggerMock.Reset();
    }

    #region NotFoundException Tests

    [TestMethod]
    public async Task InvokeAsync_WithNotFoundException_Returns404()
    {
        // Arrange
        var exception = new ProductNotFoundException(Guid.NewGuid());
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        context.Response.ContentType.Should().StartWith("application/json");
    }

    [TestMethod]
    public async Task InvokeAsync_WithNotFoundException_ReturnsCorrectMessage()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var exception = new ProductNotFoundException(productId);
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);
        var response = await ReadResponseBody<ErrorResponse>(context);

        // Assert
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Contain(productId.ToString());
    }

    #endregion

    #region UnauthorizedException Tests

    [TestMethod]
    public async Task InvokeAsync_WithUnauthorizedException_Returns401()
    {
        // Arrange
        var exception = new InvalidTokenException();
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task InvokeAsync_WithInvalidCredentialsException_Returns401()
    {
        // Arrange
        var exception = new InvalidCredentialsException();
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    #endregion

    #region BadRequestException Tests

    [TestMethod]
    public async Task InvokeAsync_WithBadRequestException_Returns400()
    {
        // Arrange
        var exception = new InvalidQuantityException("Quantity must be positive");
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [TestMethod]
    public async Task InvokeAsync_WithEmptyCartException_Returns400()
    {
        // Arrange
        var exception = new EmptyCartException();
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [TestMethod]
    public async Task InvokeAsync_WithInsufficientStockException_Returns400()
    {
        // Arrange
        var exception = new InsufficientStockException("Product", 10, 5);
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region ConflictException Tests

    [TestMethod]
    public async Task InvokeAsync_WithConflictException_Returns409()
    {
        // Arrange
        var exception = new DuplicateEmailException("test@example.com");
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [TestMethod]
    public async Task InvokeAsync_WithDuplicateProductSlugException_Returns409()
    {
        // Arrange
        var exception = new DuplicateProductSlugException("test-product");
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    #endregion

    #region ArgumentException Tests

    [TestMethod]
    public async Task InvokeAsync_WithArgumentNullException_Returns400()
    {
        // Arrange
        var exception = new ArgumentNullException("param");
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [TestMethod]
    public async Task InvokeAsync_WithArgumentException_Returns400()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region InvalidOperationException Tests

    [TestMethod]
    public async Task InvokeAsync_WithInvalidOperationException_Returns409()
    {
        // Arrange
        var exception = new InvalidOperationException("Invalid operation");
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    #endregion

    #region Generic Exception Tests

    [TestMethod]
    public async Task InvokeAsync_WithGenericException_Returns500()
    {
        // Arrange
        var exception = new Exception("Something went wrong");
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [TestMethod]
    public async Task InvokeAsync_WithGenericException_DoesNotExposeMessage()
    {
        // Arrange
        var exception = new Exception("Sensitive internal error message");
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);
        var response = await ReadResponseBody<ErrorResponse>(context);

        // Assert
        response.Should().NotBeNull();
        response!.Message.Should().NotContain("Sensitive internal error message");
        response.Message.Should().Be("An internal server error occurred. Please try again later.");
    }

    #endregion

    #region No Exception Tests

    [TestMethod]
    public async Task InvokeAsync_WithoutException_CallsNext()
    {
        // Arrange
        var wasNextCalled = false;
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => 
        {
            wasNextCalled = true;
            return Task.CompletedTask;
        }, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        wasNextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_WithoutException_DoesNotModifyResponse()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Response.StatusCode = StatusCodes.Status200OK;
        var middleware = new GlobalExceptionMiddleware(_ => Task.CompletedTask, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    #endregion

    #region Logging Tests

    [TestMethod]
    public async Task InvokeAsync_WithException_LogsError()
    {
        // Arrange
        var exception = new Exception("Test exception");
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost:5000");
        return context;
    }

    private static async Task<T?> ReadResponseBody<T>(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    #endregion

    #region Response Models

    private class ErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
    }

    #endregion
}

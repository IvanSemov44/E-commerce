using ECommerce.API.Middleware;
using ECommerce.SharedKernel.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;
using System.Text.Json;

namespace ECommerce.Tests.Unit.Middleware;

/// <summary>
/// Unit tests for GlobalExceptionMiddleware.
/// Tests exception handling and mapping to appropriate HTTP responses.
/// NOTE: With Result<T> pattern, business logic failures are handled via Result -
/// exceptions are reserved for unexpected infrastructure failures only.
/// </summary>
[TestClass]
public class GlobalExceptionMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionMiddleware>> _loggerMock;
    private static readonly JsonSerializerOptions ResponseJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GlobalExceptionMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
    }

    [TestInitialize]
    public void Setup()
    {
        _loggerMock.Reset();
    }

    #region Base Exception Type Tests

    [TestMethod]
    public async Task InvokeAsync_WithNotFoundException_Returns404()
    {
        // Arrange
        var exception = new TestNotFoundException("Resource not found");
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        context.Response.ContentType.Should().StartWith("application/json");
    }

    [TestMethod]
    public async Task InvokeAsync_WithBadRequestException_Returns400()
    {
        // Arrange
        var exception = new TestBadRequestException("Invalid request");
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [TestMethod]
    public async Task InvokeAsync_WithUnauthorizedException_Returns401()
    {
        // Arrange
        var exception = new TestUnauthorizedException("Not authorized");
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task InvokeAsync_WithConflictException_Returns409()
    {
        // Arrange
        var exception = new TestConflictException("Resource conflict");
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    #endregion

    #region EF Core Concurrency Tests

    [TestMethod]
    public async Task InvokeAsync_WithDbUpdateConcurrencyException_Returns409()
    {
        // Arrange
        var exception = new DbUpdateConcurrencyException("Concurrency conflict");
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [TestMethod]
    public async Task InvokeAsync_WithDbUpdateConcurrencyException_ReturnsCorrectMessage()
    {
        // Arrange
        var exception = new DbUpdateConcurrencyException("Concurrency conflict");
        var context = CreateHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);
        var response = await ReadResponseBody<ErrorResponse>(context);

        // Assert
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.ErrorDetails.Should().NotBeNull();
        response.ErrorDetails!.Message.Should().Contain("modified by another user");
        response.ErrorDetails.Code.Should().Be("CONCURRENCY_CONFLICT");
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
        response!.ErrorDetails.Should().NotBeNull();
        response.ErrorDetails!.Message.Should().NotContain("Sensitive internal error message");
        response.ErrorDetails.Message.Should().Be("An internal server error occurred. Please try again later.");
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
                It.Is<It.IsAnyType>((_, _) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private static DefaultHttpContext CreateHttpContext()
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
        return JsonSerializer.Deserialize<T>(body, ResponseJsonOptions);
    }

    #endregion

    #region Response Models

    private sealed class ErrorResponse
    {
        public bool Success { get; set; }
        public ErrorDetails? ErrorDetails { get; set; }
    }

    private sealed class ErrorDetails
    {
        public string Message { get; set; } = null!;
        public string? Code { get; set; }
    }

    #endregion

    #region Test Exception Classes

    private sealed class TestNotFoundException : NotFoundException
    {
        public TestNotFoundException(string message) : base(message)
        {
        }

        public TestNotFoundException() : base()
        {
        }

        public TestNotFoundException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    private sealed class TestBadRequestException : BadRequestException
    {
        public TestBadRequestException(string message) : base(message)
        {
        }

        public TestBadRequestException() : base()
        {
        }

        public TestBadRequestException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    private sealed class TestUnauthorizedException : UnauthorizedException
    {
        public TestUnauthorizedException(string message) : base(message)
        {
        }

        public TestUnauthorizedException() : base()
        {
        }

        public TestUnauthorizedException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    private sealed class TestConflictException : ConflictException
    {
        public TestConflictException(string message) : base(message)
        {
        }

        public TestConflictException() : base()
        {
        }

        public TestConflictException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    #endregion
}

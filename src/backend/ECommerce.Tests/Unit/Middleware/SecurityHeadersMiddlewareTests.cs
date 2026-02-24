using ECommerce.API.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace ECommerce.Tests.Unit.Middleware;

/// <summary>
/// Unit tests for SecurityHeadersMiddleware.
/// Tests that security headers are properly set on responses.
/// </summary>
[TestClass]
public class SecurityHeadersMiddlewareTests
{
    private readonly Mock<IWebHostEnvironment> _environmentMock;

    public SecurityHeadersMiddlewareTests()
    {
        _environmentMock = new Mock<IWebHostEnvironment>();
    }

    [TestInitialize]
    public void Setup()
    {
        _environmentMock.Reset();
    }

    #region Security Headers Tests

    [TestMethod]
    public async Task InvokeAsync_SetsXFrameOptionsHeader()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        var context = CreateHttpContext(_environmentMock.Object);
        var wasNextCalled = false;
        
        var middleware = new SecurityHeadersMiddleware(_ => 
        {
            wasNextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("X-Frame-Options");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        wasNextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_SetsXContentTypeOptionsHeader()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        var context = CreateHttpContext(_environmentMock.Object);
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("X-Content-Type-Options");
        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
    }

    [TestMethod]
    public async Task InvokeAsync_SetsXXSSProtectionHeader()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        var context = CreateHttpContext(_environmentMock.Object);
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("X-XSS-Protection");
        context.Response.Headers["X-XSS-Protection"].ToString().Should().Be("1; mode=block");
    }

    [TestMethod]
    public async Task InvokeAsync_SetsReferrerPolicyHeader()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        var context = CreateHttpContext(_environmentMock.Object);
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("Referrer-Policy");
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
    }

    [TestMethod]
    public async Task InvokeAsync_SetsPermissionsPolicyHeader()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        var context = CreateHttpContext(_environmentMock.Object);
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("Permissions-Policy");
        context.Response.Headers["Permissions-Policy"].ToString().Should().Be("camera=(), microphone=(), geolocation=()");
    }

    [TestMethod]
    public async Task InvokeAsync_SetsContentSecurityPolicyHeader()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        var context = CreateHttpContext(_environmentMock.Object);
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("Content-Security-Policy");
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.Should().Contain("default-src 'self'");
        csp.Should().Contain("script-src 'self'");
        csp.Should().Contain("style-src 'self' 'unsafe-inline'");
    }

    [TestMethod]
    public async Task InvokeAsync_InDevelopment_DoesNotSetHSTSHeader()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        var context = CreateHttpContext(_environmentMock.Object);
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().NotContainKey("Strict-Transport-Security");
    }

    [TestMethod]
    public async Task InvokeAsync_InProduction_SetsHSTSHeader()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var context = CreateHttpContext(_environmentMock.Object);
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("Strict-Transport-Security");
        var hsts = context.Response.Headers["Strict-Transport-Security"].ToString();
        hsts.Should().Contain("max-age=31536000");
        hsts.Should().Contain("includeSubDomains");
    }

    [TestMethod]
    public async Task InvokeAsync_CallsNextMiddleware()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        var context = CreateHttpContext(_environmentMock.Object);
        var wasNextCalled = false;
        var middleware = new SecurityHeadersMiddleware(_ => 
        {
            wasNextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        wasNextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_SetsAllExpectedHeaders()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var context = CreateHttpContext(_environmentMock.Object);
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("X-Frame-Options");
        context.Response.Headers.Should().ContainKey("X-Content-Type-Options");
        context.Response.Headers.Should().ContainKey("X-XSS-Protection");
        context.Response.Headers.Should().ContainKey("Referrer-Policy");
        context.Response.Headers.Should().ContainKey("Permissions-Policy");
        context.Response.Headers.Should().ContainKey("Content-Security-Policy");
        context.Response.Headers.Should().ContainKey("Strict-Transport-Security");
    }

    #endregion

    #region Helper Methods

    private static HttpContext CreateHttpContext(IWebHostEnvironment environment)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost:5000");
        
        // Set up RequestServices with the environment
        var services = new ServiceCollection();
        services.AddSingleton(environment);
        context.RequestServices = services.BuildServiceProvider();
        
        return context;
    }

    #endregion
}

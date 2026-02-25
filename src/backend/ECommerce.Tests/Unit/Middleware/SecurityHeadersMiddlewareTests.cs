using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using FluentAssertions;
using ECommerce.API.Middleware;

namespace ECommerce.Tests.Unit.Middleware;

[TestClass]
public class SecurityHeadersMiddlewareTests
{
    private SecurityHeadersMiddleware _middleware = null!;

    [TestInitialize]
    public void Setup()
    {
        _middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
    }

    [TestMethod]
    public async Task InvokeAsync_SetsXFrameOptionsHeader()
    {
        // Arrange
        var context = CreateHttpContext(isDevelopment: false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("X-Frame-Options");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
    }

    [TestMethod]
    public async Task InvokeAsync_SetsXContentTypeOptionsHeader()
    {
        // Arrange
        var context = CreateHttpContext(isDevelopment: false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("X-Content-Type-Options");
        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
    }

    [TestMethod]
    public async Task InvokeAsync_SetsXXSSProtectionHeader()
    {
        // Arrange
        var context = CreateHttpContext(isDevelopment: false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("X-XSS-Protection");
        context.Response.Headers["X-XSS-Protection"].ToString().Should().Be("1; mode=block");
    }

    [TestMethod]
    public async Task InvokeAsync_SetsReferrerPolicyHeader()
    {
        // Arrange
        var context = CreateHttpContext(isDevelopment: false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("Referrer-Policy");
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
    }

    [TestMethod]
    public async Task InvokeAsync_SetsPermissionsPolicyHeader()
    {
        // Arrange
        var context = CreateHttpContext(isDevelopment: false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("Permissions-Policy");
        context.Response.Headers["Permissions-Policy"].ToString().Should().Be("camera=(), microphone=(), geolocation=()");
    }

    [TestMethod]
    public async Task InvokeAsync_SetsContentSecurityPolicyHeader()
    {
        // Arrange
        var context = CreateHttpContext(isDevelopment: false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("Content-Security-Policy");
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.Should().Contain("default-src 'self'");
        csp.Should().Contain("script-src 'self'");
        csp.Should().Contain("style-src 'self' 'unsafe-inline'");
        csp.Should().Contain("img-src 'self' data: https:");
        csp.Should().Contain("font-src 'self' https:");
    }

    [TestMethod]
    public async Task InvokeAsync_InDevelopment_DoesNotSetHstsHeader()
    {
        // Arrange
        var context = CreateHttpContext(isDevelopment: true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().NotContainKey("Strict-Transport-Security");
    }

    [TestMethod]
    public async Task InvokeAsync_InProduction_SetsHstsHeader()
    {
        // Arrange
        var context = CreateHttpContext(isDevelopment: false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("Strict-Transport-Security");
        var hsts = context.Response.Headers["Strict-Transport-Security"].ToString();
        hsts.Should().Contain("max-age=31536000");
        hsts.Should().Contain("includeSubDomains");
    }

    [TestMethod]
    public async Task InvokeAsync_SetsAllRequiredSecurityHeaders()
    {
        // Arrange
        var context = CreateHttpContext(isDevelopment: false);
        var expectedHeaders = new[]
        {
            "X-Frame-Options",
            "X-Content-Type-Options",
            "X-XSS-Protection",
            "Referrer-Policy",
            "Permissions-Policy",
            "Content-Security-Policy",
            "Strict-Transport-Security"
        };

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        foreach (var header in expectedHeaders)
        {
            context.Response.Headers.Should().ContainKey(header, $"because {header} should be set");
        }
    }

    [TestMethod]
    public async Task InvokeAsync_CallsNextMiddleware()
    {
        // Arrange
        var wasNextCalled = false;
        var middleware = new SecurityHeadersMiddleware(_ =>
        {
            wasNextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateHttpContext(isDevelopment: false);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        wasNextCalled.Should().BeTrue();
    }

    #region Helper Methods

    private static DefaultHttpContext CreateHttpContext(bool isDevelopment)
    {
        var context = new DefaultHttpContext();
        
        var mockEnvironment = new Mock<IWebHostEnvironment>();
        mockEnvironment.Setup(e => e.EnvironmentName)
            .Returns(isDevelopment ? "Development" : "Production");

        var services = new ServiceCollection();
        services.AddSingleton<IWebHostEnvironment>(mockEnvironment.Object);
        context.RequestServices = services.BuildServiceProvider();

        return context;
    }

    #endregion
}

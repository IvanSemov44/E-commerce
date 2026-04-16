using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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
        context.Response.Headers.ContainsKey("X-Frame-Options").ShouldBeTrue();
        context.Response.Headers["X-Frame-Options"].ToString().ShouldBe("DENY");
    }

    [TestMethod]
    public async Task InvokeAsync_SetsXContentTypeOptionsHeader()
    {
        // Arrange
        var context = CreateHttpContext(isDevelopment: false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ContainsKey("X-Content-Type-Options").ShouldBeTrue();
        context.Response.Headers["X-Content-Type-Options"].ToString().ShouldBe("nosniff");
    }

    [TestMethod]
    public async Task InvokeAsync_SetsXXSSProtectionHeader()
    {
        // Arrange
        var context = CreateHttpContext(isDevelopment: false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ContainsKey("X-XSS-Protection").ShouldBeTrue();
        context.Response.Headers["X-XSS-Protection"].ToString().ShouldBe("1; mode=block");
    }

    [TestMethod]
    public async Task InvokeAsync_SetsReferrerPolicyHeader()
    {
        // Arrange
        var context = CreateHttpContext(isDevelopment: false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ContainsKey("Referrer-Policy").ShouldBeTrue();
        context.Response.Headers["Referrer-Policy"].ToString().ShouldBe("strict-origin-when-cross-origin");
    }

    [TestMethod]
    public async Task InvokeAsync_SetsPermissionsPolicyHeader()
    {
        // Arrange
        var context = CreateHttpContext(isDevelopment: false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ContainsKey("Permissions-Policy").ShouldBeTrue();
        context.Response.Headers["Permissions-Policy"].ToString().ShouldBe("camera=(), microphone=(), geolocation=()");
    }

    [TestMethod]
    public async Task InvokeAsync_SetsContentSecurityPolicyHeader()
    {
        // Arrange
        var context = CreateHttpContext(isDevelopment: false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ContainsKey("Content-Security-Policy").ShouldBeTrue();
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.ShouldContain("default-src 'self'");
        csp.ShouldContain("script-src 'self'");
        csp.ShouldContain("style-src 'self' 'unsafe-inline'");
        csp.ShouldContain("img-src 'self' data: https:");
        csp.ShouldContain("font-src 'self' https:");
    }

    [TestMethod]
    public async Task InvokeAsync_InDevelopment_DoesNotSetHstsHeader()
    {
        // Arrange
        var context = CreateHttpContext(isDevelopment: true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ContainsKey("Strict-Transport-Security").ShouldBeFalse();
    }

    [TestMethod]
    public async Task InvokeAsync_InProduction_SetsHstsHeader()
    {
        // Arrange
        var context = CreateHttpContext(isDevelopment: false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ContainsKey("Strict-Transport-Security").ShouldBeTrue();
        var hsts = context.Response.Headers["Strict-Transport-Security"].ToString();
        hsts.ShouldContain("max-age=31536000");
        hsts.ShouldContain("includeSubDomains");
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
            context.Response.Headers.ContainsKey(header).ShouldBeTrue();
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
        wasNextCalled.ShouldBeTrue();
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

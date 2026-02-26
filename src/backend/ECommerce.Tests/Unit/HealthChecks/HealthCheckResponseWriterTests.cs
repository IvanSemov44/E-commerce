using System.Text.Json;
using ECommerce.API.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ECommerce.Tests.Unit.HealthChecks;

/// <summary>
/// Tests for the HealthCheckResponseWriter class.
/// Tests JSON response generation for health check endpoints.
/// </summary>
[TestClass]
public class HealthCheckResponseWriterTests
{
    private Mock<HttpContext> _mockHttpContext = null!;
    private Mock<HttpResponse> _mockHttpResponse = null!;
    private Mock<IResponseCookies> _mockCookies = null!;
    private HeaderDictionary _headers = null!;
    private MemoryStream _responseBody = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpResponse = new Mock<HttpResponse>();
        _mockCookies = new Mock<IResponseCookies>();
        _headers = new HeaderDictionary();
        _responseBody = new MemoryStream();

        _mockHttpResponse.SetupGet(r => r.Headers).Returns(_headers);
        _mockHttpResponse.SetupGet(r => r.Body).Returns(_responseBody);
        _mockHttpResponse.Setup(r => r.WriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((s, ct) =>
            {
                var writer = new StreamWriter(_responseBody, leaveOpen: true);
                writer.Write(s);
                writer.Flush();
                return Task.CompletedTask;
            });

        _mockHttpContext.SetupGet(c => c.Response).Returns(_mockHttpResponse.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _responseBody.Dispose();
    }

    #region WriteHealthCheckResponse Tests

    [TestMethod]
    public async Task WriteHealthCheckResponse_HealthyReport_Sets200StatusCode()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_mockHttpContext.Object, healthReport);

        // Assert
        _mockHttpResponse.VerifySet(r => r.StatusCode = StatusCodes.Status200OK);
    }

    [TestMethod]
    public async Task WriteHealthCheckResponse_DegradedReport_Sets200StatusCode()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Degraded);

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_mockHttpContext.Object, healthReport);

        // Assert
        _mockHttpResponse.VerifySet(r => r.StatusCode = StatusCodes.Status200OK);
    }

    [TestMethod]
    public async Task WriteHealthCheckResponse_UnhealthyReport_Sets503StatusCode()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Unhealthy);

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_mockHttpContext.Object, healthReport);

        // Assert
        _mockHttpResponse.VerifySet(r => r.StatusCode = StatusCodes.Status503ServiceUnavailable);
    }

    [TestMethod]
    public async Task WriteHealthCheckResponse_SetsJsonContentType()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_mockHttpContext.Object, healthReport);

        // Assert
        _mockHttpResponse.VerifySet(r => r.ContentType = "application/json");
    }

    [TestMethod]
    public async Task WriteHealthCheckResponse_IncludesStatusInResponse()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_mockHttpContext.Object, healthReport);

        // Assert
        _responseBody.Position = 0;
        var reader = new StreamReader(_responseBody);
        var response = await reader.ReadToEndAsync();
        response.Should().Contain("\"status\": \"Healthy\"");
    }

    [TestMethod]
    public async Task WriteHealthCheckResponse_IncludesTimestampInResponse()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_mockHttpContext.Object, healthReport);

        // Assert
        _responseBody.Position = 0;
        var reader = new StreamReader(_responseBody);
        var response = await reader.ReadToEndAsync();
        response.Should().Contain("timestamp");
    }

    [TestMethod]
    public async Task WriteHealthCheckResponse_IncludesTotalDurationInResponse()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_mockHttpContext.Object, healthReport);

        // Assert
        _responseBody.Position = 0;
        var reader = new StreamReader(_responseBody);
        var response = await reader.ReadToEndAsync();
        response.Should().Contain("totalDurationMs");
    }

    [TestMethod]
    public async Task WriteHealthCheckResponse_IncludesChecksArrayInResponse()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_mockHttpContext.Object, healthReport);

        // Assert
        _responseBody.Position = 0;
        var reader = new StreamReader(_responseBody);
        var response = await reader.ReadToEndAsync();
        response.Should().Contain("checks");
    }

    [TestMethod]
    public async Task WriteHealthCheckResponse_WithMultipleEntries_IncludesAllChecks()
    {
        // Arrange
        var entries = new Dictionary<string, HealthReportEntry>
        {
            { "memory", new HealthReportEntry(HealthStatus.Healthy, "Memory OK", TimeSpan.FromMilliseconds(10), null, null) },
            { "database", new HealthReportEntry(HealthStatus.Healthy, "Database OK", TimeSpan.FromMilliseconds(50), null, null) }
        };
        var healthReport = new HealthReport(entries, TimeSpan.FromMilliseconds(60));

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_mockHttpContext.Object, healthReport);

        // Assert
        _responseBody.Position = 0;
        var reader = new StreamReader(_responseBody);
        var response = await reader.ReadToEndAsync();
        response.Should().Contain("memory");
        response.Should().Contain("database");
    }

    [TestMethod]
    public async Task WriteHealthCheckResponse_WithEntryData_IncludesDataInResponse()
    {
        // Arrange
        var data = new Dictionary<string, object> { { "AllocatedMB", 100L } };
        var entries = new Dictionary<string, HealthReportEntry>
        {
            { "memory", new HealthReportEntry(HealthStatus.Healthy, "Memory OK", TimeSpan.FromMilliseconds(10), null, data, null) }
        };
        var healthReport = new HealthReport(entries, TimeSpan.FromMilliseconds(10));

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_mockHttpContext.Object, healthReport);

        // Assert
        _responseBody.Position = 0;
        var reader = new StreamReader(_responseBody);
        var response = await reader.ReadToEndAsync();
        response.Should().Contain("AllocatedMB");
    }

    [TestMethod]
    public async Task WriteHealthCheckResponse_WithTags_IncludesTagsInResponse()
    {
        // Arrange
        var tags = new[] { "system", "monitoring" };
        var entries = new Dictionary<string, HealthReportEntry>
        {
            { "memory", new HealthReportEntry(HealthStatus.Healthy, "Memory OK", TimeSpan.FromMilliseconds(10), null, null, tags) }
        };
        var healthReport = new HealthReport(entries, TimeSpan.FromMilliseconds(10));

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_mockHttpContext.Object, healthReport);

        // Assert
        _responseBody.Position = 0;
        var reader = new StreamReader(_responseBody);
        var response = await reader.ReadToEndAsync();
        response.Should().Contain("tags");
    }

    #endregion

    #region WriteLivenessResponse Tests

    [TestMethod]
    public async Task WriteLivenessResponse_Sets200StatusCode()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await HealthCheckResponseWriter.WriteLivenessResponse(_mockHttpContext.Object, healthReport);

        // Assert
        _mockHttpResponse.VerifySet(r => r.StatusCode = StatusCodes.Status200OK);
    }

    [TestMethod]
    public async Task WriteLivenessResponse_SetsJsonContentType()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await HealthCheckResponseWriter.WriteLivenessResponse(_mockHttpContext.Object, healthReport);

        // Assert
        _mockHttpResponse.VerifySet(r => r.ContentType = "application/json");
    }

    [TestMethod]
    public async Task WriteLivenessResponse_IncludesStatusInResponse()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await HealthCheckResponseWriter.WriteLivenessResponse(_mockHttpContext.Object, healthReport);

        // Assert
        _responseBody.Position = 0;
        var reader = new StreamReader(_responseBody);
        var response = await reader.ReadToEndAsync();
        response.Should().Contain("\"status\": \"healthy\"");
    }

    [TestMethod]
    public async Task WriteLivenessResponse_IncludesTimestampInResponse()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await HealthCheckResponseWriter.WriteLivenessResponse(_mockHttpContext.Object, healthReport);

        // Assert
        _responseBody.Position = 0;
        var reader = new StreamReader(_responseBody);
        var response = await reader.ReadToEndAsync();
        response.Should().Contain("timestamp");
    }

    [TestMethod]
    public async Task WriteLivenessResponse_ReturnsSimpleResponse()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await HealthCheckResponseWriter.WriteLivenessResponse(_mockHttpContext.Object, healthReport);

        // Assert - Verify it's a simple response (no checks array)
        _responseBody.Position = 0;
        var reader = new StreamReader(_responseBody);
        var response = await reader.ReadToEndAsync();
        response.Should().NotContain("checks");
        response.Should().NotContain("totalDurationMs");
    }

    #endregion

    #region Helper Methods

    private static HealthReport CreateHealthReport(HealthStatus status)
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            { "test", new HealthReportEntry(status, "Test check", TimeSpan.FromMilliseconds(10), null, null) }
        };
        return new HealthReport(entries, TimeSpan.FromMilliseconds(10));
    }

    #endregion
}
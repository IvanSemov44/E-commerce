using System.Text.Json;
using ECommerce.API.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.HealthChecks;

/// <summary>
/// Tests for the HealthCheckResponseWriter class.
/// Tests JSON response generation for health check endpoints.
/// </summary>
[TestClass]
public class HealthCheckResponseWriterTests
{
    private DefaultHttpContext _httpContext = null!;
    private MemoryStream _responseBody = null!;

    [TestInitialize]
    public void Setup()
    {
        _httpContext = new DefaultHttpContext();
        _responseBody = new MemoryStream();
        _httpContext.Response.Body = _responseBody;
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
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_httpContext, healthReport);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [TestMethod]
    public async Task WriteHealthCheckResponse_DegradedReport_Sets200StatusCode()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Degraded);

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_httpContext, healthReport);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [TestMethod]
    public async Task WriteHealthCheckResponse_UnhealthyReport_Sets503StatusCode()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Unhealthy);

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_httpContext, healthReport);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [TestMethod]
    public async Task WriteHealthCheckResponse_SetsJsonContentType()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_httpContext, healthReport);

        // Assert
        _httpContext.Response.ContentType.Should().Be("application/json");
    }

    [TestMethod]
    public async Task WriteHealthCheckResponse_IncludesStatusInResponse()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_httpContext, healthReport);

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
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_httpContext, healthReport);

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
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_httpContext, healthReport);

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
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_httpContext, healthReport);

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
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_httpContext, healthReport);

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
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_httpContext, healthReport);

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
        await HealthCheckResponseWriter.WriteHealthCheckResponse(_httpContext, healthReport);

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
        await HealthCheckResponseWriter.WriteLivenessResponse(_httpContext, healthReport);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [TestMethod]
    public async Task WriteLivenessResponse_SetsJsonContentType()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await HealthCheckResponseWriter.WriteLivenessResponse(_httpContext, healthReport);

        // Assert
        _httpContext.Response.ContentType.Should().Be("application/json");
    }

    [TestMethod]
    public async Task WriteLivenessResponse_IncludesStatusInResponse()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await HealthCheckResponseWriter.WriteLivenessResponse(_httpContext, healthReport);

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
        await HealthCheckResponseWriter.WriteLivenessResponse(_httpContext, healthReport);

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
        await HealthCheckResponseWriter.WriteLivenessResponse(_httpContext, healthReport);

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
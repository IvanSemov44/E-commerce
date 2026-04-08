using ECommerce.API.Common.Configuration;
using ECommerce.API.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ECommerce.Tests.Unit.HealthChecks;

/// <summary>
/// Tests for the MemoryHealthCheck class.
/// Tests memory health check behavior under various memory conditions.
/// </summary>
[TestClass]
public class MemoryHealthCheckTests
{
    private Mock<IOptions<MonitoringOptions>> _mockOptions = null!;
    private MonitoringOptions _monitoringOptions = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockOptions = new Mock<IOptions<MonitoringOptions>>();
        _monitoringOptions = new MonitoringOptions();
        _mockOptions.Setup(x => x.Value).Returns(_monitoringOptions);
    }

    [TestMethod]
    public async Task CheckHealthAsync_MemoryBelowThreshold_ReturnsHealthy()
    {
        // Arrange
        _monitoringOptions.MemoryThresholdMB = 1024; // 1GB threshold
        var healthCheck = new MemoryHealthCheck(_mockOptions.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("Memory usage normal");
    }

    [TestMethod]
    public async Task CheckHealthAsync_MemoryAbove80Percent_ReturnsHealthyWithWarning()
    {
        // Arrange - Set a very low threshold to trigger the 80% warning
        _monitoringOptions.MemoryThresholdMB = 1; // 1MB threshold (will definitely be exceeded)
        var healthCheck = new MemoryHealthCheck(_mockOptions.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert - Memory will definitely be above 80% of 1MB
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Memory usage high");
    }

    [TestMethod]
    public async Task CheckHealthAsync_MemoryAboveThreshold_ReturnsDegraded()
    {
        // Arrange - Set threshold to 1MB which will definitely be exceeded
        _monitoringOptions.MemoryThresholdMB = 1;
        var healthCheck = new MemoryHealthCheck(_mockOptions.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("exceeds threshold");
    }

    [TestMethod]
    public async Task CheckHealthAsync_IncludesMemoryDataInResult()
    {
        // Arrange
        _monitoringOptions.MemoryThresholdMB = 1024;
        var healthCheck = new MemoryHealthCheck(_mockOptions.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey("AllocatedMB");
        result.Data.Should().ContainKey("ThresholdMB");
        result.Data.Should().ContainKey("Gen0Collections");
        result.Data.Should().ContainKey("Gen1Collections");
        result.Data.Should().ContainKey("Gen2Collections");
    }

    [TestMethod]
    public async Task CheckHealthAsync_DataContainsCorrectThreshold()
    {
        // Arrange
        _monitoringOptions.MemoryThresholdMB = 512;
        var healthCheck = new MemoryHealthCheck(_mockOptions.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data["ThresholdMB"].Should().Be(512);
    }

    [TestMethod]
    public async Task CheckHealthAsync_DataContainsAllocatedMemory()
    {
        // Arrange
        _monitoringOptions.MemoryThresholdMB = 1024;
        var healthCheck = new MemoryHealthCheck(_mockOptions.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        var allocatedMB = (long)result.Data["AllocatedMB"];
        allocatedMB.Should().BeGreaterOrEqualTo(0);
    }

    [TestMethod]
    public async Task CheckHealthAsync_DataContainsGCCounts()
    {
        // Arrange
        _monitoringOptions.MemoryThresholdMB = 1024;
        var healthCheck = new MemoryHealthCheck(_mockOptions.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        var gen0 = (int)result.Data["Gen0Collections"];
        var gen1 = (int)result.Data["Gen1Collections"];
        var gen2 = (int)result.Data["Gen2Collections"];

        gen0.Should().BeGreaterOrEqualTo(0);
        gen1.Should().BeGreaterOrEqualTo(0);
        gen2.Should().BeGreaterOrEqualTo(0);
    }

    [TestMethod]
    public async Task CheckHealthAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        _monitoringOptions.MemoryThresholdMB = 1024;
        var healthCheck = new MemoryHealthCheck(_mockOptions.Object);
        var cts = new CancellationTokenSource();

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    [TestMethod]
    public async Task CheckHealthAsync_UsesConfiguredThreshold()
    {
        // Arrange
        _monitoringOptions.MemoryThresholdMB = 2048; // 2GB
        var healthCheck = new MemoryHealthCheck(_mockOptions.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data["ThresholdMB"].Should().Be(2048);
    }

    [TestMethod]
    public async Task CheckHealthAsync_WithVeryHighThreshold_ReturnsHealthy()
    {
        // Arrange - Set a very high threshold
        _monitoringOptions.MemoryThresholdMB = int.MaxValue;
        var healthCheck = new MemoryHealthCheck(_mockOptions.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [TestMethod]
    public async Task CheckHealthAsync_WithZeroThreshold_ReturnsDegraded()
    {
        // Arrange - Set threshold to 0
        _monitoringOptions.MemoryThresholdMB = 0;
        var healthCheck = new MemoryHealthCheck(_mockOptions.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert - Any memory usage will exceed 0
        result.Status.Should().Be(HealthStatus.Degraded);
    }
}


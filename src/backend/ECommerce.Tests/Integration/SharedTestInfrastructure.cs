namespace ECommerce.Tests.Integration;

/// <summary>
/// Single factory for the entire test assembly. Containers start once, migrations run once.
/// Each test class references SharedTestInfrastructure.Factory — no per-class or per-test setup cost.
/// </summary>
[TestClass]
public static class SharedTestInfrastructure
{
    public static TestWebApplicationFactory Factory { get; private set; } = null!;

    [AssemblyInitialize]
    public static void Init(TestContext _)
    {
        Factory = new TestWebApplicationFactory();
        // Force eager host initialization so parallel test classes don't race to build it.
        // WebApplicationFactory.EnsureServer() is not thread-safe; calling CreateClient() here
        // guarantees the web host (and all migrations) are fully built before any test runs.
        using var warmup = Factory.CreateClient();
    }

    [AssemblyCleanup]
    public static void Cleanup() => Factory?.Dispose();
}

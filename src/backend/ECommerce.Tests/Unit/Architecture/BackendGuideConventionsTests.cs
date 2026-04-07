using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Architecture;

[TestClass]
public class BackendGuideConventionsTests
{
    [TestMethod]
    public void ApiControllers_ShouldNotUseInlinePaginationClampMagicNumbers()
    {
        var repoRoot = GetRepositoryRoot();
        var featuresPath = Path.Combine(repoRoot, "src", "backend", "ECommerce.API", "Features");
        var controllerFiles = Directory.GetFiles(featuresPath, "*Controller.cs", SearchOption.AllDirectories);

        var forbiddenPatterns = new[]
        {
            "if (page < 1)",
            "if (pageSize < 1)",
            "if (pageSize > 100)"
        };

        foreach (var controllerFile in controllerFiles)
        {
            var content = File.ReadAllText(controllerFile);
            foreach (var pattern in forbiddenPatterns)
            {
                Assert.IsFalse(content.Contains(pattern, StringComparison.Ordinal),
                    $"Inline pagination clamp '{pattern}' found in {Path.GetFileName(controllerFile)}. Use PaginationRequestNormalizer instead.");
            }
        }
    }

    [TestMethod]
    public void PriorityIntegrationTests_ShouldNotAllowOkForUnauthorizedOrForbiddenBranches()
    {
        var repoRoot = GetRepositoryRoot();
        var integrationPath = Path.Combine(repoRoot, "src", "backend", "ECommerce.Tests", "Integration");

        var priorityFiles = new[]
        {
            "CartControllerTests.cs",
            "ReviewsControllerTests.cs",
            "PromoCodesControllerTests.cs",
            "OrdersControllerTests.cs",
            "WishlistControllerTests.cs"
        };

        var broadUnauthorizedPattern =
            @"HttpStatusCode\.(Unauthorized|Forbidden)[^\r\n]*\|\|[^\r\n]*HttpStatusCode\.OK|" +
            @"HttpStatusCode\.OK[^\r\n]*\|\|[^\r\n]*HttpStatusCode\.(Unauthorized|Forbidden)|" +
            @"HttpStatusCode\.(Unauthorized|Forbidden)[^\r\n]*or[^\r\n]*HttpStatusCode\.OK|" +
            @"HttpStatusCode\.OK[^\r\n]*or[^\r\n]*HttpStatusCode\.(Unauthorized|Forbidden)";

        foreach (var fileName in priorityFiles)
        {
            var filePath = Path.Combine(integrationPath, fileName);
            var content = File.ReadAllText(filePath);

            Assert.IsFalse(Regex.IsMatch(content, broadUnauthorizedPattern, RegexOptions.IgnoreCase),
                $"Overly broad unauthorized/forbidden assertion allowing OK found in {fileName}.");
        }
    }

    [TestMethod]
    public void HotspotControllers_ShouldPreferRoleOrNullOverThrowingRoleAccessor()
    {
        var repoRoot = GetRepositoryRoot();
        var apiRootPath = Path.Combine(repoRoot, "src", "backend", "ECommerce.API");

        var hotspotFiles = new[]
        {
            Path.Combine("Features", "Ordering", "Controllers", "OrdersController.cs"),
            Path.Combine("Features", "Payments", "Controllers", "PaymentsController.cs"),
            Path.Combine("Features", "Reviews", "Controllers", "ReviewsController.cs"),
            Path.Combine("Features", "Shopping", "Controllers", "WishlistController.cs"),
            Path.Combine("Features", "Identity", "Controllers", "ProfileController.cs")
        };

        foreach (var relativePath in hotspotFiles)
        {
            var filePath = Path.Combine(apiRootPath, relativePath);
            Assert.IsTrue(File.Exists(filePath), $"Expected hotspot controller file not found: {relativePath}");
            var content = File.ReadAllText(filePath);

            Assert.IsFalse(Regex.IsMatch(content, @"_currentUser\.Role(?!OrNull)\b", RegexOptions.None),
                $"Throw-based role accessor usage found in {relativePath}. Prefer RoleOrNull for defensive checks.");
        }
    }

    private static string GetRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current != null)
        {
            var backendApiPath = Path.Combine(current.FullName, "src", "backend", "ECommerce.API");
            if (Directory.Exists(backendApiPath))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test execution directory.");
    }
}

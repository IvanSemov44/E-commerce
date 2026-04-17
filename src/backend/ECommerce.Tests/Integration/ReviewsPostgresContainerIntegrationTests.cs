using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

[TestClass]
public class ReviewsPostgresContainerIntegrationTests
{
    private static readonly Guid SeededProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private TestWebApplicationFactory _factory = null!;
    private HttpClient _customerClient = null!;

    [TestInitialize]
    public void Setup()
    {
        try
        {
            _factory = new TestWebApplicationFactory(
                useReviewsPostgresContainer: true,
                useCatalogPostgresContainer: false);
            _customerClient = _factory.CreateAuthenticatedClient();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Testcontainer failed", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Inconclusive($"PostgreSQL testcontainer is required for this test class. {ex.Message}");
        }
    }

    [TestCleanup]
    public void Cleanup()
    {
        _customerClient?.Dispose();
        _factory?.Dispose();
    }

    [TestMethod]
    public async Task GetProductReviews_WithPostgresContainer_ReturnsOkOrNotFound()
    {
        var response = await _customerClient.GetAsync($"/api/reviews/product/{SeededProductId}?page=1&pageSize=10");
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task GetProductRating_WithPostgresContainer_ReturnsOkOrNotFound()
    {
        var response = await _customerClient.GetAsync($"/api/reviews/product/{SeededProductId}/rating");
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            await response.Content.ReadAsStringAsync());
    }
}

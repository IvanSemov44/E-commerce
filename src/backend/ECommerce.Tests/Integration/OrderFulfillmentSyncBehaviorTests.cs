using System.Net;
using System.Text;
using System.Text.Json;

namespace ECommerce.Tests.Integration;

[TestClass]
public class OrderFulfillmentSyncBehaviorTests
{
    private static readonly Guid ExistingProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private TestWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void Setup() => _factory = new TestWebApplicationFactory();

    [TestCleanup]
    public void Cleanup()
    {
        ConditionalTestAuthHandler.IsAuthenticationEnabled = true;
        ConditionalTestAuthHandler.CurrentUserId = ConditionalTestAuthHandler.TestUserId;
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        _factory?.Dispose();
    }

    [TestMethod]
    public async Task PlaceOrder_InsufficientStock_OrderRejected_InventoryUnchanged()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var stockBefore = await GetInventoryQuantityAsync(client, ExistingProductId, TestContext.CancellationToken);
        var tooLargeQuantity = stockBefore + 1;

        var orderResponse = await client.PostAsync(
            "/api/orders",
            BuildOrderRequest(tooLargeQuantity),
            TestContext.CancellationToken);

        Assert.IsTrue(
            orderResponse.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Expected 400/422 for insufficient stock, got {(int)orderResponse.StatusCode}.");

        var stockAfter = await GetInventoryQuantityAsync(client, ExistingProductId, TestContext.CancellationToken);
        Assert.AreEqual(stockBefore, stockAfter, "Inventory must be unchanged when order is rejected.");
    }

    private static StringContent BuildOrderRequest(int quantity)
    {
        var payload = new
        {
            PaymentMethod = "card",
            ShippingAddress = new
            {
                FirstName = "Test", LastName = "User", Phone = "555-0101",
                StreetLine1 = "123 Sync St", City = "New York",
                State = "NY", PostalCode = "10001", Country = "US"
            },
            Items = new[] { new { ProductId = ExistingProductId.ToString(), Quantity = quantity } }
        };
        return new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    }

    private static async Task<int> GetInventoryQuantityAsync(HttpClient client, Guid productId, CancellationToken ct)
    {
        var response = await client.GetAsync($"/api/inventory/{productId}", ct);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            $"Inventory endpoint returned {response.StatusCode}.");

        var json = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(ct), JsonOptions);
        return json.GetProperty("data").GetProperty("quantity").GetInt32();
    }
}

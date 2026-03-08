using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

[TestClass]
public class AddToCartCreateOrderTests
{
    private TestWebApplicationFactory _factory = null!;

    [TestInitialize]
    public void Setup()
    {
        _factory = new TestWebApplicationFactory();
    }

    [TestCleanup]
    public void Cleanup()
    {
        TestWebApplicationFactory.ResetAuthState();
    }

    [TestMethod]
    public async Task AddToCart_Then_CreateOrder_EndToEnd()
    {
        // Use unauthenticated client for cart (anonymous allowed)
        var anonClient = _factory.CreateClient();

        // Add to cart (anonymous allowed)
        var addBody = new { ProductId = "22222222-2222-2222-2222-222222222222", Quantity = 2 };
        var addResponse = await anonClient.PostAsync("/api/cart/add-item", new StringContent(JsonSerializer.Serialize(addBody), Encoding.UTF8, "application/json"));
        addResponse.EnsureSuccessStatusCode();

        // Create order (use authenticated client with JWT token)
        var authClient = _factory.CreateAuthenticatedClient();
        var createOrder = new
        {
            PaymentMethod = "credit_card",
            ShippingAddress = new
            {
                FirstName = "John",
                LastName = "Doe",
                StreetLine1 = "123 Main St",
                City = "City",
                State = "ST",
                PostalCode = "12345",
                Country = "US",
            },
            Items = new[] { new { ProductId = "22222222-2222-2222-2222-222222222222", Quantity = 2 } }
        };

        // Send order request with authentication
        var orderResponse = await authClient.PostAsync("/api/orders", new StringContent(JsonSerializer.Serialize(createOrder), Encoding.UTF8, "application/json"));
        orderResponse.EnsureSuccessStatusCode();

        var content = await orderResponse.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("\"success\": true") || content.Contains("\"success\":true"),
            "Order creation response should indicate success");
    }
}

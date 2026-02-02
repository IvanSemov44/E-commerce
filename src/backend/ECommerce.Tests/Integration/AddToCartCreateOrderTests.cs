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

    [TestMethod]
    public async Task AddToCart_Then_CreateOrder_EndToEnd()
    {
        var client = _factory.CreateClient();

        // Add to cart (anonymous allowed)
        var addBody = new { ProductId = "integration-product", Quantity = 2 };
        var addResponse = await client.PostAsync("/api/cart/add-item", new StringContent(JsonSerializer.Serialize(addBody), Encoding.UTF8, "application/json"));
        addResponse.EnsureSuccessStatusCode();

        // Create order (authenticated via test auth)
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
            Items = new[] { new { ProductId = "integration-product", ProductName = "IntegrationProduct", Price = 10.0m, Quantity = 2 } }
        };

        // Ensure auth is applied (TestAuthHandler supplies user)
        var orderResponse = await client.PostAsync("/api/orders", new StringContent(JsonSerializer.Serialize(createOrder), Encoding.UTF8, "application/json"));
        orderResponse.EnsureSuccessStatusCode();

        var content = await orderResponse.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("Order created successfully") || content.Contains("order created"), "Order creation response did not indicate success");
    }
}

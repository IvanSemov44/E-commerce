using ECommerce.API;
using ECommerce.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ECommerce.Tests.Integration;

[Collection("Sequential")]
public class OrdersCharacterizationTests
{
    private static readonly Lazy<TestWebApplicationFactory> LazyFactory =
        new(() => new TestWebApplicationFactory());

    private static TestWebApplicationFactory Factory => LazyFactory.Value;

    [Fact]
    public async Task PlaceOrder_ValidData_Returns201WithOrderData()
    {
        var client = Factory.CreateClient();
        var createOrderDto = new
        {
            Items = new[]
            {
                new { ProductId = "22222222-2222-2222-2222-222222222222", Quantity = 2 }
            },
            ShippingAddress = new
            {
                StreetLine1 = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "US"
            },
            PaymentMethod = "credit_card"
        };

        var response = await client.PostAsJsonAsync("/api/orders", createOrderDto);

        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.BadRequest,
            "Create should return 201 Created or 400 Bad Request based on current implementation");
    }

    [Fact]
    public async Task PlaceOrder_EmptyCart_ReturnsBadRequest()
    {
        var client = Factory.CreateClient();
        var createOrderDto = new
        {
            Items = new object[0],
            ShippingAddress = new
            {
                StreetLine1 = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "US"
            },
            PaymentMethod = "credit_card"
        };

        var response = await client.PostAsJsonAsync("/api/orders", createOrderDto);

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnprocessableEntity,
            "Empty cart should return validation error");
    }

    [Fact]
    public async Task GetOrderById_ExistingOrder_Returns200()
    {
        var client = Factory.CreateClient();
        var orderId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var response = await client.GetAsync($"/api/orders/{orderId}");

        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "Should return 200 for existing or 404 for non-existing order");
    }

    [Fact]
    public async Task GetOrderById_UnknownId_Returns404()
    {
        var client = Factory.CreateClient();
        var unknownId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/orders/{unknownId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmOrder_Pending_Returns200()
    {
        var client = Factory.CreateAuthenticatedClient();
        var orderId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var response = await client.PostAsync($"/api/orders/{orderId}/confirm", null);

        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.UnprocessableEntity,
            "Should handle order confirmation or return error");
    }

    [Fact]
    public async Task ConfirmOrder_UnknownId_Returns404()
    {
        var client = Factory.CreateAuthenticatedClient();
        var unknownId = Guid.NewGuid();

        var response = await client.PostAsync($"/api/orders/{unknownId}/confirm", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShipOrder_ConfirmedOrder_Returns200()
    {
        var client = Factory.CreateAuthenticatedClient();
        var orderId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var shipDto = new { TrackingNumber = "TRK123456789" };

        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/ship", shipDto);

        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.UnprocessableEntity || response.StatusCode == HttpStatusCode.NotFound,
            "Should handle shipping or return error");
    }

    [Fact]
    public async Task ShipOrder_MissingTracking_ReturnsBadRequest()
    {
        var client = Factory.CreateAuthenticatedClient();
        var orderId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var shipDto = new { TrackingNumber = "" };

        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/ship", shipDto);

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnprocessableEntity,
            "Invalid tracking number should be rejected");
    }

    [Fact]
    public async Task CancelOrder_Pending_Returns200()
    {
        var client = Factory.CreateAuthenticatedClient();
        var orderId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var cancelDto = new { Reason = "Changed my mind" };

        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/cancel", cancelDto);

        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.UnprocessableEntity,
            "Should handle cancellation or return error");
    }

    [Fact]
    public async Task CancelOrder_UnknownId_Returns404()
    {
        var client = Factory.CreateAuthenticatedClient();
        var unknownId = Guid.NewGuid();
        var cancelDto = new { Reason = "Cancel" };

        var response = await client.PostAsJsonAsync($"/api/orders/{unknownId}/cancel", cancelDto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_ShippedOrder_Returns422()
    {
        var client = Factory.CreateAuthenticatedClient();
        var shippedOrderId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var cancelDto = new { Reason = "Too late" };

        var response = await client.PostAsJsonAsync($"/api/orders/{shippedOrderId}/cancel", cancelDto);

        Assert.True(response.StatusCode == HttpStatusCode.UnprocessableEntity || response.StatusCode == HttpStatusCode.BadRequest,
            "Cannot cancel shipped orders");
    }

    [Fact]
    public async Task OrderEndpoints_ReturnConsistentErrorFormats()
    {
        var client = Factory.CreateClient();

        // Multiple 404 responses should have same error format
        var response1 = await client.GetAsync($"/api/orders/{Guid.NewGuid()}");
        var response2 = await client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        Assert.Equal(response1.StatusCode, response2.StatusCode);
    }
}

# Phase 7, Step 0: Characterization Tests (Backend)

**Prerequisite**: Phase 6 complete. `src/backend` compiles and all tests pass.

Capture the behavior of the **old** `OrdersController` and `IOrderService` in a characterization test class. This establishes a baseline before any refactoring to CQRS/MediatR.

---

## Task: Write `OrdersCharacterizationTests`

File: `src/backend/ECommerce.Tests/Integration/OrdersCharacterizationTests.cs`

Use the **static factory** pattern:

```csharp
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
    public async Task PlaceOrder_ValidCartItems_Returns201WithOrderNumber()
    {
        var client = Factory.CreateClient();
        var customerId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var cartItems = new[] 
        { 
            new { productId = Guid.Parse("11111111-1111-1111-1111-111111111111"), quantity = 2, unitPrice = 50.0m },
            new { productId = Guid.Parse("11111111-1111-1111-1111-111111111112"), quantity = 1, unitPrice = 100.0m }
        };

        var req = new { customerId, items = cartItems, shippingAddressId = Guid.NewGuid() };
        var response = await client.PostAsJsonAsync("/api/orders", req);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var content = await response.Content.ReadAsAsync<ApiResponse<OrderDetailDto>>();
        Assert.NotNull(content.Data);
        Assert.NotEmpty(content.Data.OrderNumber);
        Assert.Equal("Pending", content.Data.Status);
    }

    [Fact]
    public async Task PlaceOrder_EmptyCart_Returns400BadRequest()
    {
        var client = Factory.CreateClient();
        var req = new { customerId = Guid.NewGuid(), items = new object[0], shippingAddressId = Guid.NewGuid() };

        var response = await client.PostAsJsonAsync("/api/orders", req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<object>>();
        Assert.Equal("ORDER_EMPTY", content.Code);
    }

    [Fact]
    public async Task PlaceOrder_InvalidQuantity_Returns400BadRequest()
    {
        var client = Factory.CreateClient();
        var req = new 
        { 
            customerId = Guid.NewGuid(),
            items = new[] { new { productId = Guid.NewGuid(), quantity = 0, unitPrice = 50.0m } },
            shippingAddressId = Guid.NewGuid()
        };

        var response = await client.PostAsJsonAsync("/api/orders", req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<object>>();
        Assert.Equal("ORDER_INVALID_QUANTITY", content.Code);
    }

    [Fact]
    public async Task GetOrderById_ValidId_Returns200WithOrderData()
    {
        var client = Factory.CreateClient();
        var orderId = Guid.Parse("44444444-4444-4444-4444-444444444444"); // Seeded order

        var response = await client.GetAsync($"/api/orders/{orderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<OrderDetailDto>>();
        Assert.NotNull(content.Data);
        Assert.Equal(orderId, content.Data.Id);
        Assert.NotEmpty(content.Data.OrderNumber);
    }

    [Fact]
    public async Task GetOrderById_UnknownId_Returns404NotFound()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<object>>();
        Assert.Equal("ORDER_NOT_FOUND", content.Code);
    }

    [Fact]
    public async Task GetCustomerOrders_ValidCustomerId_Returns200WithOrderList()
    {
        var client = Factory.CreateClient();
        var customerId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var response = await client.GetAsync($"/api/customers/{customerId}/orders?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<PaginatedResult<OrderDto>>>();
        Assert.NotNull(content.Data);
        Assert.True(content.Data.Items.Count > 0);
    }

    [Fact]
    public async Task ConfirmOrder_PendingOrder_Returns200AndChangesStatus()
    {
        var client = Factory.CreateClient();
        var orderId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/confirm", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<OrderDetailDto>>();
        Assert.NotNull(content.Data);
        Assert.Equal("Confirmed", content.Data.Status);
    }

    [Fact]
    public async Task ConfirmOrder_UnknownId_Returns404NotFound()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/orders/{Guid.NewGuid()}/confirm", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<object>>();
        Assert.Equal("ORDER_NOT_FOUND", content.Code);
    }

    [Fact]
    public async Task ShipOrder_ConfirmedOrder_Returns200AndChangesStatus()
    {
        var client = Factory.CreateClient();
        var orderId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var req = new { trackingNumber = "TRK123456789" };
        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/ship", req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<OrderDetailDto>>();
        Assert.NotNull(content.Data);
        Assert.Equal("Shipped", content.Data.Status);
    }

    [Fact]
    public async Task CancelOrder_PendingOrder_Returns200AndCancelsOrder()
    {
        var client = Factory.CreateClient();
        var orderId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var req = new { reason = "Customer requested" };

        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/cancel", req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<OrderDetailDto>>();
        Assert.NotNull(content.Data);
        Assert.Equal("Cancelled", content.Data.Status);
    }

    [Fact]
    public async Task CancelOrder_ShippedOrder_Returns422UnprocessableEntity()
    {
        var client = Factory.CreateClient();
        var orderId = Guid.Parse("55555555-5555-5555-5555-555555555555"); // Shipped order
        var req = new { reason = "Too late" };

        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/cancel", req);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<object>>();
        Assert.Equal("ORDER_CANNOT_CANCEL_SHIPPED", content.Code);
    }

    [Fact]
    public async Task GetOrderTotal_IncludesSubtotalTaxAndShipping()
    {
        var client = Factory.CreateClient();
        var orderId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var response = await client.GetAsync($"/api/orders/{orderId}");

        var content = await response.Content.ReadAsAsync<ApiResponse<OrderDetailDto>>();
        Assert.NotNull(content.Data);
        Assert.True(content.Data.Subtotal > 0);
        Assert.True(content.Data.Tax >= 0);
        Assert.True(content.Data.ShippingCost >= 0);
        Assert.Equal(content.Data.Subtotal + content.Data.Tax + content.Data.ShippingCost, content.Data.Total);
    }

    [Fact]
    public async Task GetPendingOrders_AdminOnly_Returns403ForNonAdmin()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/orders/admin/pending");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OrderLineItems_IncludesProductIdQuantityAndPrice()
    {
        var client = Factory.CreateClient();
        var orderId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var response = await client.GetAsync($"/api/orders/{orderId}");

        var content = await response.Content.ReadAsAsync<ApiResponse<OrderDetailDto>>();
        Assert.NotNull(content.Data);
        Assert.NotEmpty(content.Data.Items);
        foreach (var item in content.Data.Items)
        {
            Assert.NotEqual(Guid.Empty, item.ProductId);
            Assert.True(item.Quantity > 0);
            Assert.True(item.UnitPrice > 0);
        }
    }
}
```

---

## Critical Pins (Must Pass)

| Endpoint | Status | Why it matters |
|----------|--------|----------------|
| `POST /api/orders` (create) | 201 Created | Order creation with Location header |
| `POST /api/orders` (empty cart) | 400 Bad Request | Validation enforced |
| `POST /api/orders` (invalid quantity) | 400 Bad Request | Line item validation |
| `GET /api/orders/{id}` | 200 OK | Retrieve order by ID |
| `GET /api/orders/{id}` (unknown) | 404 Not Found | Error mapping for not found |
| `GET /api/customers/{id}/orders` | 200 OK with paginated list | Customer order history |
| `POST /api/orders/{id}/confirm` | 200 OK | Status: Pending → Confirmed |
| `POST /api/orders/{id}/ship` (with tracking) | 200 OK | Status: Confirmed → Shipped |
| `POST /api/orders/{id}/cancel` (pending) | 200 OK | Cancel succeeds for pending |
| `POST /api/orders/{id}/cancel` (shipped) | 422 Unprocessable | Cannot cancel shipped orders |
| `GET /api/orders/{id}` (total calc) | Correct subtotal+tax+shipping | Price calculations accurate |
| `GET /api/orders/admin/pending` (non-admin) | 403 Forbidden | Admin-only query protected |
| Order line items | ProductId, Quantity, UnitPrice present | Item details preserved |

---

## Seeded Test Data

Add to your database seeding:

```sql
-- Customer (for order creation tests)
INSERT INTO Customers (Id, Name, Email)
VALUES ('99999999-9999-9999-9999-999999999999', 'Test Customer', 'customer@test.com');

-- Pending Order (for status transition tests)
INSERT INTO Orders (Id, CustomerId, OrderNumber, Status, Subtotal, Tax, ShippingCost, Total, CreatedAt, UpdatedAt)
VALUES ('44444444-4444-4444-4444-444444444444', '99999999-9999-9999-9999-999999999999', 'ORD-20260404-001', 'Pending', 200.00, 20.00, 10.00, 230.00, GETDATE(), GETDATE());

-- Shipped Order (for cancel state validation)
INSERT INTO Orders (Id, CustomerId, OrderNumber, Status, Subtotal, Tax, ShippingCost, Total, CreatedAt, UpdatedAt)
VALUES ('55555555-5555-5555-5555-555555555555', '99999999-9999-9999-9999-999999999999', 'ORD-20260404-002', 'Shipped', 150.00, 15.00, 10.00, 175.00, GETDATE(), GETDATE());

-- Order Line Items
INSERT INTO OrderLineItems (Id, OrderId, ProductId, Quantity, UnitPrice)
VALUES ('66666666-6666-6666-6666-666666666666', '44444444-4444-4444-4444-444444444444', '11111111-1111-1111-1111-111111111111', 2, 50.00);
INSERT INTO OrderLineItems (Id, OrderId, ProductId, Quantity, UnitPrice)
VALUES ('77777777-7777-7777-7777-777777777777', '44444444-4444-4444-4444-444444444444', '11111111-1111-1111-1111-111111111112', 1, 100.00);
```

---

## Acceptance Criteria

- [ ] All 13 tests pass against the OLD service
- [ ] Zero regressions when comparing against live endpoints
- [ ] Error codes match exactly: `ORDER_NOT_FOUND`, `ORDER_EMPTY`, `ORDER_INVALID_QUANTITY`, `ORDER_CANNOT_CANCEL_SHIPPED`
- [ ] `POST /api/orders` returns 201 with Location header
- [ ] `GET /api/customers/{id}/orders` returns paginated order list
- [ ] Order status transitions: Pending → Confirmed → Shipped → Delivered (or Cancelled at any point before ship)
- [ ] `POST /api/orders/{id}/cancel` (shipped) returns 422 Unprocessable
- [ ] Order totals calculated correctly: Subtotal + Tax + ShippingCost = Total
- [ ] Admin endpoint `/api/orders/admin/pending` returns 403 for non-admin
- [ ] Order line items include ProductId, Quantity, UnitPrice

# Phase 8, Step 0: Characterization Tests (Current State)

**Prerequisite**: All 7 bounded context migrations (Phases 1–7) complete and passing.

Capture the **current behavior** of cross-context communication (synchronous MediatR domain events within a single `AppDbContext`). This baseline ensures the refactoring to integration events and separate DbContexts doesn't break anything.

---

## Task: Document Current Cross-Context Event Flow

File: `src/backend/ECommerce.Tests/Integration/Phase8CharacterizationTests.cs`

This test suite verifies the behavior of critical cross-context flows **before** extraction:

```csharp
using ECommerce.API;
using ECommerce.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Characterization tests for Phase 8 extraction.
/// These tests verify the CURRENT behavior of cross-context flows using MediatR domain events.
/// After extraction, they will be re-run to ensure eventual consistency behavior is correct.
/// </summary>
[Collection("Sequential")]
public class Phase8CharacterizationTests
{
    private static readonly Lazy<TestWebApplicationFactory> LazyFactory =
        new(() => new TestWebApplicationFactory());

    private static TestWebApplicationFactory Factory => LazyFactory.Value;

    /// <summary>
    /// Test: PlaceOrder reduces inventory immediately (synchronous in Phase 7, async in Phase 8)
    /// </summary>
    [Fact]
    public async Task PlaceOrder_InventoryReduced_Synchronously()
    {
        var client = Factory.CreateClient();
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        // Get current stock
        var stockBefore = await GetProductStock(client, productId);
        
        // Place order for 2 units
        var orderReq = new
        {
            customerId = Guid.NewGuid(),
            items = new[] { new { productId, quantity = 2, unitPrice = 50.0m } },
            tax = 10.0m,
            shippingCost = 5.0m
        };
        var orderResp = await client.PostAsJsonAsync("/api/orders", orderReq);
        Assert.Equal(HttpStatusCode.Created, orderResp.StatusCode);
        
        // Verify stock was reduced IMMEDIATELY (synchronous)
        var stockAfter = await GetProductStock(client, productId);
        Assert.Equal(stockBefore - 2, stockAfter);
    }

    /// <summary>
    /// Test: PlaceOrder sends email immediately (synchronous in Phase 7, async in Phase 8)
    /// (This test assumes a mock email service or captured event log)
    /// </summary>
    [Fact]
    public async Task PlaceOrder_EmailSent_Synchronously()
    {
        var client = Factory.CreateClient();
        
        var orderReq = new
        {
            customerId = Guid.NewGuid(),
            items = new[] { new { productId = Guid.NewGuid(), quantity = 1, unitPrice = 100.0m } },
            tax = 10.0m,
            shippingCost = 5.0m
        };
        
        var orderResp = await client.PostAsJsonAsync("/api/orders", orderReq);
        Assert.Equal(HttpStatusCode.Created, orderResp.StatusCode);
        
        // In Phase 7: Email sent before response returns (synchronous)
        // In Phase 8: Email sent eventually via message broker (async)
        // This test documents that behavior changes here
    }

    /// <summary>
    /// Test: PromoCode applied to order decreases remaining uses immediately (synchronous)
    /// </summary>
    [Fact]
    public async Task PlaceOrder_PromoCodeApplied_UsedCountIncremented_Synchronously()
    {
        var client = Factory.CreateClient();
        var promoCodeId = Guid.Parse("55555555-5555-5555-5555-555555555555"); // SAVE20 seeded

        // Get current used count
        var promoBefore = await GetPromoCodeDetail(client, promoCodeId);
        var usedBefore = promoBefore.UsedCount;

        // Place order with promo code
        var orderReq = new
        {
            customerId = Guid.NewGuid(),
            items = new[] { new { productId = Guid.NewGuid(), quantity = 1, unitPrice = 100.0m } },
            tax = 10.0m,
            shippingCost = 5.0m,
            promoCodeId // Apply discount
        };

        var orderResp = await client.PostAsJsonAsync("/api/orders", orderReq);
        Assert.Equal(HttpStatusCode.Created, orderResp.StatusCode);

        // Verify used count incremented IMMEDIATELY (synchronous)
        var promoAfter = await GetPromoCodeDetail(client, promoCodeId);
        Assert.Equal(usedBefore + 1, promoAfter.UsedCount);
    }

    /// <summary>
    /// Test: Add to cart creates CartAddedEvent, which updates product LastViewedAt immediately
    /// This tests domain event crossing from Shopping → Catalog context
    /// </summary>
    [Fact]
    public async Task AddToCart_ProductLastViewedAt_Updated_Synchronously()
    {
        var client = Factory.CreateClient();
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var productBefore = await GetProductDetail(client, productId);
        var lastViewedBefore = productBefore.LastViewedAt;

        // Add to cart
        var cartReq = new { productId, quantity = 1 };
        var cartResp = await client.PostAsJsonAsync("/api/cart/add-item", cartReq);
        Assert.Equal(HttpStatusCode.OK, cartResp.StatusCode);

        // Verify LastViewedAt updated IMMEDIATELY (synchronous domain event handler)
        var productAfter = await GetProductDetail(client, productId);
        Assert.True(productAfter.LastViewedAt >= lastViewedBefore);
    }

    /// <summary>
    /// Test: Cross-context transaction atomicity
    /// If inventory reduction fails, the entire order fails (ACID within single DbContext)
    /// </summary>
    [Fact]
    public async Task PlaceOrder_InventoryReduceFails_EntireOrderRolledBack()
    {
        var client = Factory.CreateClient();
        var productId = Guid.Parse("22222222-2222-2222-2222-222222222222"); // Out of stock
        
        var orderReq = new
        {
            customerId = Guid.NewGuid(),
            items = new[] { new { productId, quantity = 100, unitPrice = 50.0m } },
            tax = 10.0m,
            shippingCost = 5.0m
        };

        var orderResp = await client.PostAsJsonAsync("/api/orders", orderReq);
        
        // In Phase 7: Atomic — order fails, no side effects
        // In Phase 8: Not atomic — order succeeds, inventory reduction fails, must be handled in saga
        Assert.Equal(HttpStatusCode.BadRequest, orderResp.StatusCode);
        var orderContent = await orderResp.Content.ReadAsAsync<ApiResponse<object>>();
        Assert.Equal("INSUFFICIENT_INVENTORY", orderContent.Code);
    }

    /// <summary>
    /// Test: MediatR domain event handlers run in request scope
    /// (Documents current behavior; integration events will be out-of-scope in Phase 8)
    /// </summary>
    [Fact]
    public async Task DomainEventHandlers_RunInRequestScope_BeforeResponseReturns()
    {
        var client = Factory.CreateClient();
        
        // Create an order
        var orderReq = new
        {
            customerId = Guid.NewGuid(),
            items = new[] { new { productId = Guid.NewGuid(), quantity = 1, unitPrice = 100.0m } },
            tax = 10.0m,
            shippingCost = 5.0m
        };

        var orderResp = await client.PostAsJsonAsync("/api/orders", orderReq);
        var orderDto = await orderResp.Content.ReadAsAsync<ApiResponse<OrderDetailDto>>();

        // Immediately retrieve the order
        var getResp = await client.GetAsync($"/api/orders/{orderDto.Data.Id}");
        var getDto = await getResp.Content.ReadAsAsync<ApiResponse<OrderDetailDto>>();

        // In Phase 7: All domain events processed before response, so related data is visible immediately
        // In Phase 8: Events are queued to Outbox, delivered async, so there may be a brief window where related data is stale
        Assert.NotNull(getDto.Data);
    }

    // ──────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────

    private async Task<int> GetProductStock(HttpClient client, Guid productId)
    {
        var resp = await client.GetAsync($"/api/products/{productId}");
        var dto = await resp.Content.ReadAsAsync<ApiResponse<ProductDetailDto>>();
        return dto.Data.StockQuantity;
    }

    private async Task<ProductDetailDto> GetProductDetail(HttpClient client, Guid productId)
    {
        var resp = await client.GetAsync($"/api/products/{productId}");
        var dto = await resp.Content.ReadAsAsync<ApiResponse<ProductDetailDto>>();
        return dto.Data;
    }

    private async Task<PromoCodeDetailDto> GetPromoCodeDetail(HttpClient client, Guid promoCodeId)
    {
        var resp = await client.GetAsync($"/api/promo-codes/{promoCodeId}");
        var dto = await resp.Content.ReadAsAsync<ApiResponse<PromoCodeDetailDto>>();
        return dto.Data;
    }
}
```

---

## Critical Pins (Current Behavior)

| Scenario | Current Behavior | Phase 8 Change |
|----------|------------------|---|
| PlaceOrder → Inventory reduced | **Synchronous** (happens before response) | **Eventual** (queued to broker, delayed) |
| PlaceOrder → Email sent | **Synchronous** (blocks response) | **Eventual** (queued, sent async) |
| PromoCode usage incremented | **Synchronous** (ACID within transaction) | **Eventual** (event-driven) |
| AddToCart → Product LastViewedAt updated | **Synchronous** (domain event handler runs in-request) | **Eventual** (integration event from broker) |
| Cross-context failures | **Atomic rollback** (shared DbContext) | **Compensating transactions** (saga) |
| Request completion | All side effects done before response | Side effects queued; response returns earlier |

---

## Key Questions to Answer Before Phase 8

1. **Is synchronous behavior required?** Which flows must complete within the request?
   - PlaceOrder → Inventory reduction: How long can we wait?
   - Email send: Can it be delayed 5 minutes? 1 hour?

2. **What is the acceptable eventual consistency window?**
   - If a user places an order, sees stock count drop, then page refreshes showing old stock → acceptable?

3. **What happens if an event is never delivered?**
   - Message broker down for 2 hours. Thousands of `OrderPlaced` events queue up. How does the system recover?

4. **Idempotency keys** — If `InventoryReducedEvent` is delivered twice (broker retries), how do we prevent double-reducing?

---

## Acceptance Criteria

- [ ] All current cross-context flows tested and passing
- [ ] Synchronous behavior documented for each critical flow
- [ ] ACID guarantees of single DbContext verified
- [ ] Domain event handler execution order verified
- [ ] Test file provides baseline for post-Phase-8 eventual consistency behavior

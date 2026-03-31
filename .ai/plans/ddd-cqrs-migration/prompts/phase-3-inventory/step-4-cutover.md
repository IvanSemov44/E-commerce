# Phase 3, Step 4: Inventory Cutover

**Prerequisite**: Steps 1–3 are complete, `dotnet build` is clean, and all existing tests pass.

**This is the point of no return.** Do NOT start this step until the new handlers are wired up and all tests pass against them.

---

## Pre-Cutover Verification

Run ALL three test suites and confirm all pass before touching the controllers:

```bash
# 1. Integration tests (InMemory DB — fast)
cd src/backend
dotnet test  # Must be green (current total)

# 2. Characterization tests subset (confirms baseline)
dotnet test ECommerce.Tests/ECommerce.Tests.csproj \
    --filter "FullyQualifiedName~InventoryCharacterizationTests"

# 3. E2E tests (real PostgreSQL — backend must be running)
cd src/frontend/storefront
npx playwright test api-inventory.spec.ts --reporter=list
```

All three must be green. If any fail here, fix the tests (wrong baseline assumption) — do NOT proceed with migration until they pass.

---

## Task: Update InventoryController and Delete Old Service

### 0. Keep existing request DTOs

The existing controller accepts `AdjustStockRequest`, `BulkStockUpdateRequest`, `StockCheckRequest`, etc. **Keep these DTOs.** Map from DTO → command inside the controller action. Do not change the API surface (the frontend depends on it).

```csharp
// Existing: controller accepts AdjustStockRequest
[HttpPost("{productId}/adjust")]
public async Task<IActionResult> AdjustStock(Guid productId, [FromBody] AdjustStockRequest request, ...)
{
    var result = await _mediator.Send(
        new AdjustStockCommand(productId, request.Quantity, request.Reason ?? "adjustment"), ct);
    ...
}
```

### 0b. Error code → HTTP status mapping

Use this table for ALL Inventory result → response mappings:

| Error code | HTTP status | Reason |
|---|---|---|
| `VALIDATION_FAILED` | 400 Bad Request | FluentValidation failed |
| `INSUFFICIENT_STOCK` | 422 Unprocessable | Business rule — not enough stock |
| `STOCK_NEGATIVE` / `REDUCE_AMOUNT_INVALID` / `INCREASE_AMOUNT_INVALID` / `THRESHOLD_NEGATIVE` | 422 Unprocessable | Domain validation |
| `INVENTORY_ITEM_NOT_FOUND` | 404 Not Found | No inventory record for this product |

In code:
```csharp
private IActionResult MapInventoryResult(DomainError error) => error.Code switch
{
    "INVENTORY_ITEM_NOT_FOUND"
        => NotFound(ApiResponse.Fail(error.Message, error.Code)),

    "INSUFFICIENT_STOCK" or "STOCK_NEGATIVE"
    or "REDUCE_AMOUNT_INVALID" or "INCREASE_AMOUNT_INVALID"
    or "THRESHOLD_NEGATIVE"
        => UnprocessableEntity(ApiResponse.Fail(error.Message, error.Code)),

    "VALIDATION_FAILED"
        => BadRequest(ApiResponse.Fail(error.Message, error.Code)),

    _ => StatusCode(500, ApiResponse.Fail("An unexpected error occurred.", error.Code))
};
```

### 1. Rewrite InventoryController

Replace `IInventoryService` injection with `IMediator`. Keep the constructor slim, keep the route attributes identical, keep the existing request DTO types.

```csharp
using ECommerce.API.ActionFilters;
using ECommerce.Application.DTOs.Inventory;   // Keep existing request DTOs
using ECommerce.Inventory.Application.Commands.IncreaseStock;
using ECommerce.Inventory.Application.Commands.ReduceStock;
using ECommerce.Inventory.Application.Commands.AdjustStock;
using ECommerce.Inventory.Application.Queries.GetInventory;
using ECommerce.Inventory.Application.Queries.GetInventoryByProductId;
using ECommerce.Inventory.Application.Queries.GetInventoryHistory;
using ECommerce.Inventory.Application.Queries.GetLowStockItems;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class InventoryController(IMediator _mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllInventory(
        [FromQuery] InventoryQueryParameters parameters,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetInventoryQuery(parameters.Page, parameters.PageSize, parameters.Search, parameters.LowStockOnly ?? false), ct);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Inventory retrieved successfully"))
            : MapInventoryResult(result.GetErrorOrThrow());
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockProducts(
        [FromQuery] int? threshold = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetLowStockItemsQuery(threshold), ct);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Low stock products retrieved successfully"))
            : MapInventoryResult(result.GetErrorOrThrow());
    }

    [HttpGet("{productId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductStock(Guid productId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetInventoryByProductIdQuery(productId), ct);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Product stock retrieved successfully"))
            : MapInventoryResult(result.GetErrorOrThrow());
    }

    [HttpGet("{productId:guid}/available")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckAvailableQuantity(Guid productId, [FromQuery] int quantity, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetInventoryByProductIdQuery(productId), ct);
        if (!result.IsSuccess) return MapInventoryResult(result.GetErrorOrThrow());

        var item = result.GetDataOrThrow();
        var isAvailable = item.Quantity >= quantity;
        return Ok(ApiResponse<object>.Ok(
            new { ProductId = productId, RequestedQuantity = quantity, IsAvailable = isAvailable },
            "Availability check completed"));
    }

    [HttpGet("{productId}/history")]
    public async Task<IActionResult> GetInventoryHistory(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetInventoryHistoryQuery(productId, page, pageSize), ct);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Inventory history retrieved successfully"))
            : MapInventoryResult(result.GetErrorOrThrow());
    }

    [HttpPost("{productId}/adjust")]
    [ValidationFilter]
    public async Task<IActionResult> AdjustStock(Guid productId, [FromBody] AdjustStockRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new AdjustStockCommand(productId, request.Quantity, request.Reason ?? "adjustment"), ct);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Stock adjusted successfully"))
            : MapInventoryResult(result.GetErrorOrThrow());
    }

    [HttpPost("{productId}/restock")]
    [ValidationFilter]
    public async Task<IActionResult> RestockProduct(Guid productId, [FromBody] AdjustStockRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new IncreaseStockCommand(productId, request.Quantity, request.Reason ?? "restock"), ct);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), $"Stock increased by {request.Quantity} units"))
            : MapInventoryResult(result.GetErrorOrThrow());
    }

    [HttpPost("check-availability")]
    [AllowAnonymous]
    [ValidationFilter]
    public async Task<IActionResult> CheckStockAvailability([FromBody] StockCheckRequest request, CancellationToken ct)
    {
        // Check each item in the request against the new InventoryItemRepository
        var issues = new List<object>();
        bool allAvailable = true;

        foreach (var item in request.Items)
        {
            var result = await _mediator.Send(new GetInventoryByProductIdQuery(item.ProductId), ct);
            if (!result.IsSuccess)
            {
                issues.Add(new { item.ProductId, Message = "Product not found" });
                allAvailable = false;
                continue;
            }
            var inv = result.GetDataOrThrow();
            if (inv.Quantity < item.Quantity)
            {
                issues.Add(new { item.ProductId, Available = inv.Quantity, Requested = item.Quantity, Message = "Insufficient stock" });
                allAvailable = false;
            }
        }

        return Ok(ApiResponse<object>.Ok(
            new { IsAvailable = allAvailable, Issues = issues },
            allAvailable ? "All items are available" : "Some items have stock issues"));
    }

    [HttpPut("{productId:guid}")]
    [ValidationFilter]
    public async Task<IActionResult> UpdateProductStock(Guid productId, [FromBody] AdjustStockRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new AdjustStockCommand(productId, request.Quantity, request.Reason ?? "stock_update"), ct);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Product stock updated successfully"))
            : MapInventoryResult(result.GetErrorOrThrow());
    }

    [HttpPut("bulk-update")]
    [ValidationFilter]
    public async Task<IActionResult> BulkUpdateStock([FromBody] BulkStockUpdateRequest request, CancellationToken ct)
    {
        var responses = new List<object>();
        foreach (var update in request.Updates)
        {
            var result = await _mediator.Send(
                new AdjustStockCommand(update.ProductId, update.Quantity, "bulk_update"), ct);
            if (result.IsSuccess) responses.Add(result.GetDataOrThrow());
        }

        return Ok(ApiResponse<object>.Ok(
            new { Items = responses, TotalCount = responses.Count },
            "Stock updated successfully"));
    }

    private IActionResult MapInventoryResult(DomainError error) => error.Code switch
    {
        "INVENTORY_ITEM_NOT_FOUND"
            => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),

        "INSUFFICIENT_STOCK" or "STOCK_NEGATIVE"
        or "REDUCE_AMOUNT_INVALID" or "INCREASE_AMOUNT_INVALID"
        or "THRESHOLD_NEGATIVE"
            => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),

        "VALIDATION_FAILED"
            => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),

        _ => StatusCode(500, ApiResponse<object>.Failure("An unexpected error occurred.", error.Code))
    };
}
```

### 2. Delete old InventoryService

Once the controller is updated and all tests pass:

```bash
# Delete the old service and interface
rm src/backend/ECommerce.Application/Services/InventoryService.cs
rm src/backend/ECommerce.Application/Interfaces/IInventoryService.cs
```

Remove the DI registration from `Program.cs`:
```csharp
// REMOVE this line:
builder.Services.AddScoped<IInventoryService, InventoryService>();
```

### 3. Run the data migration

```bash
cd src/backend
dotnet ef database update --project ECommerce.Infrastructure/ECommerce.Infrastructure.csproj \
    --startup-project ECommerce.API/ECommerce.API.csproj
```

Verify on the database:
```sql
-- InventoryItems count must equal Products count
SELECT COUNT(*) FROM "InventoryItems";
SELECT COUNT(*) FROM "Products";

-- Products table must no longer have stock columns
SELECT column_name FROM information_schema.columns
WHERE table_name = 'Products'
AND column_name IN ('StockQuantity', 'LowStockThreshold');
-- Must return 0 rows
```

---

## Post-Cutover Verification

Run all three test suites again and confirm they still pass:

```bash
# 1. Integration tests
cd src/backend
dotnet test

# 2. Characterization tests
dotnet test ECommerce.Tests/ECommerce.Tests.csproj \
    --filter "FullyQualifiedName~InventoryCharacterizationTests"

# 3. E2E tests (backend must be running with migrated PostgreSQL)
cd src/frontend/storefront
npx playwright test api-inventory.spec.ts --reporter=list
```

---

## Acceptance Criteria

- [ ] Pre-cutover: all characterization tests pass against OLD service
- [ ] Pre-cutover: all e2e tests pass against OLD service (real PostgreSQL baseline)
- [ ] `InventoryController` updated to inject `IMediator` instead of `IInventoryService`
- [ ] All existing route paths and HTTP methods preserved (GET /inventory, POST /adjust, etc.)
- [ ] Error code → HTTP status mapping table implemented in `MapInventoryResult`
- [ ] `INSUFFICIENT_STOCK` → 422 Unprocessable (not 400)
- [ ] `INVENTORY_ITEM_NOT_FOUND` → 404 Not Found
- [ ] EF migration applied: `InventoryItems` table created, stock data migrated from `Products`
- [ ] `Products.StockQuantity` and `Products.LowStockThreshold` columns dropped
- [ ] Old `InventoryService.cs` and `IInventoryService.cs` deleted
- [ ] Post-cutover: all characterization tests still pass against NEW handlers
- [ ] Post-cutover: all e2e tests still pass against NEW handlers (real PostgreSQL)

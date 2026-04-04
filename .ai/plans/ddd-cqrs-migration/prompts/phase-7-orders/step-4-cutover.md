# Phase 7, Step 4: Cutover

**Prerequisite**: Steps 1–3 complete. Characterization and E2E tests passing against the OLD service.

Rewrite `OrdersController` to dispatch via MediatR, then delete the old service, interface, and DTOs.

---

## Task 1: Rewrite OrdersController

Replace `src/backend/ECommerce.API/Controllers/OrdersController.cs`:

```csharp
using ECommerce.API.ActionFilters;
using ECommerce.API.Helpers;
using ECommerce.Application.DTOs.Common;
using ECommerce.Orders.Application.Commands;
using ECommerce.Orders.Application.DTOs;
using ECommerce.Orders.Application.Queries;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
[Tags("Orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidationFilter]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderRequestDto dto,
        CancellationToken cancellationToken)
    {
        var cmd = new PlaceOrderCommand(
            dto.CustomerId,
            dto.Items?.Select(i => new OrderLineItemRequest(i.ProductId, i.Quantity, i.UnitPrice)).ToList() ?? new(),
            dto.Tax,
            dto.ShippingCost);

        var result = await _mediator.Send(cmd, cancellationToken);

        if (!result.IsSuccess) return MapError(result.Error!);

        return CreatedAtAction(
            nameof(GetOrderById),
            new { id = result.Value!.Id },
            ApiResponse<OrderDetailDto>.Ok(result.Value, "Order placed successfully"));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken);
        return MapResult(result, d => Ok(ApiResponse<OrderDetailDto>.Ok(d, "Order retrieved")));
    }

    [HttpGet("/api/customers/{customerId:guid}/orders")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCustomerOrders(
        Guid customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);
        var result = await _mediator.Send(new GetCustomerOrdersQuery(customerId, page, pageSize), cancellationToken);
        return Ok(ApiResponse<PaginatedResult<OrderDto>>.Ok(result.Value!, "Orders retrieved"));
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmOrder(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ConfirmOrderCommand(id), cancellationToken);
        return MapResult(result, d => Ok(ApiResponse<OrderDetailDto>.Ok(d, "Order confirmed")));
    }

    [HttpPost("{id:guid}/ship")]
    [Authorize]
    [ValidationFilter]
    public async Task<IActionResult> ShipOrder(
        Guid id,
        [FromBody] ShipOrderRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ShipOrderCommand(id, dto.TrackingNumber), cancellationToken);
        return MapResult(result, d => Ok(ApiResponse<OrderDetailDto>.Ok(d, "Order shipped")));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequestDto? dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id, dto?.Reason), cancellationToken);
        return MapResult(result, d => Ok(ApiResponse<OrderDetailDto>.Ok(d, "Order cancelled")));
    }

    [HttpGet("admin/pending")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetPendingOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);
        var result = await _mediator.Send(new GetPendingOrdersQuery(page, pageSize), cancellationToken);
        return Ok(ApiResponse<PaginatedResult<OrderDto>>.Ok(result.Value!, "Pending orders retrieved"));
    }

    private IActionResult MapResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess) return onSuccess(result.Value!);
        return MapError(result.Error!);
    }

    private IActionResult MapError(DomainError error) => error.Code switch
    {
        "ORDER_NOT_FOUND"                => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),
        "ORDER_EMPTY"                    => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),
        "ORDER_INVALID_QUANTITY"         => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),
        "ORDER_INVALID_PRICE"            => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),
        "ORDER_CANNOT_CANCEL_SHIPPED"    => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),
        "ORDER_CANNOT_CANCEL_DELIVERED"  => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),
        "ORDER_CANNOT_SHIP_PENDING"      => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),
        "ORDER_ALREADY_CONFIRMED"        => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),
        "CONCURRENCY_CONFLICT"           => Conflict(ApiResponse<object>.Failure(error.Message, error.Code)),
        _                                => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code))
    };
}

// Request DTOs
public class PlaceOrderRequestDto
{
    public Guid CustomerId { get; set; }
    public List<OrderLineItemRequestDto>? Items { get; set; }
    public decimal Tax { get; set; }
    public decimal ShippingCost { get; set; }
}

public class OrderLineItemRequestDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class ShipOrderRequestDto
{
    public string TrackingNumber { get; set; } = null!;
}

public class CancelOrderRequestDto
{
    public string? Reason { get; set; }
}
```

---

## Task 2: Delete old files

```bash
rm src/backend/ECommerce.Application/Services/OrderService.cs
rm src/backend/ECommerce.Application/Interfaces/IOrderService.cs
find src/backend/ECommerce.Application/DTOs/Orders -name "*.cs" -delete 2>/dev/null || true
```

Remove from `Program.cs`:
```csharp
// Remove: builder.Services.AddScoped<IOrderService, OrderService>();
```

---

## Task 3: Verify

```bash
cd src/backend
dotnet build
dotnet test ECommerce.Tests --filter "FullyQualifiedName~OrdersCharacterizationTests"
dotnet test ECommerce.Tests --filter "FullyQualifiedName~OrdersControllerTests"
```

---

## Acceptance Criteria

- [ ] Controller compiles, all characterization tests pass
- [ ] `POST /api/orders` returns 201 with Location header
- [ ] Duplicate prevention and validation working
- [ ] Status transitions (Pending → Confirmed → Shipped) work
- [ ] Cannot cancel shipped orders (422)
- [ ] Old service/DTOs fully deleted
- [ ] `dotnet build` clean across solution

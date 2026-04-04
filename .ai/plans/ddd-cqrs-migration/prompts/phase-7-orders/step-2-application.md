# Phase 7, Step 2: Application Project

**Prerequisite**: Step 1 (Domain) complete and building.

Create `ECommerce.Orders.Application` — DTOs, commands, queries, and handlers.

---

## Task 1: Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Orders.Application -o ECommerce.Orders.Application
dotnet sln ECommerce.sln add ECommerce.Orders.Application/ECommerce.Orders.Application.csproj
dotnet add ECommerce.Orders.Application/ECommerce.Orders.Application.csproj reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add ECommerce.Orders.Application/ECommerce.Orders.Application.csproj reference ECommerce.Orders.Domain/ECommerce.Orders.Domain.csproj
dotnet add ECommerce.Orders.Application/ECommerce.Orders.Application.csproj package MediatR
rm ECommerce.Orders.Application/Class1.cs
```

---

## Task 2: Define DTOs

**File: `ECommerce.Orders.Application/DTOs/OrderLineItemDto.cs`**

```csharp
namespace ECommerce.Orders.Application.DTOs;

public class OrderLineItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}
```

**File: `ECommerce.Orders.Application/DTOs/OrderDto.cs`**

```csharp
namespace ECommerce.Orders.Application.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string OrderNumber { get; set; } = null!;
    public string Status { get; set; } = null!;
    public List<OrderLineItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**File: `ECommerce.Orders.Application/DTOs/OrderDetailDto.cs`**

```csharp
namespace ECommerce.Orders.Application.DTOs;

public class OrderDetailDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string OrderNumber { get; set; } = null!;
    public string Status { get; set; } = null!;
    public List<OrderLineItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

## Task 3: Define Commands

**File: `ECommerce.Orders.Application/Commands/PlaceOrderCommand.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Orders.Application.Commands;

public record PlaceOrderCommand(
    Guid CustomerId,
    List<OrderLineItemRequest> Items,
    decimal Tax,
    decimal ShippingCost) : IRequest<Result<OrderDetailDto>>, ITransactionalCommand;

public record OrderLineItemRequest(Guid ProductId, int Quantity, decimal UnitPrice);
```

**File: `ECommerce.Orders.Application/Commands/ConfirmOrderCommand.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Orders.Application.Commands;

public record ConfirmOrderCommand(Guid OrderId) : IRequest<Result<OrderDetailDto>>, ITransactionalCommand;
```

**File: `ECommerce.Orders.Application/Commands/ShipOrderCommand.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Orders.Application.Commands;

public record ShipOrderCommand(Guid OrderId, string TrackingNumber) : IRequest<Result<OrderDetailDto>>, ITransactionalCommand;
```

**File: `ECommerce.Orders.Application/Commands/CancelOrderCommand.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Orders.Application.Commands;

public record CancelOrderCommand(Guid OrderId, string? Reason) : IRequest<Result<OrderDetailDto>>, ITransactionalCommand;
```

---

## Task 4: Define Queries

**File: `ECommerce.Orders.Application/Queries/GetOrderByIdQuery.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Orders.Application.Queries;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<Result<OrderDetailDto>>;
```

**File: `ECommerce.Orders.Application/Queries/GetCustomerOrdersQuery.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Orders.Application.Queries;

public record GetCustomerOrdersQuery(
    Guid CustomerId,
    int Page,
    int PageSize) : IRequest<Result<PaginatedResult<OrderDto>>>;
```

**File: `ECommerce.Orders.Application/Queries/GetAllOrdersQuery.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Orders.Application.Queries;

public record GetAllOrdersQuery(
    int Page,
    int PageSize,
    string? Status) : IRequest<Result<PaginatedResult<OrderDto>>>;
```

**File: `ECommerce.Orders.Application/Queries/GetPendingOrdersQuery.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Orders.Application.Queries;

public record GetPendingOrdersQuery(int Page, int PageSize) : IRequest<Result<PaginatedResult<OrderDto>>>;
```

---

## Task 5: Command Handlers

**File: `ECommerce.Orders.Application/CommandHandlers/PlaceOrderCommandHandler.cs`**

```csharp
using ECommerce.Orders.Domain.Aggregates.Order;
using ECommerce.Orders.Domain.Interfaces;
using ECommerce.Orders.Domain.ValueObjects;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Orders.Application.CommandHandlers;

public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Result<OrderDetailDto>>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public PlaceOrderCommandHandler(IOrderRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderDetailDto>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        if (!request.Items.Any())
            return Result<OrderDetailDto>.Failure(OrdersErrors.OrderEmpty);

        // Generate order number (format: ORD-YYYYMMDD-NNNNNN)
        var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random().Next(100000, 999999);
        var orderNumber = $"ORD-{dateStr}-{random}";

        var orderNumberVO = OrderNumber.Create(orderNumber).Value!;

        // Create line items
        var items = new List<OrderLineItem>();
        var subtotal = 0m;

        foreach (var itemReq in request.Items)
        {
            var qty = Quantity.Create(itemReq.Quantity).Value!;
            var price = Money.Create(itemReq.UnitPrice).Value!;
            items.Add(new OrderLineItem(itemReq.ProductId, qty, price));
            subtotal += itemReq.UnitPrice * itemReq.Quantity;
        }

        var subtotalMoney = Money.Create(subtotal).Value!;
        var tax = Money.Create(request.Tax).Value!;
        var shipping = Money.Create(request.ShippingCost).Value!;

        var createResult = Order.Create(request.CustomerId, orderNumberVO, items, subtotalMoney, tax, shipping);
        if (!createResult.IsSuccess)
            return Result<OrderDetailDto>.Failure(createResult.Error!);

        var order = createResult.Value!;
        await _repository.UpsertAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<OrderDetailDto>.Ok(order.ToDetailDto());
    }
}
```

**File: `ECommerce.Orders.Application/CommandHandlers/ConfirmOrderCommandHandler.cs`**

```csharp
using ECommerce.Orders.Domain.Interfaces;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Orders.Application.CommandHandlers;

public class ConfirmOrderCommandHandler : IRequestHandler<ConfirmOrderCommand, Result<OrderDetailDto>>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmOrderCommandHandler(IOrderRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderDetailDto>> Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result<OrderDetailDto>.Failure(OrdersErrors.OrderNotFound);

        var result = order.Confirm();
        if (!result.IsSuccess)
            return Result<OrderDetailDto>.Failure(result.Error!);

        await _repository.UpsertAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<OrderDetailDto>.Ok(order.ToDetailDto());
    }
}
```

**File: `ECommerce.Orders.Application/CommandHandlers/ShipOrderCommandHandler.cs`**

```csharp
using ECommerce.Orders.Domain.Interfaces;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Orders.Application.CommandHandlers;

public class ShipOrderCommandHandler : IRequestHandler<ShipOrderCommand, Result<OrderDetailDto>>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ShipOrderCommandHandler(IOrderRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderDetailDto>> Handle(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result<OrderDetailDto>.Failure(OrdersErrors.OrderNotFound);

        var result = order.Ship(request.TrackingNumber);
        if (!result.IsSuccess)
            return Result<OrderDetailDto>.Failure(result.Error!);

        await _repository.UpsertAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<OrderDetailDto>.Ok(order.ToDetailDto());
    }
}
```

**File: `ECommerce.Orders.Application/CommandHandlers/CancelOrderCommandHandler.cs`**

```csharp
using ECommerce.Orders.Domain.Interfaces;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Orders.Application.CommandHandlers;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<OrderDetailDto>>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(IOrderRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderDetailDto>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result<OrderDetailDto>.Failure(OrdersErrors.OrderNotFound);

        var result = order.Cancel(request.Reason);
        if (!result.IsSuccess)
            return Result<OrderDetailDto>.Failure(result.Error!);

        await _repository.UpsertAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<OrderDetailDto>.Ok(order.ToDetailDto());
    }
}
```

---

## Task 6: Query Handlers

**File: `ECommerce.Orders.Application/QueryHandlers/GetOrderByIdQueryHandler.cs`**

```csharp
using ECommerce.Orders.Domain.Interfaces;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Orders.Application.QueryHandlers;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDetailDto>>
{
    private readonly IOrderRepository _repository;

    public GetOrderByIdQueryHandler(IOrderRepository repository) => _repository = repository;

    public async Task<Result<OrderDetailDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result<OrderDetailDto>.Failure(OrdersErrors.OrderNotFound);

        return Result<OrderDetailDto>.Ok(order.ToDetailDto());
    }
}
```

**File: `ECommerce.Orders.Application/QueryHandlers/GetCustomerOrdersQueryHandler.cs`**

```csharp
using ECommerce.Orders.Domain.Interfaces;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Orders.Application.QueryHandlers;

public class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQuery, Result<PaginatedResult<OrderDto>>>
{
    private readonly IOrderRepository _repository;

    public GetCustomerOrdersQueryHandler(IOrderRepository repository) => _repository = repository;

    public async Task<Result<PaginatedResult<OrderDto>>> Handle(GetCustomerOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetByCustomerAsync(
            request.CustomerId, request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(o => o.ToDto()).ToList();
        return Result<PaginatedResult<OrderDto>>.Ok(
            new PaginatedResult<OrderDto>(dtos, request.Page, request.PageSize, total));
    }
}
```

**File: `ECommerce.Orders.Application/QueryHandlers/GetAllOrdersQueryHandler.cs`**

```csharp
using ECommerce.Orders.Domain.Interfaces;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Orders.Application.QueryHandlers;

public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, Result<PaginatedResult<OrderDto>>>
{
    private readonly IOrderRepository _repository;

    public GetAllOrdersQueryHandler(IOrderRepository repository) => _repository = repository;

    public async Task<Result<PaginatedResult<OrderDto>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetAllAsync(
            request.Page, request.PageSize, request.Status, cancellationToken);

        var dtos = items.Select(o => o.ToDto()).ToList();
        return Result<PaginatedResult<OrderDto>>.Ok(
            new PaginatedResult<OrderDto>(dtos, request.Page, request.PageSize, total));
    }
}
```

**File: `ECommerce.Orders.Application/QueryHandlers/GetPendingOrdersQueryHandler.cs`**

```csharp
using ECommerce.Orders.Domain.Interfaces;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Orders.Application.QueryHandlers;

public class GetPendingOrdersQueryHandler : IRequestHandler<GetPendingOrdersQuery, Result<PaginatedResult<OrderDto>>>
{
    private readonly IOrderRepository _repository;

    public GetPendingOrdersQueryHandler(IOrderRepository repository) => _repository = repository;

    public async Task<Result<PaginatedResult<OrderDto>>> Handle(GetPendingOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetPendingAsync(request.Page, request.PageSize, cancellationToken);
        var dtos = items.Select(o => o.ToDto()).ToList();
        return Result<PaginatedResult<OrderDto>>.Ok(
            new PaginatedResult<OrderDto>(dtos, request.Page, request.PageSize, total));
    }
}
```

---

## Task 7: Mapping Extensions

**File: `ECommerce.Orders.Application/Mappings/OrdersMappingExtensions.cs`**

```csharp
using ECommerce.Orders.Domain.Aggregates.Order;

namespace ECommerce.Orders.Application.Mappings;

public static class OrdersMappingExtensions
{
    public static OrderDto ToDto(this Order order) => new()
    {
        Id = order.Id,
        CustomerId = order.CustomerId,
        OrderNumber = order.OrderNumber.Value,
        Status = order.Status.ToString(),
        Items = order.Items.Select(i => new OrderLineItemDto
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity.Value,
            UnitPrice = i.UnitPrice.Amount
        }).ToList(),
        Subtotal = order.Subtotal.Amount,
        Tax = order.Tax.Amount,
        ShippingCost = order.ShippingCost.Amount,
        Total = order.Total.Amount,
        CreatedAt = order.CreatedAt
    };

    public static OrderDetailDto ToDetailDto(this Order order) => new()
    {
        Id = order.Id,
        CustomerId = order.CustomerId,
        OrderNumber = order.OrderNumber.Value,
        Status = order.Status.ToString(),
        Items = order.Items.Select(i => new OrderLineItemDto
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity.Value,
            UnitPrice = i.UnitPrice.Amount
        }).ToList(),
        Subtotal = order.Subtotal.Amount,
        Tax = order.Tax.Amount,
        ShippingCost = order.ShippingCost.Amount,
        Total = order.Total.Amount,
        TrackingNumber = order.TrackingNumber,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt
    };
}
```

---

## Task 8: DependencyInjection

**File: `ECommerce.Orders.Application/DependencyInjection.cs`**

```csharp
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Orders.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOrdersApplication(this IServiceCollection services)
    {
        services.AddMediatR(typeof(DependencyInjection));
        return services;
    }
}
```

---

## Acceptance Criteria

- [ ] Project builds with zero errors
- [ ] DTOs defined: `OrderDto`, `OrderDetailDto`, `OrderLineItemDto`
- [ ] 4 commands defined: PlaceOrder, ConfirmOrder, ShipOrder, CancelOrder
- [ ] 4 queries defined: GetById, GetCustomerOrders, GetAllOrders, GetPending
- [ ] All command handlers implement `IRequestHandler<TCommand, Result<OrderDetailDto>>`
- [ ] All query handlers implement `IRequestHandler<TQuery, Result<PaginatedResult<OrderDto>>>`
- [ ] `ToDto()` and `ToDetailDto()` mapping extensions work correctly
- [ ] All commands implement `ITransactionalCommand`
- [ ] DependencyInjection registers MediatR handlers

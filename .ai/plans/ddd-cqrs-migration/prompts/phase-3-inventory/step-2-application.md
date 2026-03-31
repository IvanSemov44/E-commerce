# Phase 3, Step 2: Inventory Application Project

**Prerequisite**: Step 1 (`ECommerce.Inventory.Domain`) is complete and `dotnet build` passes.

---

## Task: Create ECommerce.Inventory.Application Project

### 1. Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Inventory.Application -f net10.0 -o Inventory/ECommerce.Inventory.Application
dotnet sln ../../ECommerce.sln add Inventory/ECommerce.Inventory.Application/ECommerce.Inventory.Application.csproj

dotnet add Inventory/ECommerce.Inventory.Application/ECommerce.Inventory.Application.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add Inventory/ECommerce.Inventory.Application/ECommerce.Inventory.Application.csproj \
    reference Inventory/ECommerce.Inventory.Domain/ECommerce.Inventory.Domain.csproj

dotnet add Inventory/ECommerce.Inventory.Application/ECommerce.Inventory.Application.csproj package MediatR
dotnet add Inventory/ECommerce.Inventory.Application/ECommerce.Inventory.Application.csproj package FluentValidation

rm Inventory/ECommerce.Inventory.Application/Class1.cs
```

### 2. Create application errors

**File: `Inventory/ECommerce.Inventory.Application/Errors/InventoryApplicationErrors.cs`**

```csharp
using ECommerce.SharedKernel.Results;

namespace ECommerce.Inventory.Application.Errors;

public static class InventoryApplicationErrors
{
    // Raised by handlers after a repo lookup — not by the aggregate itself
    public static readonly DomainError InventoryItemNotFound = new("INVENTORY_ITEM_NOT_FOUND", "Inventory item not found for this product.");
}
```

> **Rule**: `InventoryErrors` (domain) = errors the aggregate raises by itself (InsufficientStock, ThresholdNegative, etc.).
> `InventoryApplicationErrors` (application) = errors that require a repository check (InventoryItemNotFound).

---

### 3. Create application-layer interface

**File: `Inventory/ECommerce.Inventory.Application/Interfaces/IEmailService.cs`**

```csharp
namespace ECommerce.Inventory.Application.Interfaces;

public interface IEmailService
{
    Task SendLowStockAlertAsync(Guid productId, int currentStock, int threshold, CancellationToken ct);
}
```

Implementation lives in `Inventory.Infrastructure/Services/EmailService.cs`.

---

### 4. Create DTOs

**File: `Inventory/ECommerce.Inventory.Application/DTOs/InventoryItemDto.cs`**

```csharp
namespace ECommerce.Inventory.Application.DTOs;

public record InventoryItemDto(
    Guid   Id,
    Guid   ProductId,
    int    Quantity,
    int    LowStockThreshold,
    bool   IsLowStock,
    bool   IsOutOfStock
);
```

**File: `Inventory/ECommerce.Inventory.Application/DTOs/InventoryLogEntryDto.cs`**

```csharp
namespace ECommerce.Inventory.Application.DTOs;

public record InventoryLogEntryDto(
    int      Delta,
    string   Reason,
    int      StockAfter,
    DateTime OccurredAt
);
```

**File: `Inventory/ECommerce.Inventory.Application/DTOs/StockAdjustmentResultDto.cs`**

```csharp
namespace ECommerce.Inventory.Application.DTOs;

public record StockAdjustmentResultDto(
    Guid     ProductId,
    int      NewQuantity,
    int      QuantityChanged,
    DateTime AdjustedAt
);
```

---

### 5. Create queries

For each query: a folder with `Query.cs` and `QueryHandler.cs`.

---

**`Queries/GetInventory/GetInventoryQuery.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Application.DTOs;

namespace ECommerce.Inventory.Application.Queries.GetInventory;

public record GetInventoryQuery(
    int     Page         = 1,
    int     PageSize     = 20,
    string? Search       = null,
    bool    LowStockOnly = false
) : IRequest<Result<List<InventoryItemDto>>>;
```

**`Queries/GetInventory/GetInventoryQueryHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Application.DTOs;
using ECommerce.Inventory.Domain.Interfaces;

namespace ECommerce.Inventory.Application.Queries.GetInventory;

public class GetInventoryQueryHandler(IInventoryItemRepository _repo)
    : IRequestHandler<GetInventoryQuery, Result<List<InventoryItemDto>>>
{
    public async Task<Result<List<InventoryItemDto>>> Handle(
        GetInventoryQuery query, CancellationToken cancellationToken)
    {
        var items = query.LowStockOnly
            ? await _repo.GetLowStockAsync(cancellationToken: cancellationToken)
            : await _repo.GetAllAsync(cancellationToken);

        var dtos = items
            .Select(i => new InventoryItemDto(
                i.Id, i.ProductId, i.Stock.Quantity, i.LowStockThreshold,
                i.Stock.Quantity <= i.LowStockThreshold,
                i.Stock.Quantity <= 0))
            .ToList();

        return Result<List<InventoryItemDto>>.Ok(dtos);
    }
}
```

---

**`Queries/GetInventoryByProductId/GetInventoryByProductIdQuery.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Application.DTOs;

namespace ECommerce.Inventory.Application.Queries.GetInventoryByProductId;

public record GetInventoryByProductIdQuery(Guid ProductId)
    : IRequest<Result<InventoryItemDto>>;
```

**`Queries/GetInventoryByProductId/GetInventoryByProductIdQueryHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Application.DTOs;
using ECommerce.Inventory.Application.Errors;
using ECommerce.Inventory.Domain.Interfaces;

namespace ECommerce.Inventory.Application.Queries.GetInventoryByProductId;

public class GetInventoryByProductIdQueryHandler(IInventoryItemRepository _repo)
    : IRequestHandler<GetInventoryByProductIdQuery, Result<InventoryItemDto>>
{
    public async Task<Result<InventoryItemDto>> Handle(
        GetInventoryByProductIdQuery query, CancellationToken cancellationToken)
    {
        var item = await _repo.GetByProductIdAsync(query.ProductId, cancellationToken);
        if (item is null)
            return Result<InventoryItemDto>.Fail(InventoryApplicationErrors.InventoryItemNotFound);

        return Result<InventoryItemDto>.Ok(new InventoryItemDto(
            item.Id, item.ProductId, item.Stock.Quantity, item.LowStockThreshold,
            item.Stock.Quantity <= item.LowStockThreshold,
            item.Stock.Quantity <= 0));
    }
}
```

---

**`Queries/GetLowStockItems/GetLowStockItemsQuery.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Application.DTOs;

namespace ECommerce.Inventory.Application.Queries.GetLowStockItems;

public record GetLowStockItemsQuery(int? ThresholdOverride = null)
    : IRequest<Result<List<InventoryItemDto>>>;
```

**`Queries/GetLowStockItems/GetLowStockItemsQueryHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Application.DTOs;
using ECommerce.Inventory.Domain.Interfaces;

namespace ECommerce.Inventory.Application.Queries.GetLowStockItems;

public class GetLowStockItemsQueryHandler(IInventoryItemRepository _repo)
    : IRequestHandler<GetLowStockItemsQuery, Result<List<InventoryItemDto>>>
{
    public async Task<Result<List<InventoryItemDto>>> Handle(
        GetLowStockItemsQuery query, CancellationToken cancellationToken)
    {
        var items = await _repo.GetLowStockAsync(query.ThresholdOverride, cancellationToken);

        return Result<List<InventoryItemDto>>.Ok(items.Select(i => new InventoryItemDto(
            i.Id, i.ProductId, i.Stock.Quantity, i.LowStockThreshold,
            i.Stock.Quantity <= i.LowStockThreshold,
            i.Stock.Quantity <= 0)).ToList());
    }
}
```

---

### 6. Create commands

For each command: a folder with `Command.cs`, `CommandHandler.cs`, `CommandValidator.cs`.

---

**`Commands/IncreaseStock/IncreaseStockCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Inventory.Application.DTOs;

namespace ECommerce.Inventory.Application.Commands.IncreaseStock;

public record IncreaseStockCommand(
    Guid   ProductId,
    int    Amount,
    string Reason
) : IRequest<Result<StockAdjustmentResultDto>>, ITransactionalCommand;
```

**`Commands/IncreaseStock/IncreaseStockCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Inventory.Application.Commands.IncreaseStock;

public class IncreaseStockCommandValidator : AbstractValidator<IncreaseStockCommand>
{
    public IncreaseStockCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Reason is required.");
    }
}
```

**`Commands/IncreaseStock/IncreaseStockCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Inventory.Application.DTOs;
using ECommerce.Inventory.Application.Errors;
using ECommerce.Inventory.Domain.Interfaces;

namespace ECommerce.Inventory.Application.Commands.IncreaseStock;

public class IncreaseStockCommandHandler(
    IInventoryItemRepository _repo,
    IUnitOfWork _uow
) : IRequestHandler<IncreaseStockCommand, Result<StockAdjustmentResultDto>>
{
    public async Task<Result<StockAdjustmentResultDto>> Handle(
        IncreaseStockCommand command, CancellationToken cancellationToken)
    {
        var item = await _repo.GetByProductIdAsync(command.ProductId, cancellationToken);
        if (item is null)
            return Result<StockAdjustmentResultDto>.Fail(InventoryApplicationErrors.InventoryItemNotFound);

        var result = item.Increase(command.Amount, command.Reason);
        if (!result.IsSuccess) return Result<StockAdjustmentResultDto>.Fail(result.GetErrorOrThrow());

        await _uow.SaveChangesAsync(cancellationToken);

        return Result<StockAdjustmentResultDto>.Ok(new StockAdjustmentResultDto(
            command.ProductId, item.Stock.Quantity, command.Amount, DateTime.UtcNow));
    }
}
```

---

**`Commands/ReduceStock/ReduceStockCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Inventory.Application.DTOs;

namespace ECommerce.Inventory.Application.Commands.ReduceStock;

public record ReduceStockCommand(
    Guid   ProductId,
    int    Amount,
    string Reason
) : IRequest<Result<StockAdjustmentResultDto>>, ITransactionalCommand;
```

**`Commands/ReduceStock/ReduceStockCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Inventory.Application.Commands.ReduceStock;

public class ReduceStockCommandValidator : AbstractValidator<ReduceStockCommand>
{
    public ReduceStockCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Reason is required.");
    }
}
```

**`Commands/ReduceStock/ReduceStockCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Inventory.Application.DTOs;
using ECommerce.Inventory.Application.Errors;
using ECommerce.Inventory.Domain.Interfaces;

namespace ECommerce.Inventory.Application.Commands.ReduceStock;

public class ReduceStockCommandHandler(
    IInventoryItemRepository _repo,
    IUnitOfWork _uow
) : IRequestHandler<ReduceStockCommand, Result<StockAdjustmentResultDto>>
{
    public async Task<Result<StockAdjustmentResultDto>> Handle(
        ReduceStockCommand command, CancellationToken cancellationToken)
    {
        var item = await _repo.GetByProductIdAsync(command.ProductId, cancellationToken);
        if (item is null)
            return Result<StockAdjustmentResultDto>.Fail(InventoryApplicationErrors.InventoryItemNotFound);

        var result = item.Reduce(command.Amount, command.Reason);
        if (!result.IsSuccess) return Result<StockAdjustmentResultDto>.Fail(result.GetErrorOrThrow());

        await _uow.SaveChangesAsync(cancellationToken);

        return Result<StockAdjustmentResultDto>.Ok(new StockAdjustmentResultDto(
            command.ProductId, item.Stock.Quantity, -command.Amount, DateTime.UtcNow));
    }
}
```

---

**`Commands/AdjustStock/AdjustStockCommand.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Inventory.Application.DTOs;

namespace ECommerce.Inventory.Application.Commands.AdjustStock;

public record AdjustStockCommand(
    Guid   ProductId,
    int    NewQuantity,
    string Reason
) : IRequest<Result<StockAdjustmentResultDto>>, ITransactionalCommand;
```

**`Commands/AdjustStock/AdjustStockCommandValidator.cs`**
```csharp
using FluentValidation;

namespace ECommerce.Inventory.Application.Commands.AdjustStock;

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.NewQuantity).GreaterThanOrEqualTo(0).WithMessage("Quantity cannot be negative.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Reason is required.");
    }
}
```

**`Commands/AdjustStock/AdjustStockCommandHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Inventory.Application.DTOs;
using ECommerce.Inventory.Application.Errors;
using ECommerce.Inventory.Domain.Interfaces;

namespace ECommerce.Inventory.Application.Commands.AdjustStock;

public class AdjustStockCommandHandler(
    IInventoryItemRepository _repo,
    IUnitOfWork _uow
) : IRequestHandler<AdjustStockCommand, Result<StockAdjustmentResultDto>>
{
    public async Task<Result<StockAdjustmentResultDto>> Handle(
        AdjustStockCommand command, CancellationToken cancellationToken)
    {
        var item = await _repo.GetByProductIdAsync(command.ProductId, cancellationToken);
        if (item is null)
            return Result<StockAdjustmentResultDto>.Fail(InventoryApplicationErrors.InventoryItemNotFound);

        var previousQty = item.Stock.Quantity;
        var result = item.Adjust(command.NewQuantity, command.Reason);
        if (!result.IsSuccess) return Result<StockAdjustmentResultDto>.Fail(result.GetErrorOrThrow());

        await _uow.SaveChangesAsync(cancellationToken);

        return Result<StockAdjustmentResultDto>.Ok(new StockAdjustmentResultDto(
            command.ProductId,
            item.Stock.Quantity,
            command.NewQuantity - previousQty,
            DateTime.UtcNow));
    }
}
```

---

**`Queries/GetInventoryHistory/GetInventoryHistoryQuery.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Application.DTOs;

namespace ECommerce.Inventory.Application.Queries.GetInventoryHistory;

public record GetInventoryHistoryQuery(
    Guid ProductId,
    int  Page     = 1,
    int  PageSize = 50
) : IRequest<Result<List<InventoryLogEntryDto>>>;
```

**`Queries/GetInventoryHistory/GetInventoryHistoryQueryHandler.cs`**
```csharp
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Application.DTOs;
using ECommerce.Inventory.Application.Errors;
using ECommerce.Inventory.Domain.Interfaces;

namespace ECommerce.Inventory.Application.Queries.GetInventoryHistory;

public class GetInventoryHistoryQueryHandler(IInventoryItemRepository _repo)
    : IRequestHandler<GetInventoryHistoryQuery, Result<List<InventoryLogEntryDto>>>
{
    public async Task<Result<List<InventoryLogEntryDto>>> Handle(
        GetInventoryHistoryQuery query, CancellationToken cancellationToken)
    {
        var item = await _repo.GetByProductIdAsync(query.ProductId, cancellationToken);
        if (item is null)
            return Result<List<InventoryLogEntryDto>>.Fail(InventoryApplicationErrors.InventoryItemNotFound);

        var entries = item.Log
            .OrderByDescending(l => l.OccurredAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new InventoryLogEntryDto(l.Delta, l.Reason, l.StockAfter, l.OccurredAt))
            .ToList();

        return Result<List<InventoryLogEntryDto>>.Ok(entries);
    }
}
```

---

### 7. Create event handlers

Event handlers live in `EventHandlers/`. Each handler does ONE thing, catches exceptions, never rethrows (Rule 17).

**`EventHandlers/SendLowStockAlertOnLowStockDetectedHandler.cs`**
```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using ECommerce.Inventory.Application.Interfaces;
using ECommerce.Inventory.Domain.Events;

namespace ECommerce.Inventory.Application.EventHandlers;

public class SendLowStockAlertOnLowStockDetectedHandler(
    IEmailService _email,
    ILogger<SendLowStockAlertOnLowStockDetectedHandler> _logger
) : INotificationHandler<LowStockDetectedEvent>
{
    public async Task Handle(LowStockDetectedEvent notification, CancellationToken ct)
    {
        try
        {
            await _email.SendLowStockAlertAsync(
                notification.ProductId,
                notification.CurrentStock,
                notification.Threshold,
                ct);
        }
        catch (Exception ex)
        {
            // Rule 17: handlers don't throw to callers — log and move on
            _logger.LogError(ex,
                "Failed to send low stock alert for ProductId {ProductId}",
                notification.ProductId);
        }
    }
}
```

**`EventHandlers/ReduceStockOnOrderPlacedHandler.cs`** (stub — implement in Phase 7)

```csharp
// NOTE: OrderPlacedEvent will come from the Ordering context (Phase 7).
// This file documents the contract so the Phase 7 implementor knows where to put it.
//
// When Phase 7 arrives:
//   1. Add OrderPlacedEvent to ECommerce.Ordering.Domain
//   2. Reference Ordering.Domain from Inventory.Application (or use a shared contracts project)
//   3. Uncomment and implement this handler:
//
// public class ReduceStockOnOrderPlacedHandler(
//     IInventoryItemRepository _repo,
//     IUnitOfWork _uow,
//     ILogger<ReduceStockOnOrderPlacedHandler> _logger
// ) : INotificationHandler<OrderPlacedEvent>
// {
//     public async Task Handle(OrderPlacedEvent notification, CancellationToken ct)
//     {
//         foreach (var orderItem in notification.Items)
//         {
//             var inventoryItem = await _repo.GetByProductIdAsync(orderItem.ProductId, ct);
//             if (inventoryItem is null)
//             {
//                 _logger.LogWarning("No inventory item for ProductId {ProductId}", orderItem.ProductId);
//                 continue;
//             }
//             var result = inventoryItem.Reduce(orderItem.Quantity, $"order:{notification.OrderId}");
//             if (!result.IsSuccess)
//                 _logger.LogError("Failed to reduce stock for ProductId {ProductId}: {Error}",
//                     orderItem.ProductId, result.GetErrorOrThrow().Message);
//         }
//         await _uow.SaveChangesAsync(ct);
//     }
// }

namespace ECommerce.Inventory.Application.EventHandlers;
// Placeholder namespace — implement in Phase 7
```

---

### 8. Verify

```bash
cd src/backend
dotnet build Inventory/ECommerce.Inventory.Application/ECommerce.Inventory.Application.csproj
dotnet build  # Entire solution still builds
```

---

## Tester Handoff

Once this step is delivered, the tester writes handler unit tests in `ECommerce.Inventory.Tests/Application/`. See `step-6-handler-tests.md`.

---

## Acceptance Criteria

- [ ] `ECommerce.Inventory.Application` project created and added to solution
- [ ] Dependencies: `SharedKernel`, `Inventory.Domain`, `MediatR`, `FluentValidation`
- [ ] `InventoryApplicationErrors.InventoryItemNotFound` defined (separate from domain errors)
- [ ] `IEmailService` interface defined in Application project
- [ ] 4 queries with handlers: `GetInventoryQuery`, `GetInventoryByProductIdQuery`, `GetLowStockItemsQuery`, `GetInventoryHistoryQuery`
- [ ] 3 commands with handlers and validators: `IncreaseStockCommand`, `ReduceStockCommand`, `AdjustStockCommand`
- [ ] All commands implement `ITransactionalCommand` and inject `IUnitOfWork`
- [ ] `SendLowStockAlertOnLowStockDetectedHandler` implemented — catches ALL exceptions, never rethrows
- [ ] `ReduceStockOnOrderPlacedHandler` stub with documented Phase 7 contract
- [ ] `dotnet build` passes for Application project and entire solution

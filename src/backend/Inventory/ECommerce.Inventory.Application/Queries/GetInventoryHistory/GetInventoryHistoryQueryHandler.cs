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
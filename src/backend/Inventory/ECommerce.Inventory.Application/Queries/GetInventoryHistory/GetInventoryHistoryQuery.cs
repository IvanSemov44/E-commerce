using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Application.DTOs;

namespace ECommerce.Inventory.Application.Queries.GetInventoryHistory;

public record GetInventoryHistoryQuery(
    Guid ProductId,
    int Page = 1,
    int PageSize = 50
) : IRequest<Result<List<InventoryLogEntryDto>>>;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Application.DTOs;

namespace ECommerce.Inventory.Application.Queries.GetInventory;

public record GetInventoryQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool LowStockOnly = false
) : IRequest<Result<List<InventoryItemDto>>>;
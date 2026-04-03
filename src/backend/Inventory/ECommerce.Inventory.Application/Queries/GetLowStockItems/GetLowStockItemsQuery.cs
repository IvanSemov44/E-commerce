using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Application.DTOs;

namespace ECommerce.Inventory.Application.Queries.GetLowStockItems;

public record GetLowStockItemsQuery(int? ThresholdOverride = null)
    : IRequest<Result<List<InventoryItemDto>>>;
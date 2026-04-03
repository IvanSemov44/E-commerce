using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Application.DTOs;

namespace ECommerce.Inventory.Application.Queries.GetInventoryByProductId;

public record GetInventoryByProductIdQuery(Guid ProductId)
    : IRequest<Result<InventoryItemDto>>;
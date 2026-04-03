using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Inventory.Application.DTOs;

namespace ECommerce.Inventory.Application.Commands.ReduceStock;

public record ReduceStockCommand(
    Guid ProductId,
    int Amount,
    string Reason
) : IRequest<Result<StockAdjustmentResultDto>>, ITransactionalCommand;
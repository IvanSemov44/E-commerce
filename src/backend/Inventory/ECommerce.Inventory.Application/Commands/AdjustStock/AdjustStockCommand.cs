using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Inventory.Application.DTOs;

namespace ECommerce.Inventory.Application.Commands.AdjustStock;

public record AdjustStockCommand(
    Guid ProductId,
    int NewQuantity,
    string Reason
) : IRequest<Result<StockAdjustmentResultDto>>, ITransactionalCommand;
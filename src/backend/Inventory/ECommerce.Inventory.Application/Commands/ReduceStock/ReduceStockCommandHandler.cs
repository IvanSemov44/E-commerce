using MediatR;
using Microsoft.Extensions.Logging;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Inventory.Application.DTOs;
using ECommerce.Inventory.Application.Errors;
using ECommerce.Inventory.Application.Interfaces;
using ECommerce.Inventory.Domain.Interfaces;

namespace ECommerce.Inventory.Application.Commands.ReduceStock;

public class ReduceStockCommandHandler(
    IInventoryItemRepository _repo,
    IUnitOfWork _uow,
    IInventoryProjectionEventPublisher _projectionPublisher,
    ILogger<ReduceStockCommandHandler> _logger
) : IRequestHandler<ReduceStockCommand, Result<StockAdjustmentResultDto>>
{
    public async Task<Result<StockAdjustmentResultDto>> Handle(
        ReduceStockCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reducing stock for product {ProductId} by {Amount}",
            command.ProductId, command.Amount);

        var item = await _repo.GetByProductIdAsync(command.ProductId, cancellationToken);
        if (item is null)
        {
            _logger.LogWarning("Product {ProductId} not found", command.ProductId);
            return Result<StockAdjustmentResultDto>.Fail(InventoryApplicationErrors.InventoryItemNotFound);
        }

        var previousQty = item.Stock.Quantity;
        var result = item.Reduce(command.Amount, command.Reason);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to reduce stock for product {ProductId}: {Error}",
                command.ProductId, result.GetErrorOrThrow().Message);
            return Result<StockAdjustmentResultDto>.Fail(result.GetErrorOrThrow());
        }

        await _uow.SaveChangesAsync(cancellationToken);
        await _projectionPublisher.PublishStockProjectionUpdatedAsync(
            command.ProductId,
            item.Stock.Quantity,
            command.Reason,
            cancellationToken);

        _logger.LogInformation("Stock reduced for product {ProductId}: {PreviousQty} -> {NewQty}",
            command.ProductId, previousQty, item.Stock.Quantity);

        return Result<StockAdjustmentResultDto>.Ok(new StockAdjustmentResultDto(
            command.ProductId, item.Stock.Quantity, -command.Amount, DateTime.UtcNow));
    }
}
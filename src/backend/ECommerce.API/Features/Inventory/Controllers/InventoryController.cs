using ECommerce.API.ActionFilters;
using ECommerce.API.Common.Extensions;
using ECommerce.API.Common.Helpers;

using ECommerce.Contracts.DTOs.Common;
using ECommerce.SharedKernel.Pagination;
using ECommerce.Inventory.Application.DTOs;
using ECommerce.Inventory.Application.Commands.AdjustStock;
using ECommerce.Inventory.Application.Commands.BulkAdjustStock;
using ECommerce.Inventory.Application.Commands.IncreaseStock;
using ECommerce.Inventory.Application.Queries.CheckBulkStockAvailability;
using ECommerce.Inventory.Application.Queries.GetInventory;
using ECommerce.Inventory.Application.Queries.GetInventoryByProductId;
using ECommerce.Inventory.Application.Queries.GetInventoryHistory;
using ECommerce.Inventory.Application.Queries.GetLowStockItems;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Features.Inventory.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Inventory")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class InventoryController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    private IActionResult MapError(DomainError error) => error.Code switch
    {
        "INVENTORY_ITEM_NOT_FOUND"
            => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),
        "INSUFFICIENT_STOCK" or "STOCK_NEGATIVE"
        or "REDUCE_AMOUNT_INVALID" or "INCREASE_AMOUNT_INVALID"
        or "THRESHOLD_NEGATIVE"
            => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),
        "VALIDATION_FAILED"
            => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),
        _ => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code))
    };

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<InventoryItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllInventory(
        [FromQuery] InventoryQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        (var page, var pageSize) = PaginationRequestNormalizer.Normalize(parameters.Page, parameters.PageSize);

        var result = await _mediator.Send(
            new GetInventoryQuery(page, pageSize, parameters.Search, parameters.LowStockOnly ?? false),
            cancellationToken);

        return result.ToActionResult(
            data => Ok(ApiResponse<PaginatedResult<InventoryItemDto>>.Ok(data, "Inventory retrieved successfully")),
            MapError);
    }

    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<InventoryItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLowStockItems(
        [FromQuery] int? threshold = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);

        var result = await _mediator.Send(new GetLowStockItemsQuery(threshold, page, pageSize), cancellationToken);
        return result.ToActionResult(
            data => Ok(ApiResponse<PaginatedResult<InventoryItemDto>>.Ok(data, "Low stock items retrieved successfully")),
            MapError);
    }

    [HttpGet("{productId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductStock(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInventoryByProductIdQuery(productId), cancellationToken);
        return result.ToActionResult(
            data => Ok(ApiResponse<InventoryItemDto>.Ok(data, "Product stock retrieved successfully")),
            MapError);
    }

    [HttpGet("{productId:guid}/available")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<StockAvailabilityResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckAvailableQuantity(Guid productId, [FromQuery] int quantity, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInventoryByProductIdQuery(productId), cancellationToken);
        return result.ToActionResult(
            item => Ok(ApiResponse<StockAvailabilityResultDto>.Ok(
                new StockAvailabilityResultDto(productId, quantity, item.Quantity >= quantity),
                "Availability check completed")),
            MapError);
    }

    [HttpGet("{productId:guid}/history")]
    [ProducesResponseType(typeof(ApiResponse<List<InventoryLogEntryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInventoryHistory(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetInventoryHistoryQuery(productId, page, pageSize), cancellationToken);
        return result.ToActionResult(
            data => Ok(ApiResponse<List<InventoryLogEntryDto>>.Ok(data, "Inventory history retrieved successfully")),
            MapError);
    }

    [HttpPost("{productId:guid}/adjust")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<StockAdjustmentResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AdjustStock(Guid productId, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AdjustStockCommand(productId, request.Quantity, request.Reason ?? "adjustment"), cancellationToken);

        return result.ToActionResult(
            data => Ok(ApiResponse<StockAdjustmentResultDto>.Ok(data, "Stock adjusted successfully")),
            MapError);
    }

    [HttpPost("{productId:guid}/restock")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<StockAdjustmentResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RestockProduct(Guid productId, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new IncreaseStockCommand(productId, request.Quantity, request.Reason ?? "restock"), cancellationToken);

        return result.ToActionResult(
            data => Ok(ApiResponse<StockAdjustmentResultDto>.Ok(data, $"Stock increased by {request.Quantity} units")),
            MapError);
    }

    [HttpPost("check-availability")]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<BulkStockAvailabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckStockAvailability([FromBody] StockCheckRequest request, CancellationToken cancellationToken)
    {
        var items = request.Items.Select(i => new StockCheckItem(i.ProductId, i.Quantity)).ToList();
        var result = await _mediator.Send(new CheckBulkStockAvailabilityQuery(items), cancellationToken);

        return result.ToActionResult(
            data => Ok(ApiResponse<BulkStockAvailabilityDto>.Ok(
                data,
                data.IsAvailable ? "All items are available" : "Some items have stock issues")),
            MapError);
    }

    [HttpPut("{productId:guid}")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<StockAdjustmentResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateProductStock(Guid productId, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AdjustStockCommand(productId, request.Quantity, request.Reason ?? "stock_update"), cancellationToken);

        return result.ToActionResult(
            data => Ok(ApiResponse<StockAdjustmentResultDto>.Ok(data, "Product stock updated successfully")),
            MapError);
    }

    [HttpPut("bulk-update")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<List<StockAdjustmentResultDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> BulkUpdateStock([FromBody] BulkStockUpdateRequest request, CancellationToken cancellationToken)
    {
        var updates = request.Updates.Select(u => new BulkAdjustStockItem(u.ProductId, u.Quantity)).ToList();
        var result = await _mediator.Send(new BulkAdjustStockCommand(updates), cancellationToken);

        return result.ToActionResult(
            data => Ok(ApiResponse<List<StockAdjustmentResultDto>>.Ok(data, "Stock updated successfully")),
            MapError);
    }
}

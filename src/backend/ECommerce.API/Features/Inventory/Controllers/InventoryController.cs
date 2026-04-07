using ECommerce.API.ActionFilters;
using ECommerce.API.Helpers;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Inventory;
using ECommerce.Inventory.Application.Commands.IncreaseStock;
using ECommerce.Inventory.Application.Commands.ReduceStock;
using ECommerce.Inventory.Application.Commands.AdjustStock;
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

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllInventory(
        [FromQuery] InventoryQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetInventoryQuery(parameters.Page, parameters.PageSize, parameters.Search, parameters.LowStockOnly ?? false), cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<List<object>>.Ok(result.GetDataOrThrow().Cast<object>().ToList(), "Inventory retrieved successfully"))
            : MapInventoryResult(result.GetErrorOrThrow());
    }

    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(ApiResponse<List<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLowStockProducts(
        [FromQuery] int? threshold = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetLowStockItemsQuery(threshold), cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<List<object>>.Ok(result.GetDataOrThrow().Cast<object>().ToList(), "Low stock products retrieved successfully"))
            : MapInventoryResult(result.GetErrorOrThrow());
    }

    [HttpGet("{productId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProductStock(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInventoryByProductIdQuery(productId), cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Product stock retrieved successfully"))
            : MapInventoryResult(result.GetErrorOrThrow());
    }

    [HttpGet("{productId:guid}/available")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<dynamic>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckAvailableQuantity(Guid productId, [FromQuery] int quantity, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInventoryByProductIdQuery(productId), cancellationToken);
        if (!result.IsSuccess) return MapInventoryResult(result.GetErrorOrThrow());

        var item = result.GetDataOrThrow();
        var isAvailable = item.Quantity >= quantity;
        return Ok(ApiResponse<object>.Ok(
            new { ProductId = productId, RequestedQuantity = quantity, IsAvailable = isAvailable },
            "Availability check completed"));
    }

    [HttpGet("{productId}/history")]
    [ProducesResponseType(typeof(ApiResponse<List<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInventoryHistory(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetInventoryHistoryQuery(productId, page, pageSize), cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<List<object>>.Ok(result.GetDataOrThrow().Cast<object>().ToList(), "Inventory history retrieved successfully"))
            : MapInventoryResult(result.GetErrorOrThrow());
    }

    [HttpPost("{productId}/adjust")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AdjustStock(Guid productId, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AdjustStockCommand(productId, request.Quantity, request.Reason ?? "adjustment"), cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Stock adjusted successfully"))
            : MapInventoryResult(result.GetErrorOrThrow());
    }

    [HttpPost("{productId}/restock")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RestockProduct(Guid productId, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new IncreaseStockCommand(productId, request.Quantity, request.Reason ?? "restock"), cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), $"Stock increased by {request.Quantity} units"))
            : MapInventoryResult(result.GetErrorOrThrow());
    }

    [HttpPost("check-availability")]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckStockAvailability([FromBody] StockCheckRequest request, CancellationToken cancellationToken)
    {
        var issues = new List<object>();
        bool allAvailable = true;

        foreach (var item in request.Items)
        {
            var result = await _mediator.Send(new GetInventoryByProductIdQuery(item.ProductId), cancellationToken);
            if (!result.IsSuccess)
            {
                issues.Add(new { item.ProductId, Message = "Product not found" });
                allAvailable = false;
                continue;
            }
            var inv = result.GetDataOrThrow();
            if (inv.Quantity < item.Quantity)
            {
                issues.Add(new { item.ProductId, Available = inv.Quantity, Requested = item.Quantity, Message = "Insufficient stock" });
                allAvailable = false;
            }
        }

        return Ok(ApiResponse<object>.Ok(
            new { IsAvailable = allAvailable, Issues = issues },
            allAvailable ? "All items are available" : "Some items have stock issues"));
    }

    [HttpPut("{productId:guid}")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProductStock(Guid productId, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AdjustStockCommand(productId, request.Quantity, request.Reason ?? "stock_update"), cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.GetDataOrThrow(), "Product stock updated successfully"))
            : MapInventoryResult(result.GetErrorOrThrow());
    }

    [HttpPut("bulk-update")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<List<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BulkUpdateStock([FromBody] BulkStockUpdateRequest request, CancellationToken cancellationToken)
    {
        var responses = new List<object>();
        foreach (var update in request.Updates)
        {
            var result = await _mediator.Send(
                new AdjustStockCommand(update.ProductId, update.Quantity, "bulk_update"), cancellationToken);
            if (result.IsSuccess) responses.Add(result.GetDataOrThrow());
        }

        return Ok(ApiResponse<List<object>>.Ok(
            responses,
            "Stock updated successfully"));
    }

    private IActionResult MapInventoryResult(DomainError error) => error.Code switch
    {
        "INVENTORY_ITEM_NOT_FOUND"
            => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),

        "INSUFFICIENT_STOCK" or "STOCK_NEGATIVE"
        or "REDUCE_AMOUNT_INVALID" or "INCREASE_AMOUNT_INVALID"
        or "THRESHOLD_NEGATIVE"
            => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),

        "VALIDATION_FAILED"
            => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code)),

        _ => StatusCode(500, ApiResponse<object>.Failure("An unexpected error occurred.", error.Code))
    };
}

using ECommerce.API.ActionFilters;
using ECommerce.API.Common.Configuration;
using ECommerce.API.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MediatR;
using ECommerce.Ordering.Application.DTOs;
using ECommerce.Contracts.DTOs.Common;
using ECommerce.SharedKernel.Pagination;
using ECommerce.Contracts.DTOs.Orders;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Ordering.Application.Commands.PlaceOrder;
using ECommerce.Ordering.Application.Commands.ConfirmOrder;
using ECommerce.Ordering.Application.Commands.ShipOrder;
using ECommerce.Ordering.Application.Commands.CancelOrder;
using OrderingQueries = ECommerce.Ordering.Application.Queries;

namespace ECommerce.API.Features.Ordering.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Orders")]
[Authorize]
public class OrdersController(
    ICurrentUserService currentUser,
    IMediator mediator,
    ILogger<OrdersController> logger,
    IOptions<BusinessRulesOptions> businessRules) : ControllerBase
{
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<OrdersController> _logger = logger;
    private readonly IMediator _mediator = mediator;
    private readonly BusinessRulesOptions _businessRules = businessRules.Value;

    [HttpPost]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        _logger.LogInformation("Creating order via MediatR for user {UserId}", userId);

        var cmd = new PlaceOrderCommand
        {
            UserId = userId ?? Guid.Empty,
            ShippingAddressId = dto.ShippingAddress.Id ?? Guid.NewGuid(),
            CartItems = dto.Items?.Select(i => new CartItemInput(Guid.Parse(i.ProductId), i.Quantity)).ToList() ?? new(),
            PaymentMethod = dto.PaymentMethod ?? "card",
            PromoCode = dto.PromoCode,
            ShippingCost = _businessRules.StandardShippingCost,
            TaxRate = _businessRules.TaxRate
        };

        var result = await _mediator.Send(cmd, cancellationToken);
        return result.ToActionResult(
            id => CreatedAtAction(nameof(GetOrderById), new { id }, ApiResponse<Guid>.Ok(id, "Order created successfully")));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving order {OrderId}", id);
        var query = new OrderingQueries.GetOrderById.GetOrderByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return result.ToActionResult(
            data => Ok(ApiResponse<OrderDetailDto>.Ok(MapToOrderDetailDto(data), "Order retrieved successfully")));
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConfirmOrder(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Confirming order {OrderId}", id);
        var result = await _mediator.Send(new ConfirmOrderCommand(id), cancellationToken);
        return result.ToActionResult(
            id => Ok(ApiResponse<Guid>.Ok(id, "Order confirmed")));
    }

    [HttpPost("{id:guid}/ship")]
    [Authorize]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ShipOrder(Guid id, [FromBody] ShipOrderRequestDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shipping order {OrderId} with tracking {Tracking}", id, dto.TrackingNumber);
        var result = await _mediator.Send(new ShipOrderCommand(id, dto.TrackingNumber), cancellationToken);
        return result.ToActionResult(
            id => Ok(ApiResponse<Guid>.Ok(id, "Order shipped")));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all orders, page {Page}", page);
        var query = new OrderingQueries.GetOrders.GetOrdersQuery(page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return result.ToActionResult(
            paged => Ok(ApiResponse<PaginatedResult<OrderDetailDto>>.Ok(
                new PaginatedResult<OrderDetailDto>
                {
                    Items = paged.Items.Select(MapToOrderDetailDto).ToList(),
                    TotalCount = paged.TotalCount,
                    Page = paged.Page,
                    PageSize = paged.PageSize
                }, "Orders retrieved successfully")));
    }

    [HttpGet("my-orders")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        _logger.LogInformation("Retrieving orders for user {UserId}, page {Page}", userId.Value, page);
        var query = new OrderingQueries.GetUserOrders.GetUserOrdersQuery(userId.Value, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return result.ToActionResult(
            paged => Ok(ApiResponse<PaginatedResult<OrderDetailDto>>.Ok(
                new PaginatedResult<OrderDetailDto>
                {
                    Items = paged.Items.Select(MapToOrderDetailDto).ToList(),
                    TotalCount = paged.TotalCount,
                    Page = paged.Page,
                    PageSize = paged.PageSize
                }, "Orders retrieved successfully")));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequestDto? dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling order {OrderId}", id);
        var result = await _mediator.Send(new CancelOrderCommand(id, dto?.Reason ?? ""), cancellationToken);
        return result.ToActionResult(
            id => Ok(ApiResponse<Guid>.Ok(id, "Order cancelled")));
    }

    private static OrderDetailDto MapToOrderDetailDto(OrderDto order) => new()
    {
        Id = order.Id,
        UserId = order.UserId,
        OrderNumber = order.OrderNumber,
        Status = order.Status,
        TotalAmount = order.Total,
        CreatedAt = order.CreatedAt,
        Items = order.Items.Select(i => new OrderItemDto
        {
            Id = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.TotalPrice
        }).ToList()
    };
}

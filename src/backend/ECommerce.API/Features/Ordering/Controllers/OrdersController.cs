using System.Collections.Frozen;
using ECommerce.API.ActionFilters;
using ECommerce.API.Common.Configuration;
using ECommerce.API.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MediatR;
using ECommerce.Ordering.Application.DTOs;
using ECommerce.Contracts.DTOs.Common;
using ECommerce.Contracts.DTOs.Orders;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Ordering.Application.Commands.PlaceOrder;
using ECommerce.Ordering.Application.Commands.ConfirmOrder;
using ECommerce.Ordering.Application.Commands.ShipOrder;
using ECommerce.Ordering.Application.Commands.CancelOrder;
using ECommerce.Ordering.Application.Queries.GetOrderById;
using ECommerce.SharedKernel.Results;
using OrderingQueries = ECommerce.Ordering.Application.Queries;

namespace ECommerce.API.Features.Ordering.Controllers;

/// <summary>
/// Controller for order management.
/// </summary>
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
    private static readonly FrozenSet<string> _notFound = FrozenSet.Create(
        "ORDER_NOT_FOUND", "PROMO_CODE_NOT_FOUND", "ADDRESS_NOT_FOUND");

    private static readonly FrozenSet<string> _unauthorized = FrozenSet.Create(
        "UNAUTHORIZED");

    private static readonly FrozenSet<string> _forbidden = FrozenSet.Create(
        "FORBIDDEN");

    private static readonly FrozenSet<string> _unprocessable = FrozenSet.Create(
        "ORDER_EMPTY", "ORDER_TOTAL_INVALID", "ORDER_INVALID_TRANSITION",
        "PAYMENT_REF_EMPTY", "PAYMENT_AMOUNT_INVALID", "ORDER_STATUS_UNKNOWN");

    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<OrdersController> _logger = logger;
    private readonly IMediator _mediator = mediator;
    private readonly BusinessRulesOptions _businessRules = businessRules.Value;

    private IActionResult Problem(DomainError error)
    {
        var body = ApiResponse<object>.Failure(error.Message, error.Code);
        if (_notFound.Contains(error.Code))      return NotFound(body);
        if (_unauthorized.Contains(error.Code))  return Unauthorized(body);
        if (_forbidden.Contains(error.Code))     return StatusCode(StatusCodes.Status403Forbidden, body);
        if (_unprocessable.Contains(error.Code)) return UnprocessableEntity(body);
        return BadRequest(body);
    }

    /// <summary>
    /// Creates a new order using MediatR.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderDto dto,
        CancellationToken cancellationToken)
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
            id => CreatedAtAction(nameof(GetOrderById), new { id },
                ApiResponse<Guid>.Ok(id, "Order created successfully")),
            Problem);
    }

    /// <summary>
    /// Retrieves an order by its ID.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The order details.</returns>
    /// <response code="200">Order retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Order not found.</response>
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
            data => Ok(ApiResponse<OrderDetailDto>.Ok(MapToOrderDetailDto(data), "Order retrieved successfully")),
            Problem);
    }

    /// <summary>
    /// Confirms an order (transitions from Pending to Confirmed).
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConfirmOrder(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Confirming order {OrderId}", id);
        var cmd = new ConfirmOrderCommand(id);
        var result = await _mediator.Send(cmd, cancellationToken);

        return result.ToActionResult(
            id => Ok(ApiResponse<Guid>.Ok(id, "Order confirmed")),
            Problem);
    }

    /// <summary>
    /// Ships an order (transitions from Confirmed to Shipped).
    /// </summary>
    [HttpPost("{id:guid}/ship")]
    [Authorize]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ShipOrder(Guid id, [FromBody] ShipOrderRequestDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shipping order {OrderId} with tracking {Tracking}", id, dto.TrackingNumber);
        var cmd = new ShipOrderCommand(id, dto.TrackingNumber);
        var result = await _mediator.Send(cmd, cancellationToken);

        return result.ToActionResult(
            id => Ok(ApiResponse<Guid>.Ok(id, "Order shipped")),
            Problem);
    }

    /// <summary>
    /// Retrieves all orders (admin only) - paginated.
    /// </summary>
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
                    Items = paged.Items.ConvertAll(MapToOrderDetailDto),
                    TotalCount = paged.TotalCount,
                    Page = paged.Page,
                    PageSize = paged.PageSize
                }, "Orders retrieved successfully")),
            Problem);
    }

    /// <summary>
    /// Retrieves a paginated list of orders for the authenticated user.
    /// </summary>
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
                    Items = paged.Items.ConvertAll(MapToOrderDetailDto),
                    TotalCount = paged.TotalCount,
                    Page = paged.Page,
                    PageSize = paged.PageSize
                }, "Orders retrieved successfully")),
            Problem);
    }

    /// <summary>
    /// Cancels an order if it hasn't been shipped yet.
    /// </summary>
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
        var cmd = new CancelOrderCommand(id, dto?.Reason ?? "");
        var result = await _mediator.Send(cmd, cancellationToken);

        return result.ToActionResult(
            id => Ok(ApiResponse<Guid>.Ok(id, "Order cancelled")),
            Problem);
    }

    private static OrderDetailDto MapToOrderDetailDto(OrderDto order)
    {
        return new OrderDetailDto
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

}

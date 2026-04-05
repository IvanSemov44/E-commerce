using ECommerce.API.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using ECommerce.Core.Results;
using ECommerce.Ordering.Application.Commands.PlaceOrder;
using ECommerce.Ordering.Application.Commands.ConfirmOrder;
using ECommerce.Ordering.Application.Commands.ShipOrder;
using ECommerce.Ordering.Application.Commands.CancelOrder;
using ECommerce.Ordering.Application.Queries.GetOrderById;
using ECommerce.SharedKernel.Results;
using OrderingQueries = ECommerce.Ordering.Application.Queries;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for order management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private const string IdempotencyHeaderName = "Idempotency-Key";

    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<OrdersController> _logger;
    private readonly IMediator _mediator;

    public OrdersController(
        ICurrentUserService currentUser,
        IMediator mediator,
        ILogger<OrdersController> logger)
    {
        _currentUser = currentUser;
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new order using MediatR.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status201Created)]
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
            ShippingAddressId = Guid.NewGuid(),
            CartItems = dto.Items?.Select(i => new CartItemInput(Guid.Parse(i.ProductId), i.Quantity)).ToList() ?? new(),
            PaymentMethod = dto.PaymentMethod ?? "card",
            PaymentReference = Guid.NewGuid().ToString(),
            PromoCode = dto.PromoCode,
            ShippingCost = 10m,
            TaxAmount = 0m
        };

        var cmdResult = await _mediator.Send(cmd, cancellationToken);
        var result = (ECommerce.SharedKernel.Results.Result<ECommerce.Ordering.Application.DTOs.OrderDto>)cmdResult;

        if (result is ECommerce.SharedKernel.Results.Result<ECommerce.Ordering.Application.DTOs.OrderDto>.Success success)
        {
            return CreatedAtAction(nameof(GetOrderById), new { id = success.Data.Id },
                ApiResponse<OrderDetailDto>.Ok(MapToOrderDetailDto(success.Data), "Order created successfully"));
        }

        if (result is ECommerce.SharedKernel.Results.Result<ECommerce.Ordering.Application.DTOs.OrderDto>.Failure failure)
        {
            var statusCode = failure.Error.Code switch
            {
                "ORDER_NOT_FOUND" => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, ApiResponse<object>.Failure(failure.Error.Message, failure.Error.Code));
        }

        return StatusCode(500, ApiResponse<object>.Failure("Unknown error occurred", "INTERNAL_ERROR"));
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
        var currentUserId = _currentUser.UserIdOrNull;
        var role = _currentUser.RoleOrNull;
        var isAdmin = _currentUser.IsAuthenticated &&
                     (role == Core.Enums.UserRole.Admin || role == Core.Enums.UserRole.SuperAdmin);

        var query = new OrderingQueries.GetOrderById.GetOrderByIdQuery(id);
        var skResult = await _mediator.Send(query, cancellationToken);
        var result = (ECommerce.SharedKernel.Results.Result<ECommerce.Ordering.Application.DTOs.OrderDto>)skResult;

        if (result is ECommerce.SharedKernel.Results.Result<ECommerce.Ordering.Application.DTOs.OrderDto>.Success success)
            return Ok(ApiResponse<OrderDetailDto>.Ok(MapToOrderDetailDto(success.Data), "Order retrieved successfully"));

        if (result is ECommerce.SharedKernel.Results.Result<ECommerce.Ordering.Application.DTOs.OrderDto>.Failure failure)
        {
            var statusCode = failure.Error.Code switch
            {
                "ORDER_NOT_FOUND" => StatusCodes.Status404NotFound,
                "FORBIDDEN" => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, ApiResponse<object>.Failure(failure.Error.Message, failure.Error.Code));
        }

        return StatusCode(500, ApiResponse<object>.Failure("Unknown error occurred", "INTERNAL_ERROR"));
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
        var cmdResult = await _mediator.Send(cmd, cancellationToken);
        var result = (ECommerce.SharedKernel.Results.Result<ECommerce.Ordering.Application.DTOs.OrderDto>)cmdResult;

        if (result is ECommerce.SharedKernel.Results.Result<ECommerce.Ordering.Application.DTOs.OrderDto>.Success success)
            return Ok(ApiResponse<OrderDetailDto>.Ok(MapToOrderDetailDto(success.Data), "Order confirmed"));

        if (result is ECommerce.SharedKernel.Results.Result<ECommerce.Ordering.Application.DTOs.OrderDto>.Failure failure)
        {
            var statusCode = failure.Error.Code switch
            {
                "ORDER_NOT_FOUND" => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status422UnprocessableEntity
            };
            return StatusCode(statusCode, ApiResponse<object>.Failure(failure.Error.Message, failure.Error.Code));
        }

        return StatusCode(500, ApiResponse<object>.Failure("Unknown error occurred", "INTERNAL_ERROR"));
    }

    /// <summary>
    /// Ships an order (transitions from Confirmed to Shipped).
    /// </summary>
    [HttpPost("{id:guid}/ship")]
    [Authorize]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ShipOrder(Guid id, [FromBody] ShipOrderRequestDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shipping order {OrderId} with tracking {Tracking}", id, dto.TrackingNumber);
        var cmd = new ShipOrderCommand(id, dto.TrackingNumber);
        var cmdResult = await _mediator.Send(cmd, cancellationToken);
        var result = (ECommerce.SharedKernel.Results.Result<ECommerce.Ordering.Application.DTOs.OrderDto>)cmdResult;

        if (result is ECommerce.SharedKernel.Results.Result<ECommerce.Ordering.Application.DTOs.OrderDto>.Success success)
            return Ok(ApiResponse<OrderDetailDto>.Ok(MapToOrderDetailDto(success.Data), "Order shipped"));

        if (result is ECommerce.SharedKernel.Results.Result<ECommerce.Ordering.Application.DTOs.OrderDto>.Failure failure)
        {
            var statusCode = failure.Error.Code switch
            {
                "ORDER_NOT_FOUND" => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status422UnprocessableEntity
            };
            return StatusCode(statusCode, ApiResponse<object>.Failure(failure.Error.Message, failure.Error.Code));
        }

        return StatusCode(500, ApiResponse<object>.Failure("Unknown error occurred", "INTERNAL_ERROR"));
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

        var query = new OrderingQueries.GetOrders.GetOrdersQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (result is ECommerce.SharedKernel.Results.Result<List<ECommerce.Ordering.Application.DTOs.OrderDto>>.Success success)
        {
            var allOrders = success.Data.Select(MapToOrderDetailDto).ToList();
            var total = allOrders.Count;
            var paginatedItems = allOrders.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var paginatedResult = new PaginatedResult<OrderDetailDto>
            {
                Items = paginatedItems,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
            return Ok(ApiResponse<PaginatedResult<OrderDetailDto>>.Ok(paginatedResult, "Orders retrieved successfully"));
        }

        if (result is ECommerce.SharedKernel.Results.Result<List<ECommerce.Ordering.Application.DTOs.OrderDto>>.Failure failure)
            return StatusCode(500, ApiResponse<object>.Failure(failure.Error.Message, failure.Error.Code));

        return StatusCode(500, ApiResponse<object>.Failure("Unknown error occurred", "INTERNAL_ERROR"));
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

        var query = new OrderingQueries.GetUserOrders.GetUserOrdersQuery(userId.Value);
        var result = await _mediator.Send(query, cancellationToken);

        if (result is ECommerce.SharedKernel.Results.Result<List<ECommerce.Ordering.Application.DTOs.OrderDto>>.Success success)
        {
            var allOrders = success.Data.Select(MapToOrderDetailDto).ToList();
            var total = allOrders.Count;
            var paginatedItems = allOrders.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var paginatedResult = new PaginatedResult<OrderDetailDto>
            {
                Items = paginatedItems,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
            return Ok(ApiResponse<PaginatedResult<OrderDetailDto>>.Ok(paginatedResult, "Orders retrieved successfully"));
        }

        if (result is ECommerce.SharedKernel.Results.Result<List<ECommerce.Ordering.Application.DTOs.OrderDto>>.Failure failure)
            return StatusCode(500, ApiResponse<object>.Failure(failure.Error.Message, failure.Error.Code));

        return StatusCode(500, ApiResponse<object>.Failure("Unknown error occurred", "INTERNAL_ERROR"));
    }

    /// <summary>
    /// Cancels an order if it hasn't been shipped yet.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequestDto? dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling order {OrderId}", id);
        var cmd = new CancelOrderCommand(id, dto?.Reason ?? "");
        var cmdResult = await _mediator.Send(cmd, cancellationToken);
        var result = (ECommerce.SharedKernel.Results.Result<ECommerce.Ordering.Application.DTOs.OrderDto>)cmdResult;

        if (result is ECommerce.SharedKernel.Results.Result<ECommerce.Ordering.Application.DTOs.OrderDto>.Success success)
            return Ok(ApiResponse<OrderDetailDto>.Ok(MapToOrderDetailDto(success.Data), "Order cancelled"));

        if (result is ECommerce.SharedKernel.Results.Result<ECommerce.Ordering.Application.DTOs.OrderDto>.Failure failure)
        {
            var statusCode = failure.Error.Code switch
            {
                "ORDER_NOT_FOUND" => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status422UnprocessableEntity
            };
            return StatusCode(statusCode, ApiResponse<object>.Failure(failure.Error.Message, failure.Error.Code));
        }

        return StatusCode(500, ApiResponse<object>.Failure("Unknown error occurred", "INTERNAL_ERROR"));
    }

    private BadRequestObjectResult? ValidateIdempotencyKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || !Guid.TryParse(idempotencyKey, out _))
        {
            return BadRequest(ApiResponse<object>.Failure(
                $"{IdempotencyHeaderName} header is required and must be a valid UUID",
                "INVALID_IDEMPOTENCY_KEY"));
        }

        return null;
    }

    private bool TryBuildInProgressIdempotencyResponse(IdempotencyStartStatus status, out IActionResult? response)
    {
        if (status == IdempotencyStartStatus.InProgress)
        {
            response = Conflict(ApiResponse<object>.Failure(
                "Request with this idempotency key is already being processed",
                "IDEMPOTENCY_IN_PROGRESS"));
            return true;
        }

        response = null;
        return false;
    }

    private static OrderDetailDto MapToOrderDetailDto(ECommerce.Ordering.Application.DTOs.OrderDto order)
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

// Request DTOs
public class ShipOrderRequestDto
{
    public string TrackingNumber { get; set; } = null!;
}

public class CancelOrderRequestDto
{
    public string? Reason { get; set; }
}



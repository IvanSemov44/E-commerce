using ECommerce.API.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using ECommerce.Core.Results;

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

    private readonly IOrderService _orderService;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderService orderService,
        ICurrentUserService currentUser,
        IIdempotencyStore idempotencyStore,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _currentUser = currentUser;
        _idempotencyStore = idempotencyStore;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new order for the authenticated user from their shopping cart.
    /// </summary>
    /// <param name="dto">The order creation details including shipping and payment information.</param>
    /// <param name="idempotencyKey">Idempotency key header to prevent duplicate order creation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created order with all details.</returns>
    /// <response code="201">Order created successfully.</response>
    /// <response code="400">Invalid order data or cart validation failed.</response>
    /// <response code="409">Order conflict such as insufficient stock or invalid inventory state.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Cart or products not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderDto dto,
        [FromHeader(Name = IdempotencyHeaderName)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var idempotencyError = ValidateIdempotencyKey(idempotencyKey);
        if (idempotencyError != null)
        {
            return idempotencyError;
        }

        var idempotencyStoreKey = $"orders:create:{idempotencyKey}";
        var idempotencyStart = await _idempotencyStore.StartAsync<OrderDetailDto>(idempotencyStoreKey, TimeSpan.FromMinutes(5), cancellationToken);
        if (idempotencyStart.Status == IdempotencyStartStatus.Replay && idempotencyStart.CachedResponse != null)
        {
            _logger.LogInformation("Returning cached idempotent order response for key {IdempotencyKey}", idempotencyKey);
            return CreatedAtAction(nameof(GetOrderById), new { id = idempotencyStart.CachedResponse.Id },
                ApiResponse<OrderDetailDto>.Ok(idempotencyStart.CachedResponse, "Order created successfully"));
        }

        if (TryBuildInProgressIdempotencyResponse(idempotencyStart.Status, out var inProgressResponse))
        {
            return inProgressResponse;
        }

        var userId = _currentUser.UserIdOrNull;
        _logger.LogInformation("Creating order for user {UserId}. Guest: {IsGuest}", userId, userId == null);

        var result = await _orderService.CreateOrderAsync(userId, dto, cancellationToken: cancellationToken);

        if (result is Result<OrderDetailDto>.Success success)
        {
            await _idempotencyStore.CompleteAsync(idempotencyStoreKey, success.Data, TimeSpan.FromHours(24), cancellationToken);

            return CreatedAtAction(nameof(GetOrderById), new { id = success.Data.Id },
                ApiResponse<OrderDetailDto>.Ok(success.Data, "Order created successfully"));
        }

        if (result is Result<OrderDetailDto>.Failure failure)
        {
            await _idempotencyStore.AbandonAsync(idempotencyStoreKey, cancellationToken);

            var statusCode = failure.Code switch
            {
                "USER_NOT_FOUND" or "ORDER_NOT_FOUND" => StatusCodes.Status404NotFound,
                "INSUFFICIENT_STOCK" or "INVALID_QUANTITY" or "PRODUCT_NOT_AVAILABLE" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, ApiResponse<OrderDetailDto>.Failure(new ErrorResponse
            {
                Message = failure.Message,
                Code = failure.Code
            }));
        }

        await _idempotencyStore.AbandonAsync(idempotencyStoreKey, cancellationToken);

        return StatusCode(500, ApiResponse<OrderDetailDto>.Failure(new ErrorResponse
        {
            Message = "Unknown error occurred",
            Code = "INTERNAL_ERROR"
        }));
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
        var result = await _orderService.GetOrderByIdForUserAsync(id, currentUserId, isAdmin, cancellationToken: cancellationToken);

        if (result is Result<OrderDetailDto>.Success success)
            return Ok(ApiResponse<OrderDetailDto>.Ok(success.Data, "Order retrieved successfully"));

        if (result is Result<OrderDetailDto>.Failure failure)
        {
            var statusCode = failure.Code switch
            {
                "ORDER_NOT_FOUND" => StatusCodes.Status404NotFound,
                "FORBIDDEN" => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, ApiResponse<object>.Failure(failure.Message, failure.Code));
        }

        return StatusCode(500, ApiResponse<object>.Failure("Unknown error occurred", "INTERNAL_ERROR"));
    }

    /// <summary>
    /// Retrieves an order by its order number.
    /// </summary>
    /// <param name="orderNumber">The order number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The order details.</returns>
    /// <response code="200">Order retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Order not found.</response>
    [HttpGet("number/{orderNumber}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrderByNumber(string orderNumber, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving order by number {OrderNumber}", orderNumber);
        var currentUserId = _currentUser.UserIdOrNull;
        var role = _currentUser.RoleOrNull;
        var isAdmin = _currentUser.IsAuthenticated &&
                     (role == Core.Enums.UserRole.Admin || role == Core.Enums.UserRole.SuperAdmin);
        var result = await _orderService.GetOrderByNumberForUserAsync(orderNumber, currentUserId, isAdmin, cancellationToken: cancellationToken);

        if (result is Result<OrderDetailDto>.Success success)
            return Ok(ApiResponse<OrderDetailDto>.Ok(success.Data, "Order retrieved successfully"));

        if (result is Result<OrderDetailDto>.Failure failure)
        {
            var statusCode = failure.Code switch
            {
                "ORDER_NOT_FOUND" => StatusCodes.Status404NotFound,
                "FORBIDDEN" => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, ApiResponse<object>.Failure(failure.Message, failure.Code));
        }

        return StatusCode(500, ApiResponse<object>.Failure("Unknown error occurred", "INTERNAL_ERROR"));
    }

    /// <summary>
    /// Retrieves a paginated list of orders for the authenticated user.
    /// </summary>
    /// <param name="parameters">Query parameters for filtering and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of the user's orders.</returns>
    /// <response code="200">Orders retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrders([FromQuery] OrderQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserIdOrNull;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated", "USER_NOT_AUTHENTICATED"));

        _logger.LogInformation("Retrieving orders for user {UserId}, page {Page}", userId.Value, parameters.Page);

        var result = await _orderService.GetUserOrdersAsync(userId.Value, parameters, cancellationToken: cancellationToken);

        return Ok(ApiResponse<PaginatedResult<OrderDto>>.Ok(result, "Orders retrieved successfully"));
    }

    /// <summary>
    /// Retrieves a paginated list of all orders in the system (admin only).
    /// </summary>
    /// <param name="parameters">Query parameters for filtering and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of all orders.</returns>
    /// <response code="200">Orders retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view all orders.</response>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllOrders([FromQuery] OrderQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all orders, page {Page}", parameters.Page);
        var result = await _orderService.GetAllOrdersAsync(parameters, cancellationToken: cancellationToken);
        return Ok(ApiResponse<PaginatedResult<OrderDto>>.Ok(result, "Orders retrieved successfully"));
    }

    /// <summary>
    /// Updates the status of an order (admin only).
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="statusUpdate">The new order status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated order.</returns>
    /// <response code="200">Order status updated successfully.</response>
    /// <response code="400">Invalid status or status transition not allowed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to update order status.</response>
    /// <response code="404">Order not found.</response>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto statusUpdate, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating order {OrderId} status to {Status}", id, statusUpdate.Status);
        var result = await _orderService.UpdateOrderStatusAsync(id, statusUpdate.Status, statusUpdate.TrackingNumber, cancellationToken: cancellationToken);

        if (result is Result<OrderDetailDto>.Success success)
        {
            return Ok(ApiResponse<OrderDetailDto>.Ok(success.Data, "Order status updated successfully"));
        }

        if (result is Result<OrderDetailDto>.Failure failure)
        {
            var statusCode = failure.Code switch
            {
                "ORDER_NOT_FOUND" => StatusCodes.Status404NotFound,
                "INVALID_ORDER_STATUS" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, ApiResponse<OrderDetailDto>.Failure(new ErrorResponse
            {
                Message = failure.Message,
                Code = failure.Code
            }));
        }

        return StatusCode(500, ApiResponse<OrderDetailDto>.Failure(new ErrorResponse
        {
            Message = "Unknown error occurred",
            Code = "INTERNAL_ERROR"
        }));
    }

    /// <summary>
    /// Cancels an order if it hasn't been shipped yet.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cancellation result.</returns>
    /// <response code="200">Order cancelled successfully.</response>
    /// <response code="400">Order cannot be cancelled because it has already been shipped or delivered.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to cancel this order.</response>
    /// <response code="404">Order not found.</response>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromHeader(Name = IdempotencyHeaderName)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var idempotencyError = ValidateIdempotencyKey(idempotencyKey);
        if (idempotencyError != null)
        {
            return idempotencyError;
        }

        var idempotencyStoreKey = $"orders:cancel:{id}:{idempotencyKey}";
        var idempotencyStart = await _idempotencyStore.StartAsync<object>(idempotencyStoreKey, TimeSpan.FromMinutes(5), cancellationToken);
        if (idempotencyStart.Status == IdempotencyStartStatus.Replay)
        {
            _logger.LogInformation("Returning cached idempotent cancel response for order {OrderId} and key {IdempotencyKey}", id, idempotencyKey);
            return Ok(ApiResponse<object>.Ok(new object(), "Order cancelled successfully"));
        }

        if (TryBuildInProgressIdempotencyResponse(idempotencyStart.Status, out var inProgressResponse))
        {
            return inProgressResponse;
        }

        _logger.LogInformation("Cancelling order {OrderId}", id);
        var currentUserId = _currentUser.UserIdOrNull;
        var role = _currentUser.RoleOrNull;
        var isAdmin = _currentUser.IsAuthenticated &&
                     (role == Core.Enums.UserRole.Admin || role == Core.Enums.UserRole.SuperAdmin);
        var result = await _orderService.CancelOrderAsync(id, currentUserId, isAdmin, cancellationToken: cancellationToken);

        if (result is Result<Unit>.Failure failure)
        {
            await _idempotencyStore.AbandonAsync(idempotencyStoreKey, cancellationToken);

            var statusCode = failure.Code switch
            {
                "ORDER_NOT_FOUND" => StatusCodes.Status404NotFound,
                "FORBIDDEN" => StatusCodes.Status403Forbidden,
                "INVALID_ORDER_STATUS" => StatusCodes.Status400BadRequest,
                "CONCURRENCY_CONFLICT" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, ApiResponse<object>.Failure(failure.Message, failure.Code));
        }

        await _idempotencyStore.CompleteAsync(idempotencyStoreKey, new object(), TimeSpan.FromHours(24), cancellationToken);

        return Ok(ApiResponse<object>.Ok(new object(), "Order cancelled successfully"));
    }

    private IActionResult? ValidateIdempotencyKey(string? idempotencyKey)
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
}



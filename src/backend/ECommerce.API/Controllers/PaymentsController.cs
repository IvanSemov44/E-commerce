using ECommerce.API.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using ECommerce.Core.Results;
using System.Text.Json;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for payment processing (mocked Stripe/PayPal integration).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private const string IdempotencyHeaderName = "Idempotency-Key";
    private static readonly JsonSerializerOptions WebhookJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IPaymentService _paymentService;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IWebhookVerificationService _webhookVerificationService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        ICurrentUserService currentUser,
        IIdempotencyStore idempotencyStore,
        IWebhookVerificationService webhookVerificationService,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _currentUser = currentUser;
        _idempotencyStore = idempotencyStore;
        _webhookVerificationService = webhookVerificationService;
        _logger = logger;
    }

    /// <summary>
    /// Process a payment for an order.
    /// </summary>
    /// <param name="dto">Payment details including order ID and payment method.</param>
    /// <param name="idempotencyKey">Idempotency key header to prevent duplicate payment processing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payment response with success/failure status and payment intent ID.</returns>
    /// <response code="200">Payment processed successfully or failed with details.</response>
    /// <response code="400">Invalid payment request.</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    /// <response code="404">Order not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("process")]
    [Authorize]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] ProcessPaymentDto dto,
        [FromHeader(Name = IdempotencyHeaderName)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var idempotencyError = ValidateIdempotencyKey(idempotencyKey);
        if (idempotencyError != null)
        {
            return idempotencyError;
        }

        var idempotencyStoreKey = $"payments:process:{idempotencyKey}";
        var idempotencyStart = await _idempotencyStore.StartAsync<PaymentResponseDto>(idempotencyStoreKey, TimeSpan.FromMinutes(5), cancellationToken);
        if (idempotencyStart.Status == IdempotencyStartStatus.Replay && idempotencyStart.CachedResponse != null)
        {
            _logger.LogInformation("Returning cached idempotent payment response for key {IdempotencyKey}", idempotencyKey);
            return Ok(ApiResponse<PaymentResponseDto>.Ok(idempotencyStart.CachedResponse, "Payment processed successfully"));
        }

        if (TryBuildInProgressIdempotencyResponse(idempotencyStart.Status, out var inProgressResponse))
        {
            return inProgressResponse!;
        }

        _logger.LogInformation("Payment processing initiated for order {OrderId} via {PaymentMethod}",
            dto.OrderId, dto.PaymentMethod);

        var result = await _paymentService.ProcessPaymentAsync(dto, cancellationToken: cancellationToken);

        if (result is Result<PaymentResponseDto>.Success success)
        {
            _logger.LogInformation("Payment result for order {OrderId}. PaymentIntentId: {PaymentIntentId}",
                dto.OrderId, success.Data.PaymentIntentId);

            if (success.Data.Success)
            {
                await _idempotencyStore.CompleteAsync(idempotencyStoreKey, success.Data, TimeSpan.FromHours(24), cancellationToken);
                return Ok(ApiResponse<PaymentResponseDto>.Ok(success.Data, "Payment processed successfully"));
            }
            else
            {
                await _idempotencyStore.AbandonAsync(idempotencyStoreKey, cancellationToken);
                return UnprocessableEntity(ApiResponse<PaymentResponseDto>.Failure(success.Data.Message, "PAYMENT_DECLINED"));
            }
        }

        if (result is Result<PaymentResponseDto>.Failure failure)
        {
            await _idempotencyStore.AbandonAsync(idempotencyStoreKey, cancellationToken);

            var statusCode = failure.Code switch
            {
                "ORDER_NOT_FOUND" => StatusCodes.Status404NotFound,
                "UNSUPPORTED_PAYMENT_METHOD" => StatusCodes.Status422UnprocessableEntity,
                "PAYMENT_AMOUNT_MISMATCH" => StatusCodes.Status400BadRequest,
                "CONCURRENCY_CONFLICT" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, ApiResponse<PaymentResponseDto>.Failure(failure.Message, failure.Code));
        }

        await _idempotencyStore.AbandonAsync(idempotencyStoreKey, cancellationToken);

        return StatusCode(500, ApiResponse<PaymentResponseDto>.Failure("Unknown error occurred", "INTERNAL_ERROR"));
    }

    /// <summary>
    /// Get payment details for a specific order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payment details including status and amount.</returns>
    /// <response code="200">Payment details retrieved successfully.</response>
    /// <response code="403">User does not have permission to view payment details for this order.</response>
    /// <response code="404">Order or payment not found.</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{orderId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PaymentDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentDetails(Guid orderId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving payment details for order {OrderId}", orderId);
        var currentUserId = _currentUser.UserIdOrNull;
        var role = _currentUser.RoleOrNull;
        var isAdmin = _currentUser.IsAuthenticated &&
                     (role == Core.Enums.UserRole.Admin || role == Core.Enums.UserRole.SuperAdmin);
        var result = await _paymentService.GetPaymentDetailsAsync(orderId, currentUserId, isAdmin, cancellationToken: cancellationToken);

        if (result is Result<PaymentDetailsDto>.Success success)
        {
            return Ok(ApiResponse<PaymentDetailsDto>.Ok(success.Data, "Payment details retrieved successfully"));
        }

        if (result is Result<PaymentDetailsDto>.Failure failure)
        {
            var statusCode = failure.Code switch
            {
                "ORDER_NOT_FOUND" => StatusCodes.Status404NotFound,
                "NO_PAYMENT_FOUND" => StatusCodes.Status404NotFound,
                "FORBIDDEN" => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, ApiResponse<PaymentDetailsDto>.Failure(failure.Message, failure.Code));
        }

        return StatusCode(500, ApiResponse<PaymentDetailsDto>.Failure("Unknown error occurred", "INTERNAL_ERROR"));
    }

    /// <summary>
    /// Refund a payment for an order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="dto">Refund details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Refund response with status and refund ID.</returns>
    /// <response code="200">Refund processed successfully.</response>
    /// <response code="400">Invalid refund request.</response>
    /// <response code="404">Order or payment not found.</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    /// <response code="403">Forbidden - insufficient permissions.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("{orderId:guid}/refund")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<RefundResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefundPayment(
        Guid orderId,
        [FromBody] RefundPaymentDto dto,
        [FromHeader(Name = IdempotencyHeaderName)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var idempotencyError = ValidateIdempotencyKey(idempotencyKey);
        if (idempotencyError != null)
        {
            return idempotencyError;
        }

        var idempotencyStoreKey = $"payments:refund:{orderId}:{idempotencyKey}";
        var idempotencyStart = await _idempotencyStore.StartAsync<RefundResponseDto>(idempotencyStoreKey, TimeSpan.FromMinutes(5), cancellationToken);
        if (idempotencyStart.Status == IdempotencyStartStatus.Replay && idempotencyStart.CachedResponse != null)
        {
            _logger.LogInformation("Returning cached idempotent refund response for order {OrderId} and key {IdempotencyKey}", orderId, idempotencyKey);
            return Ok(ApiResponse<RefundResponseDto>.Ok(idempotencyStart.CachedResponse, "Refund processed successfully"));
        }

        if (TryBuildInProgressIdempotencyResponse(idempotencyStart.Status, out var inProgressResponse))
        {
            return inProgressResponse!;
        }

        dto.OrderId = orderId;

        _logger.LogInformation("Refund initiated for order {OrderId}", orderId);

        var result = await _paymentService.RefundPaymentAsync(dto, cancellationToken: cancellationToken);

        if (result is Result<RefundResponseDto>.Success success)
        {
            await _idempotencyStore.CompleteAsync(idempotencyStoreKey, success.Data, TimeSpan.FromHours(24), cancellationToken);

            _logger.LogInformation("Refund processed for order {OrderId}. RefundId: {RefundId}",
                orderId, success.Data.RefundId);
            return Ok(ApiResponse<RefundResponseDto>.Ok(success.Data, "Refund processed successfully"));
        }

        if (result is Result<RefundResponseDto>.Failure failure)
        {
            await _idempotencyStore.AbandonAsync(idempotencyStoreKey, cancellationToken);

            var statusCode = failure.Code switch
            {
                "ORDER_NOT_FOUND" => StatusCodes.Status404NotFound,
                "INVALID_REFUND" => StatusCodes.Status400BadRequest,
                "CONCURRENCY_CONFLICT" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, ApiResponse<RefundResponseDto>.Failure(failure.Message, failure.Code));
        }

        await _idempotencyStore.AbandonAsync(idempotencyStoreKey, cancellationToken);

        return StatusCode(500, ApiResponse<RefundResponseDto>.Failure("Unknown error occurred", "INTERNAL_ERROR"));
    }

    /// <summary>
    /// Get payment intent details.
    /// </summary>
    /// <param name="paymentIntentId">The payment intent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payment intent details if found.</returns>
    /// <response code="200">Payment intent details retrieved successfully.</response>
    /// <response code="404">Payment intent not found.</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("intent/{paymentIntentId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentIntent(string paymentIntentId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving payment intent {PaymentIntentId}", paymentIntentId);

        var paymentDetails = await _paymentService.GetPaymentIntentAsync(paymentIntentId, cancellationToken: cancellationToken);

        if (paymentDetails is Result<PaymentDetailsDto>.Failure failure)
        {
            _logger.LogWarning("Payment intent {PaymentIntentId} not found", paymentIntentId);
            return NotFound(ApiResponse<object>.Failure(failure.Message, failure.Code));
        }

        return Ok(ApiResponse<PaymentDetailsDto>.Ok(((Result<PaymentDetailsDto>.Success)paymentDetails).Data, "Payment intent retrieved successfully"));
    }

    /// <summary>
    /// Get list of supported payment methods.
    /// </summary>
    /// <returns>List of supported payment methods.</returns>
    /// <response code="200">Supported payment methods retrieved successfully.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("methods")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SupportedPaymentMethodsResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public IActionResult GetSupportedPaymentMethods()
    {
        var response = new SupportedPaymentMethodsResponseDto
        {
            Methods = new List<string>
            {
                "stripe",
                "paypal",
                "credit_card",
                "debit_card",
                "apple_pay",
                "google_pay"
            }
        };

        return Ok(ApiResponse<SupportedPaymentMethodsResponseDto>.Ok(response, "Supported payment methods retrieved successfully"));
    }

    /// <summary>
    /// Health check endpoint for payment service.
    /// </summary>
    /// <returns>Payment service status.</returns>
    /// <response code="200">Payment service is healthy.</response>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<HealthCheckResponseDto>), StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        var response = new HealthCheckResponseDto
        {
            Status = "healthy",
            Service = "PaymentService",
            Timestamp = DateTime.UtcNow
        };

        return Ok(ApiResponse<HealthCheckResponseDto>.Ok(response, "Payment service is healthy"));
    }

    /// <summary>
    /// Process payment webhook from payment providers (Stripe, PayPal).
    /// This endpoint allows anonymous access for payment provider callbacks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Webhook processing response.</returns>
    /// <response code="200">Webhook processed successfully.</response>
    /// <response code="400">Invalid webhook payload.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessPaymentWebhook(CancellationToken cancellationToken)
    {
        // Read raw body for signature verification
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0; // Reset stream position

        // Verify webhook signature
        var signature = Request.Headers["X-Webhook-Signature"].FirstOrDefault();
        if (!_webhookVerificationService.VerifySignature(rawBody, signature ?? string.Empty))
        {
            _logger.LogWarning("Webhook signature verification failed from IP {IP}",
                HttpContext.Connection.RemoteIpAddress);
            return Unauthorized(ApiResponse<object>.Failure("Invalid webhook signature", "INVALID_WEBHOOK_SIGNATURE"));
        }

        // Deserialize after verification
        var webhookPayload = JsonSerializer.Deserialize<PaymentWebhookDto>(rawBody, WebhookJsonOptions);

        if (webhookPayload == null)
        {
            _logger.LogWarning("Invalid webhook payload received");
            return BadRequest(ApiResponse<object>.Failure("Invalid webhook payload", "INVALID_WEBHOOK_PAYLOAD"));
        }

        _logger.LogInformation("Verified webhook received for event type: {EventType}",
            webhookPayload.EventType ?? "unknown");

        // Process the verified webhook event
        // In a real implementation:
        // 1. Update order status based on payment status
        // 2. Store the webhook event for audit purposes
        // 3. Trigger any necessary business logic

        _logger.LogInformation("Webhook processed successfully for PaymentIntentId: {PaymentIntentId}",
            webhookPayload.PaymentIntentId ?? "unknown");

        return Ok(ApiResponse<object>.Ok(new { status = "received" }, "Webhook processed successfully"));
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
}


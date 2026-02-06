using ECommerce.API.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for payment processing (mocked Stripe/PayPal integration).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IWebhookVerificationService _webhookVerificationService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        IWebhookVerificationService webhookVerificationService,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _webhookVerificationService = webhookVerificationService;
        _logger = logger;
    }

    /// <summary>
    /// Process a payment for an order.
    /// </summary>
    /// <param name="dto">Payment details including order ID and payment method.</param>
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Payment processing initiated for order {OrderId} via {PaymentMethod}",
            dto.OrderId, dto.PaymentMethod);

        var result = await _paymentService.ProcessPaymentAsync(dto, cancellationToken: cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("Payment successful for order {OrderId}. PaymentIntentId: {PaymentIntentId}",
                dto.OrderId, result.PaymentIntentId);
            return Ok(ApiResponse<PaymentResponseDto>.Ok(result, "Payment processed successfully"));
        }
        else
        {
            _logger.LogWarning("Payment failed for order {OrderId}. Reason: {Message}",
                dto.OrderId, result.Message);
            return UnprocessableEntity(ApiResponse<PaymentResponseDto>.Error(result.Message));
        }
    }

    /// <summary>
    /// Get payment details for a specific order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payment details including status and amount.</returns>
    /// <response code="200">Payment details retrieved successfully.</response>
    /// <response code="404">Order or payment not found.</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{orderId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PaymentDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentDetails(Guid orderId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving payment details for order {OrderId}", orderId);

        var paymentDetails = await _paymentService.GetPaymentDetailsAsync(orderId, cancellationToken: cancellationToken);
        return Ok(ApiResponse<PaymentDetailsDto>.Ok(paymentDetails, "Payment details retrieved successfully"));
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefundPayment(Guid orderId, [FromBody] RefundPaymentDto dto, CancellationToken cancellationToken)
    {
        dto.OrderId = orderId;

        _logger.LogInformation("Refund initiated for order {OrderId}", orderId);

        var result = await _paymentService.RefundPaymentAsync(dto, cancellationToken: cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("Refund successful for order {OrderId}. RefundId: {RefundId}",
                orderId, result.RefundId);
            return Ok(ApiResponse<RefundResponseDto>.Ok(result, "Refund processed successfully"));
        }
        else
        {
            _logger.LogWarning("Refund failed for order {OrderId}. Reason: {Message}",
                orderId, result.Message);
            return Ok(ApiResponse<RefundResponseDto>.Ok(result, "Refund processing failed"));
        }
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentIntent(string paymentIntentId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving payment intent {PaymentIntentId}", paymentIntentId);

        var paymentDetails = await _paymentService.GetPaymentIntentAsync(paymentIntentId, cancellationToken: cancellationToken);

        if (paymentDetails == null)
        {
            _logger.LogWarning("Payment intent {PaymentIntentId} not found", paymentIntentId);
            return NotFound(ApiResponse<string>.Error("Payment intent not found"));
        }

        return Ok(ApiResponse<PaymentDetailsDto>.Ok(paymentDetails, "Payment intent retrieved successfully"));
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
    /// <param name="webhookPayload">Webhook event payload from payment provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Webhook processing response.</returns>
    /// <response code="200">Webhook processed successfully.</response>
    /// <response code="204">Webhook processed successfully (no content).</response>
    /// <response code="400">Invalid webhook payload.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
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
            return Unauthorized(ApiResponse<object>.Error("Invalid webhook signature"));
        }

        // Deserialize after verification
        var webhookPayload = System.Text.Json.JsonSerializer.Deserialize<PaymentWebhookDto>(
            rawBody,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (webhookPayload == null)
        {
            _logger.LogWarning("Invalid webhook payload received");
            return BadRequest(ApiResponse<string>.Error("Invalid webhook payload"));
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

        return Ok(new { status = "received", message = "Webhook processed successfully" });
    }
}


using System.Collections.Frozen;
using System.Text.Json;
using ECommerce.API.ActionFilters;
using ECommerce.API.Shared.Extensions;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using ECommerce.Payments.Application.Commands.ProcessPayment;
using ECommerce.Payments.Application.Commands.RefundPayment;
using ECommerce.Payments.Application.DTOs;
using ECommerce.Payments.Application.Interfaces;
using ECommerce.Payments.Application.Queries.GetPaymentDetails;
using ECommerce.Payments.Application.Queries.GetPaymentIntent;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Features.Payments.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Payments")]
[Authorize]
public class PaymentsController(
    IMediator mediator,
    ICurrentUserService currentUser,
    IWebhookVerificationService webhookVerificationService,
    ILogger<PaymentsController> logger) : ControllerBase
{
    private const string IdempotencyHeaderName = "Idempotency-Key";
    private static readonly JsonSerializerOptions WebhookJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly FrozenSet<string> NotFoundCodes = FrozenSet.ToFrozenSet(new[]
    {
        "ORDER_NOT_FOUND",
        "NO_PAYMENT_FOUND",
        "PAYMENT_INTENT_NOT_FOUND"
    });

    private static readonly FrozenSet<string> ConflictCodes = FrozenSet.ToFrozenSet(new[]
    {
        "CONCURRENCY_CONFLICT",
        "IDEMPOTENCY_IN_PROGRESS"
    });

    private static readonly FrozenSet<string> ForbiddenCodes = FrozenSet.ToFrozenSet(new[]
    {
        "FORBIDDEN"
    });

    private static readonly FrozenSet<string> UnprocessableCodes = FrozenSet.ToFrozenSet(new[]
    {
        "UNSUPPORTED_PAYMENT_METHOD",
        "PAYMENT_DECLINED"
    });

    [HttpPost("process")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] ProcessPaymentDto dto,
        [FromHeader(Name = IdempotencyHeaderName)] string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(ApiResponse<object>.Failure(
                $"{IdempotencyHeaderName} header is required and must be a valid UUID",
                "INVALID_IDEMPOTENCY_KEY"));
        }

        var result = await mediator.Send(new ProcessPaymentCommand(dto, idempotencyKey), cancellationToken);

        if (result is Result<PaymentResponseDto>.Success s && !s.Data.Success)
            return UnprocessableEntity(ApiResponse<PaymentResponseDto>.Failure(s.Data.Message, "PAYMENT_DECLINED"));

        return result.ToActionResult(
            data => Ok(ApiResponse<PaymentResponseDto>.Ok(data, "Payment processed successfully")),
            MapError);
    }

    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPaymentDetails(Guid orderId, CancellationToken cancellationToken = default)
    {
        var role = currentUser.RoleOrNull;
        var isAdmin = currentUser.IsAuthenticated &&
            (role == Core.Enums.UserRole.Admin || role == Core.Enums.UserRole.SuperAdmin);

        var result = await mediator.Send(
            new GetPaymentDetailsQuery(orderId, currentUser.UserIdOrNull, isAdmin),
            cancellationToken);

        return result.ToActionResult(
            data => Ok(ApiResponse<PaymentDetailsDto>.Ok(data, "Payment details retrieved successfully")),
            MapError);
    }

    [HttpPost("{orderId:guid}/refund")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<RefundResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RefundPayment(
        Guid orderId,
        [FromBody] RefundPaymentDto dto,
        [FromHeader(Name = IdempotencyHeaderName)] string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(ApiResponse<object>.Failure(
                $"{IdempotencyHeaderName} header is required and must be a valid UUID",
                "INVALID_IDEMPOTENCY_KEY"));
        }

        var result = await mediator.Send(new RefundPaymentCommand(orderId, dto, idempotencyKey), cancellationToken);

        return result.ToActionResult(
            data => Ok(ApiResponse<RefundResponseDto>.Ok(data, "Refund processed successfully")),
            MapError);
    }

    [HttpGet("intent/{paymentIntentId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPaymentIntent(string paymentIntentId, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetPaymentIntentQuery(paymentIntentId), cancellationToken);

        return result.ToActionResult(
            data => Ok(ApiResponse<PaymentDetailsDto>.Ok(data, "Payment intent retrieved successfully")),
            MapError);
    }

    [HttpGet("methods")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SupportedPaymentMethodsResponseDto>), StatusCodes.Status200OK)]
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

    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessPaymentWebhook(CancellationToken cancellationToken = default)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        var signature = Request.Headers["X-Webhook-Signature"].FirstOrDefault();
        if (!webhookVerificationService.VerifySignature(rawBody, signature ?? string.Empty))
        {
            logger.LogWarning("Webhook signature verification failed from IP {IP}", HttpContext.Connection.RemoteIpAddress);
            return Unauthorized(ApiResponse<object>.Failure("Invalid webhook signature", "INVALID_WEBHOOK_SIGNATURE"));
        }

        var webhookPayload = JsonSerializer.Deserialize<PaymentWebhookDto>(rawBody, WebhookJsonOptions);
        if (webhookPayload is null)
            return BadRequest(ApiResponse<object>.Failure("Invalid webhook payload", "INVALID_WEBHOOK_PAYLOAD"));

        logger.LogInformation("Verified webhook received for event type: {EventType}", webhookPayload.EventType ?? "unknown");
        return Ok(ApiResponse<object>.Ok(new { status = "received" }, "Webhook processed successfully"));
    }

    private IActionResult MapError(DomainError error)
    {
        if (NotFoundCodes.Contains(error.Code))
            return NotFound(ApiResponse<object>.Failure(error.Message, error.Code));

        if (ForbiddenCodes.Contains(error.Code))
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Failure(error.Message, error.Code));

        if (ConflictCodes.Contains(error.Code))
            return Conflict(ApiResponse<object>.Failure(error.Message, error.Code));

        if (UnprocessableCodes.Contains(error.Code))
            return UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code));

        return BadRequest(ApiResponse<object>.Failure(error.Message, error.Code));
    }
}


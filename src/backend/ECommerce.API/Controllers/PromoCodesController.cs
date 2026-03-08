using ECommerce.API.ActionFilters;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.PromoCodes;
using ECommerce.Application.Interfaces;
using ECommerce.API.Helpers;
using ECommerce.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for managing promotional codes and discounts.
/// </summary>
[ApiController]
[Route("api/promo-codes")]
[Produces("application/json")]
[Tags("PromoCodes")]
public class PromoCodesController : ControllerBase
{
    private readonly IPromoCodeService _promoCodeService;
    private readonly ILogger<PromoCodesController> _logger;

    public PromoCodesController(
        IPromoCodeService promoCodeService,
        ILogger<PromoCodesController> logger)
    {
        _promoCodeService = promoCodeService;
        _logger = logger;
    }

    /// <summary>
    /// Get all active promo codes (Public - for storefront display).
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<PromoCodeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetActiveCodes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);

        _logger.LogInformation("Retrieving active promo codes");

        var result = await _promoCodeService.GetActiveCodesAsync(page, pageSize, cancellationToken: cancellationToken);

        return Ok(ApiResponse<PaginatedResult<PromoCodeDto>>.Ok(result, "Active promo codes retrieved successfully"));
    }

    /// <summary>
    /// Get all promo codes with pagination and filtering (Admin only).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<PromoCodeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllPromoCodes(
        [FromQuery] PromoCodeQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving promo codes (page: {Page}, pageSize: {PageSize}, search: {Search}, isActive: {IsActive})",
            parameters.Page, parameters.PageSize, parameters.Search, parameters.IsActive);

        var result = await _promoCodeService.GetAllAsync(parameters, cancellationToken: cancellationToken);
        return Ok(ApiResponse<PaginatedResult<PromoCodeDto>>.Ok(result, "Promo codes retrieved successfully"));
    }

    /// <summary>
    /// Get promo code by ID (Admin only).
    /// </summary>
    /// <param name="id">The promo code ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PromoCodeDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPromoCodeById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving promo code {Id}", id);

        var promoCode = await _promoCodeService.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (promoCode is Result<PromoCodeDetailDto>.Failure failure)
        {
            return NotFound(ApiResponse<object>.Failure(failure.Message, failure.Code));
        }

        return Ok(ApiResponse<PromoCodeDetailDto>.Ok(((Result<PromoCodeDetailDto>.Success)promoCode).Data, "Promo code retrieved successfully"));
    }

    /// <summary>
    /// Create a new promo code (Admin only).
    /// </summary>
    /// <param name="dto">Promo code creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<PromoCodeDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePromoCode([FromBody] CreatePromoCodeDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating promo code: {Code}", dto.Code);

        var result = await _promoCodeService.CreateAsync(dto, cancellationToken: cancellationToken);
        return result is Result<PromoCodeDetailDto>.Success success
            ? CreatedAtAction(
                nameof(GetPromoCodeById),
                new { id = success.Data.Id },
                ApiResponse<PromoCodeDetailDto>.Ok(success.Data, "Promo code created successfully"))
            : result is Result<PromoCodeDetailDto>.Failure failure
                ? failure.Code switch
                {
                    "DUPLICATE_PROMO_CODE" => Conflict(ApiResponse<object>.Failure(failure.Message, failure.Code)),
                    "CONCURRENCY_CONFLICT" => Conflict(ApiResponse<object>.Failure(failure.Message, failure.Code)),
                    _ => BadRequest(ApiResponse<PromoCodeDetailDto>.Failure(failure.Message, failure.Code))
                }
                : BadRequest(ApiResponse<PromoCodeDetailDto>.Failure("An error occurred", "UNKNOWN_ERROR"));
    }

    /// <summary>
    /// Update an existing promo code (Admin only).
    /// </summary>
    /// <param name="id">The promo code ID.</param>
    /// <param name="dto">Updated promo code details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<PromoCodeDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePromoCode(Guid id, [FromBody] UpdatePromoCodeDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating promo code {Id}", id);

        var result = await _promoCodeService.UpdateAsync(id, dto, cancellationToken: cancellationToken);
        return result is Result<PromoCodeDetailDto>.Success success
            ? Ok(ApiResponse<PromoCodeDetailDto>.Ok(success.Data, "Promo code updated successfully"))
            : result is Result<PromoCodeDetailDto>.Failure failure
                ? failure.Code switch
                {
                    "PROMO_CODE_NOT_FOUND" => NotFound(ApiResponse<object>.Failure(failure.Message, failure.Code)),
                    "DUPLICATE_PROMO_CODE" => Conflict(ApiResponse<object>.Failure(failure.Message, failure.Code)),
                    "CONCURRENCY_CONFLICT" => Conflict(ApiResponse<object>.Failure(failure.Message, failure.Code)),
                    _ => BadRequest(ApiResponse<PromoCodeDetailDto>.Failure(failure.Message, failure.Code))
                }
                : BadRequest(ApiResponse<PromoCodeDetailDto>.Failure("An error occurred", "UNKNOWN_ERROR"));
    }

    /// <summary>
    /// Deactivate a promo code (Admin only) - Soft delete.
    /// </summary>
    /// <param name="id">The promo code ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{id}/deactivate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeactivatePromoCode(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivating promo code {Id}", id);

        var result = await _promoCodeService.DeactivateAsync(id, cancellationToken: cancellationToken);
        if (result is Result<Unit>.Failure failure)
        {
            var statusCode = failure.Code switch
            {
                "PROMO_CODE_NOT_FOUND" => StatusCodes.Status404NotFound,
                "CONCURRENCY_CONFLICT" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            return StatusCode(statusCode, ApiResponse<object>.Failure(failure.Message, failure.Code));
        }

        return Ok(ApiResponse<object>.Ok(new object(), "Promo code deactivated successfully"));
    }

    /// <summary>
    /// Delete a promo code (Admin only) - Hard delete.
    /// </summary>
    /// <param name="id">The promo code ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeletePromoCode(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting promo code {Id}", id);

        var result = await _promoCodeService.DeleteAsync(id, cancellationToken: cancellationToken);
        if (result is Result<Unit>.Failure failure)
        {
            var statusCode = failure.Code switch
            {
                "PROMO_CODE_NOT_FOUND" => StatusCodes.Status404NotFound,
                "CONCURRENCY_CONFLICT" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            return StatusCode(statusCode, ApiResponse<object>.Failure(failure.Message, failure.Code));
        }

        return Ok(ApiResponse<object>.Ok(new object(), "Promo code deleted successfully"));
    }

    /// <summary>
    /// Validate a promo code for an order (Public - supports guest checkout).
    /// </summary>
    /// <param name="request">Validation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("validate")]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ValidatePromoCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidatePromoCode([FromBody] ValidatePromoCodeRequestDto request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating promo code: {Code}", request.Code);

        var result = await _promoCodeService.ValidatePromoCodeAsync(request.Code, request.OrderAmount, cancellationToken: cancellationToken);
        return Ok(ApiResponse<ValidatePromoCodeDto>.Ok(result, "Promo code validation completed"));
    }
}


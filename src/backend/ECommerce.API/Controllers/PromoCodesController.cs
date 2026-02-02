using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.PromoCodes;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for managing promotional codes and discounts.
/// </summary>
[ApiController]
[Route("api/promo-codes")]
[Produces("application/json")]
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
    /// Get all promo codes with pagination and filtering (Admin only).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<PromoCodeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllPromoCodes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving promo codes (page: {Page}, pageSize: {PageSize}, search: {Search}, isActive: {IsActive})",
            page, pageSize, search, isActive);

        var result = await _promoCodeService.GetAllAsync(page, pageSize, search, isActive, cancellationToken: cancellationToken);
        return Ok(ApiResponse<PaginatedResult<PromoCodeDto>>.Ok(result, "Promo codes retrieved successfully"));
    }

    /// <summary>
    /// Get promo code by ID (Admin only).
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PromoCodeDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPromoCodeById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving promo code {Id}", id);

        var promoCode = await _promoCodeService.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (promoCode == null)
        {
            return NotFound(ApiResponse<string>.Error("Promo code not found"));
        }

        return Ok(ApiResponse<PromoCodeDetailDto>.Ok(promoCode, "Promo code retrieved successfully"));
    }

    /// <summary>
    /// Create a new promo code (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PromoCodeDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePromoCode([FromBody] CreatePromoCodeDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating promo code: {Code}", dto.Code);

        var promoCode = await _promoCodeService.CreateAsync(dto, cancellationToken: cancellationToken);
        return CreatedAtAction(
            nameof(GetPromoCodeById),
            new { id = promoCode.Id },
            ApiResponse<PromoCodeDetailDto>.Ok(promoCode, "Promo code created successfully"));
    }

    /// <summary>
    /// Update an existing promo code (Admin only).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PromoCodeDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePromoCode(Guid id, [FromBody] UpdatePromoCodeDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating promo code {Id}", id);

        var promoCode = await _promoCodeService.UpdateAsync(id, dto, cancellationToken: cancellationToken);
        return Ok(ApiResponse<PromoCodeDetailDto>.Ok(promoCode, "Promo code updated successfully"));
    }

    /// <summary>
    /// Deactivate a promo code (Admin only) - Soft delete.
    /// </summary>
    [HttpPut("{id}/deactivate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeactivatePromoCode(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivating promo code {Id}", id);

        await _promoCodeService.DeactivateAsync(id, cancellationToken: cancellationToken);
        return Ok(ApiResponse<object>.Ok(new object(), "Promo code deactivated successfully"));
    }

    /// <summary>
    /// Validate a promo code for an order (Public - supports guest checkout).
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ValidatePromoCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidatePromoCode([FromBody] ValidatePromoCodeRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating promo code: {Code}", request.Code);

        var result = await _promoCodeService.ValidatePromoCodeAsync(request.Code, request.OrderAmount, cancellationToken: cancellationToken);
        return Ok(ApiResponse<ValidatePromoCodeDto>.Ok(result, "Promo code validation completed"));
    }
}

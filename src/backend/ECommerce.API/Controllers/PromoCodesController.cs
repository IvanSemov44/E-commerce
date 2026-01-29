using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.PromoCodes;
using ECommerce.Application.Services;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

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
    /// Get all promo codes with pagination and filtering (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<PromoCodeDto>>), 200)]
    public async Task<IActionResult> GetAllPromoCodes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var result = await _promoCodeService.GetAllAsync(page, pageSize, search, isActive);
            return Ok(ApiResponse<PaginatedResult<PromoCodeDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving promo codes");
            return StatusCode(500, ApiResponse<object>.Error("An error occurred while retrieving promo codes"));
        }
    }

    /// <summary>
    /// Get promo code by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PromoCodeDetailDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetPromoCodeById(Guid id)
    {
        try
        {
            var promoCode = await _promoCodeService.GetByIdAsync(id);
            if (promoCode == null)
            {
                return NotFound(ApiResponse<object>.Error("Promo code not found"));
            }

            return Ok(ApiResponse<PromoCodeDetailDto>.Ok(promoCode));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving promo code {Id}", id);
            return StatusCode(500, ApiResponse<object>.Error("An error occurred while retrieving the promo code"));
        }
    }

    /// <summary>
    /// Create a new promo code (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PromoCodeDetailDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CreatePromoCode([FromBody] CreatePromoCodeDto dto)
    {
        try
        {
            var promoCode = await _promoCodeService.CreateAsync(dto);
            return CreatedAtAction(
                nameof(GetPromoCodeById),
                new { id = promoCode.Id },
                ApiResponse<PromoCodeDetailDto>.Ok(promoCode, "Promo code created successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid promo code data");
            return BadRequest(ApiResponse<object>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating promo code");
            return StatusCode(500, ApiResponse<object>.Error("An error occurred while creating the promo code"));
        }
    }

    /// <summary>
    /// Update an existing promo code (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PromoCodeDetailDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> UpdatePromoCode(Guid id, [FromBody] UpdatePromoCodeDto dto)
    {
        try
        {
            var promoCode = await _promoCodeService.UpdateAsync(id, dto);
            return Ok(ApiResponse<PromoCodeDetailDto>.Ok(promoCode, "Promo code updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Error(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid promo code data");
            return BadRequest(ApiResponse<object>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating promo code {Id}", id);
            return StatusCode(500, ApiResponse<object>.Error("An error occurred while updating the promo code"));
        }
    }

    /// <summary>
    /// Deactivate a promo code (Admin only) - Soft delete
    /// </summary>
    [HttpPut("{id}/deactivate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeactivatePromoCode(Guid id)
    {
        try
        {
            var result = await _promoCodeService.DeactivateAsync(id);
            if (!result)
            {
                return NotFound(ApiResponse<object>.Error("Promo code not found"));
            }

            return Ok(ApiResponse<object>.Ok(null, "Promo code deactivated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating promo code {Id}", id);
            return StatusCode(500, ApiResponse<object>.Error("An error occurred while deactivating the promo code"));
        }
    }

    /// <summary>
    /// Validate a promo code for an order (Public - supports guest checkout)
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ValidatePromoCodeDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> ValidatePromoCode([FromBody] ValidatePromoCodeRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest(ApiResponse<object>.Error("Promo code is required"));
            }

            if (request.OrderAmount <= 0)
            {
                return BadRequest(ApiResponse<object>.Error("Order amount must be greater than zero"));
            }

            var result = await _promoCodeService.ValidatePromoCodeAsync(request.Code, request.OrderAmount);
            return Ok(ApiResponse<ValidatePromoCodeDto>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating promo code {Code}", request.Code);
            return StatusCode(500, ApiResponse<object>.Error("An error occurred while validating the promo code"));
        }
    }
}

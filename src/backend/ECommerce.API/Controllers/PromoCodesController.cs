using ECommerce.API.ActionFilters;
using ECommerce.API.Helpers;
using ECommerce.Application.DTOs.Common;
using ECommerce.Core.Results;
using ECommerce.Promotions.Application.Commands.CreatePromoCode;
using ECommerce.Promotions.Application.Commands.DeactivatePromoCode;
using ECommerce.Promotions.Application.Commands.UpdatePromoCode;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.Queries.GetPromoCode;
using ECommerce.Promotions.Application.Queries.GetPromoCodes;
using ECommerce.Promotions.Application.Queries.ValidatePromoCode;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OldCreatePromoCodeDto = ECommerce.Application.DTOs.PromoCodes.CreatePromoCodeDto;
using OldUpdatePromoCodeDto = ECommerce.Application.DTOs.PromoCodes.UpdatePromoCodeDto;
using OldValidatePromoCodeRequestDto = ECommerce.Application.DTOs.PromoCodes.ValidatePromoCodeRequestDto;
using OldPromoCodeDto = ECommerce.Application.DTOs.PromoCodes.PromoCodeDto;
using OldValidatePromoCodeDto = ECommerce.Application.DTOs.PromoCodes.ValidatePromoCodeDto;
using OldPromoCodeQueryParameters = ECommerce.Application.DTOs.PromoCodes.PromoCodeQueryParameters;
using OldPromoCodeDetailDto = ECommerce.Application.DTOs.PromoCodes.PromoCodeDetailDto;

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
    private readonly IMediator _mediator;
    private readonly ILogger<PromoCodesController> _logger;

    public PromoCodesController(
        IMediator mediator,
        ILogger<PromoCodesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all active promo codes (Public - for storefront display).
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ECommerce.Application.DTOs.Common.PaginatedResult<OldPromoCodeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetActiveCodes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);

        _logger.LogInformation("Retrieving active promo codes");

        var query = new GetPromoCodesQuery(Page: page, PageSize: pageSize, IsActive: true);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            var error = result.GetErrorOrThrow();
            return BadRequest(ApiResponse<object>.Failure(error.Message, error.Code));
        }

        var data = result.GetDataOrThrow();
        var paginated = new ECommerce.Application.DTOs.Common.PaginatedResult<OldPromoCodeDto>
        {
            Items = data.Items.Select(MapToAppDto).ToList(),
            TotalCount = data.TotalCount,
            Page = data.Page,
            PageSize = data.PageSize
        };

        return Ok(ApiResponse<ECommerce.Application.DTOs.Common.PaginatedResult<OldPromoCodeDto>>.Ok(paginated, "Active promo codes retrieved successfully"));
    }

    /// <summary>
    /// Get all promo codes with pagination and filtering (Admin only).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ECommerce.Application.DTOs.Common.PaginatedResult<OldPromoCodeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllPromoCodes(
        [FromQuery] OldPromoCodeQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving promo codes (page: {Page}, pageSize: {PageSize}, search: {Search}, isActive: {IsActive})",
            parameters.Page, parameters.PageSize, parameters.Search, parameters.IsActive);

        var query = new GetPromoCodesQuery(
            Page: parameters.Page,
            PageSize: parameters.PageSize,
            Search: parameters.Search,
            IsActive: parameters.IsActive);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            var error = result.GetErrorOrThrow();
            return BadRequest(ApiResponse<object>.Failure(error.Message, error.Code));
        }

        var data = result.GetDataOrThrow();
        var paginated = new ECommerce.Application.DTOs.Common.PaginatedResult<OldPromoCodeDto>
        {
            Items = data.Items.Select(MapToAppDto).ToList(),
            TotalCount = data.TotalCount,
            Page = data.Page,
            PageSize = data.PageSize
        };

        return Ok(ApiResponse<ECommerce.Application.DTOs.Common.PaginatedResult<OldPromoCodeDto>>.Ok(paginated, "Promo codes retrieved successfully"));
    }

    /// <summary>
    /// Get promo code by ID (Admin only).
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<OldPromoCodeDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPromoCodeById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving promo code {Id}", id);

        var query = new GetPromoCodeQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            var error = result.GetErrorOrThrow();
            return NotFound(ApiResponse<object>.Failure(error.Message, error.Code));
        }

        var dto = result.GetDataOrThrow();
        return Ok(ApiResponse<OldPromoCodeDetailDto>.Ok(MapToAppDetailDto(dto), "Promo code retrieved successfully"));
    }

    /// <summary>
    /// Create a new promo code (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<OldPromoCodeDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePromoCode([FromBody] OldCreatePromoCodeDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating promo code: {Code}", dto.Code);

        var command = new CreatePromoCodeCommand(
            Code: dto.Code,
            DiscountType: dto.DiscountType,
            DiscountValue: dto.DiscountValue,
            ValidFrom: dto.StartDate,
            ValidUntil: dto.EndDate,
            MaxUses: dto.MaxUses,
            MinimumOrderAmount: dto.MinOrderAmount,
            MaxDiscountAmount: dto.MaxDiscountAmount);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var error = result.GetErrorOrThrow();
            return error.Code switch
            {
                "DUPLICATE_PROMO_CODE" => Conflict(ApiResponse<object>.Failure(error.Message, error.Code)),
                "CONCURRENCY_CONFLICT" => Conflict(ApiResponse<object>.Failure(error.Message, error.Code)),
                _ => BadRequest(ApiResponse<OldPromoCodeDetailDto>.Failure(error.Message, error.Code))
            };
        }

        var createdDto = result.GetDataOrThrow();
        return CreatedAtAction(
            nameof(GetPromoCodeById),
            new { id = createdDto.Id },
            ApiResponse<OldPromoCodeDetailDto>.Ok(MapToAppDetailDto(createdDto), "Promo code created successfully"));
    }

    /// <summary>
    /// Update an existing promo code (Admin only).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<OldPromoCodeDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePromoCode(Guid id, [FromBody] OldUpdatePromoCodeDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating promo code {Id}", id);

        var command = new UpdatePromoCodeCommand(
            Id: id,
            IsActive: dto.IsActive,
            DiscountType: dto.DiscountType,
            DiscountValue: dto.DiscountValue,
            ValidFrom: dto.StartDate,
            ValidUntil: dto.EndDate,
            MaxUses: dto.MaxUses,
            MinimumOrderAmount: dto.MinOrderAmount,
            MaxDiscountAmount: dto.MaxDiscountAmount);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var error = result.GetErrorOrThrow();
            return error.Code switch
            {
                "PROMO_CODE_NOT_FOUND" => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),
                "DUPLICATE_PROMO_CODE" => Conflict(ApiResponse<object>.Failure(error.Message, error.Code)),
                "CONCURRENCY_CONFLICT" => Conflict(ApiResponse<object>.Failure(error.Message, error.Code)),
                _ => BadRequest(ApiResponse<OldPromoCodeDetailDto>.Failure(error.Message, error.Code))
            };
        }

        var updatedDto = result.GetDataOrThrow();
        return Ok(ApiResponse<OldPromoCodeDetailDto>.Ok(MapToAppDetailDto(updatedDto), "Promo code updated successfully"));
    }

    /// <summary>
    /// Deactivate a promo code (Admin only) - Soft delete.
    /// </summary>
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

        var command = new DeactivatePromoCodeCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var error = result.GetErrorOrThrow();
            var statusCode = error.Code switch
            {
                "PROMO_CODE_NOT_FOUND" => StatusCodes.Status404NotFound,
                "CONCURRENCY_CONFLICT" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            return StatusCode(statusCode, ApiResponse<object>.Failure(error.Message, error.Code));
        }

        return Ok(ApiResponse<object>.Ok(new object(), "Promo code deactivated successfully"));
    }

    /// <summary>
    /// Validate a promo code for an order (Public - supports guest checkout).
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<OldValidatePromoCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidatePromoCode([FromBody] OldValidatePromoCodeRequestDto request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating promo code: {Code}", request.Code);

        var query = new ValidatePromoCodeQuery(request.Code, request.OrderAmount);

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            var error = result.GetErrorOrThrow();
            return BadRequest(ApiResponse<object>.Failure(error.Message, error.Code));
        }

        var validationDto = result.GetDataOrThrow();
        var responseDto = new OldValidatePromoCodeDto
        {
            IsValid = validationDto.IsValid,
            DiscountAmount = validationDto.DiscountAmount,
            Message = validationDto.Message
        };

        return Ok(ApiResponse<OldValidatePromoCodeDto>.Ok(responseDto, "Promo code validation completed"));
    }

    private static OldPromoCodeDto MapToAppDto(PromoCodeListItemDto dto)
    {
        return new OldPromoCodeDto
        {
            Id = dto.Id,
            Code = dto.Code,
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            IsActive = dto.IsActive,
            UsedCount = dto.UsedCount,
            MaxUses = dto.MaxUses
        };
    }

    private static OldPromoCodeDetailDto MapToAppDetailDto(PromoCodeDto dto)
    {
        return new OldPromoCodeDetailDto
        {
            Id = dto.Id,
            Code = dto.Code,
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            StartDate = dto.ValidFrom,
            EndDate = dto.ValidUntil,
            MaxUses = dto.MaxUses,
            UsedCount = dto.UsedCount,
            IsActive = dto.IsActive,
            MinOrderAmount = dto.MinimumOrderAmount,
            MaxDiscountAmount = dto.MaxDiscountAmount,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }
}

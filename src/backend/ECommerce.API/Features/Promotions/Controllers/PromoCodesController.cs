using System.Collections.Frozen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ECommerce.API.ActionFilters;
using ECommerce.API.Extensions;
using ECommerce.API.Helpers;
using ECommerce.Application.DTOs.Common;
using ECommerce.Promotions.Application.Commands.CreatePromoCode;
using ECommerce.Promotions.Application.Commands.DeactivatePromoCode;
using ECommerce.Promotions.Application.Commands.DeletePromoCode;
using ECommerce.Promotions.Application.Commands.UpdatePromoCode;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.Queries.GetActivePromoCodes;
using ECommerce.Promotions.Application.Queries.GetPromoCodeById;
using ECommerce.Promotions.Application.Queries.GetPromoCodes;
using ECommerce.Promotions.Application.Queries.ValidatePromoCode;
using ECommerce.SharedKernel.Results;

namespace ECommerce.API.Features.Promotions.Controllers;

[ApiController]
[Route("api/promo-codes")]
[Produces("application/json")]
[Tags("PromoCodes")]
public class PromoCodesController(IMediator mediator) : ControllerBase
{
    private static readonly FrozenSet<string> _notFound = FrozenSet.Create("PROMO_CODE_NOT_FOUND");
    private static readonly FrozenSet<string> _conflict = FrozenSet.Create("DUPLICATE_PROMO_CODE", "CONCURRENCY_CONFLICT");
    private static readonly FrozenSet<string> _unprocessable = FrozenSet.Create("PROMO_NOT_VALID", "PROMO_MIN_ORDER");

    private IActionResult MapError(DomainError error)
    {
        var body = ApiResponse<object>.Failure(error.Message, error.Code);
        if (_notFound.Contains(error.Code))       return NotFound(body);
        if (_conflict.Contains(error.Code))       return Conflict(body);
        if (_unprocessable.Contains(error.Code))  return UnprocessableEntity(body);
        return BadRequest(body);
    }

    /// <summary>Get all active promo codes (Public — for storefront display).</summary>
    [HttpGet("active")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<PromoCodeListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetActiveCodes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);
        var result = await mediator.Send(new GetActivePromoCodesQuery(page, pageSize), ct);
        return result.ToActionResult(
            paginatedList => Ok(ApiResponse<PaginatedResult<PromoCodeListItemDto>>.Ok(
                MapToPaginatedResult(paginatedList), "Active promo codes retrieved successfully")),
            MapError);
    }

    /// <summary>Get all promo codes with pagination and filtering (Admin only).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<PromoCodeListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllPromoCodes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        (page, pageSize) = PaginationRequestNormalizer.Normalize(page, pageSize);
        var result = await mediator.Send(new GetPromoCodesQuery(page, pageSize, search, isActive), ct);
        return result.ToActionResult(
            paginatedList => Ok(ApiResponse<PaginatedResult<PromoCodeListItemDto>>.Ok(
                MapToPaginatedResult(paginatedList), "Promo codes retrieved successfully")),
            MapError);
    }

    /// <summary>Get promo code by ID (Admin only).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PromoCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPromoCodeById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPromoCodeByIdQuery(id), ct);
        return result.ToActionResult(
            dto => Ok(ApiResponse<PromoCodeDto>.Ok(dto, "Promo code retrieved successfully")),
            MapError);
    }

    /// <summary>Create a new promo code (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<PromoCodeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePromoCode(
        [FromBody] CreatePromoCodeRequestDto dto,
        CancellationToken ct)
    {
        var cmd = new CreatePromoCodeCommand(
            dto.Code,
            dto.DiscountType,
            dto.DiscountValue,
            dto.StartDate,
            dto.EndDate,
            dto.MaxUses,
            dto.MinOrderAmount,
            dto.MaxDiscountAmount);

        var result = await mediator.Send(cmd, ct);
        return result.ToActionResult(
            createdDto => CreatedAtAction(
                nameof(GetPromoCodeById),
                new { id = createdDto.Id },
                ApiResponse<PromoCodeDto>.Ok(createdDto, "Promo code created successfully")),
            MapError);
    }

    /// <summary>Update an existing promo code (Admin only).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<PromoCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePromoCode(
        Guid id,
        [FromBody] UpdatePromoCodeRequestDto dto,
        CancellationToken ct)
    {
        var cmd = new UpdatePromoCodeCommand(
            id,
            dto.IsActive,
            dto.DiscountType,
            dto.DiscountValue,
            dto.StartDate,
            dto.EndDate,
            dto.MaxUses,
            dto.MinOrderAmount,
            dto.MaxDiscountAmount);

        var result = await mediator.Send(cmd, ct);
        return result.ToActionResult(
            updatedDto => Ok(ApiResponse<PromoCodeDto>.Ok(updatedDto, "Promo code updated successfully")),
            MapError);
    }

    /// <summary>Deactivate a promo code (Admin only) — soft-delete.</summary>
    [HttpPut("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeactivatePromoCode(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeactivatePromoCodeCommand(id), ct);
        return result.ToActionResult(
            () => Ok(ApiResponse<object>.Ok(new object(), "Promo code deactivated successfully")),
            MapError);
    }

    /// <summary>Delete a promo code (Admin only) — hard delete.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePromoCode(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeletePromoCodeCommand(id), ct);
        return result.ToActionResult(
            () => Ok(ApiResponse<object>.Ok(new object(), "Promo code deleted successfully")),
            MapError);
    }

    /// <summary>Validate a promo code for an order (Public — supports guest checkout).</summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ValidatePromoCodeResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidatePromoCode(
        [FromBody] ValidatePromoCodeRequestDto request,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ValidatePromoCodeQuery(request.Code, request.OrderAmount), ct);
        return result.ToActionResult(
            dto => Ok(ApiResponse<ValidatePromoCodeResultDto>.Ok(dto, "Promo code validation completed")),
            MapError);
    }

    private static PaginatedResult<PromoCodeListItemDto> MapToPaginatedResult(
        ECommerce.Promotions.Application.DTOs.Common.PaginatedList<PromoCodeListItemDto> paginatedList)
    {
        return new PaginatedResult<PromoCodeListItemDto>
        {
            Items = paginatedList.Items.ToList(),
            TotalCount = paginatedList.TotalCount,
            Page = paginatedList.Page,
            PageSize = paginatedList.PageSize
        };
    }
}

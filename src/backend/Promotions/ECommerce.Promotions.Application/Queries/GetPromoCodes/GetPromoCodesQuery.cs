using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.DTOs.Common;

namespace ECommerce.Promotions.Application.Queries.GetPromoCodes;

public record GetPromoCodesQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null
) : IRequest<Result<PaginatedList<PromoCodeListItemDto>>>;
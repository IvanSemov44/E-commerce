using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.DTOs.Common;

namespace ECommerce.Promotions.Application.Queries.GetActivePromoCodes;

public record GetActivePromoCodesQuery(
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedList<PromoCodeListItemDto>>>;

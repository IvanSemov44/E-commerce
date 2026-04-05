using ECommerce.Reviews.Application.DTOs;
using ECommerce.Reviews.Application.DTOs.Common;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Reviews.Application.QueryHandlers;

public class GetUserReviewsQueryHandler(
    IReviewRepository reviewRepository) : IRequestHandler<GetUserReviewsQuery, Result<PaginatedResult<ReviewDetailDto>>>
{
    public async Task<Result<PaginatedResult<ReviewDetailDto>>> Handle(GetUserReviewsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await reviewRepository.GetByUserAsync(request.UserId, request.Page, request.PageSize, cancellationToken);

        return Result<PaginatedResult<ReviewDetailDto>>.Ok(new PaginatedResult<ReviewDetailDto>
        {
            Items = items.Select(review => review.ToDetailDto()).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}
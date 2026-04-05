using ECommerce.Reviews.Application.DTOs;
using ECommerce.Reviews.Application.DTOs.Common;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Reviews.Application.QueryHandlers;

public class GetFlaggedReviewsQueryHandler(
    IReviewRepository reviewRepository) : IRequestHandler<GetFlaggedReviewsQuery, Result<PaginatedResult<ReviewDto>>>
{
    public async Task<Result<PaginatedResult<ReviewDto>>> Handle(GetFlaggedReviewsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await reviewRepository.GetFlaggedAsync(request.Page, request.PageSize, cancellationToken);

        return Result<PaginatedResult<ReviewDto>>.Ok(new PaginatedResult<ReviewDto>
        {
            Items = items.Select(review => review.ToDto()).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}
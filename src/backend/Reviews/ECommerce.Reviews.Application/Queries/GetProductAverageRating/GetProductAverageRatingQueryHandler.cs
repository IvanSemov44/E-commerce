using ECommerce.Reviews.Application.Interfaces;
using ECommerce.Reviews.Domain.Errors;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Reviews.Application.QueryHandlers;

public class GetProductAverageRatingQueryHandler(
    IReviewRepository reviewRepository,
    ICatalogService catalogService) : IRequestHandler<GetProductAverageRatingQuery, Result<decimal>>
{
    public async Task<Result<decimal>> Handle(GetProductAverageRatingQuery request, CancellationToken cancellationToken)
    {
        if (!await catalogService.ProductExistsAsync(request.ProductId, cancellationToken))
            return Result<decimal>.Fail(ReviewsErrors.ProductNotFound);

        decimal averageRating = await reviewRepository.GetAverageRatingAsync(request.ProductId, cancellationToken);
        return Result<decimal>.Ok(averageRating);
    }
}
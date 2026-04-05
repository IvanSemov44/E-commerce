using ECommerce.Reviews.Application.DTOs;
using ECommerce.Reviews.Application.DTOs.Common;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Queries;

public record GetProductReviewsQuery(
    Guid ProductId,
    int Page,
    int PageSize) : IRequest<Result<PaginatedResult<ReviewDto>>>;
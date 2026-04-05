using ECommerce.Reviews.Application.DTOs;
using ECommerce.Reviews.Application.DTOs.Common;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Queries;

public record GetUserReviewsQuery(
    Guid UserId,
    int Page,
    int PageSize) : IRequest<Result<PaginatedResult<ReviewDetailDto>>>;
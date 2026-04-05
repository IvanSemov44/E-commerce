using ECommerce.Reviews.Application.DTOs;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Queries;

public record GetReviewByIdQuery(Guid ReviewId) : IRequest<Result<ReviewDetailDto>>;
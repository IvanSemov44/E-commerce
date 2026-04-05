using ECommerce.Reviews.Application.DTOs;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Commands;

public record UpdateReviewCommand(
    Guid ReviewId,
    Guid UserId,
    bool IsAdmin,
    int? Rating,
    string? Title,
    string? Comment) : IRequest<Result<ReviewDetailDto>>, ITransactionalCommand;

using ECommerce.Reviews.Application.DTOs;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Commands;

public record CreateReviewCommand(
    Guid ProductId,
    Guid UserId,
    Guid? OrderId,
    int Rating,
    string? Title,
    string Comment) : IRequest<Result<ReviewDetailDto>>, ITransactionalCommand;
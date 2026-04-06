using ECommerce.SharedKernel.Results;

namespace ECommerce.Infrastructure.Integration;

public interface IDeadLetterReplayService
{
    Task<Result<DeadLetterPageDto>> GetDeadLettersAsync(
        int page,
        int pageSize,
        bool includeRequeued,
        CancellationToken cancellationToken = default);

    Task<Result<DeadLetterMessageDto>> RequeueAsync(
        Guid deadLetterId,
        CancellationToken cancellationToken = default);
}

public sealed record DeadLetterMessageDto(
    Guid Id,
    Guid OutboxMessageId,
    Guid IdempotencyKey,
    string EventType,
    int RetryCount,
    string? LastError,
    DateTime FailedAt,
    DateTime? RequeuedAt);

public sealed record DeadLetterPageDto(
    IReadOnlyList<DeadLetterMessageDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

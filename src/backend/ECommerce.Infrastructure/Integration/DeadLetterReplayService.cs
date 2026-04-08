using System.Text.Json;
using ECommerce.SharedKernel.Constants;
using ECommerce.Contracts;
using ECommerce.Infrastructure.Data;
using ECommerce.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Integration;

public sealed class DeadLetterReplayService(AppDbContext dbContext) : IDeadLetterReplayService
{
    public async Task<Result<DeadLetterPageDto>> GetDeadLettersAsync(
        int page,
        int pageSize,
        bool includeRequeued,
        CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 200);

        var query = dbContext.DeadLetterMessages.AsNoTracking();
        if (!includeRequeued)
            query = query.Where(x => x.RequeuedAt == null);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.FailedAt)
            .ThenByDescending(x => x.Id)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(x => new DeadLetterMessageDto(
                x.Id,
                x.OutboxMessageId,
                x.IdempotencyKey,
                x.EventType,
                x.RetryCount,
                x.LastError,
                x.FailedAt,
                x.RequeuedAt))
            .ToListAsync(cancellationToken);

        return Result<DeadLetterPageDto>.Ok(new DeadLetterPageDto(items, totalCount, safePage, safePageSize));
    }

    public async Task<Result<DeadLetterMessageDto>> RequeueAsync(
        Guid deadLetterId,
        CancellationToken cancellationToken = default)
    {
        var deadLetterMessage = await dbContext.DeadLetterMessages
            .FirstOrDefaultAsync(x => x.Id == deadLetterId, cancellationToken);

        if (deadLetterMessage is null)
        {
            return Result<DeadLetterMessageDto>.Fail(new DomainError(
                ErrorCodes.DeadLetterMessageNotFound,
                $"Dead-letter message '{deadLetterId}' was not found."));
        }

        if (deadLetterMessage.RequeuedAt is not null)
        {
            return Result<DeadLetterMessageDto>.Fail(new DomainError(
                ErrorCodes.DeadLetterAlreadyRequeued,
                $"Dead-letter message '{deadLetterId}' has already been requeued."));
        }

        var eventType = Type.GetType(deadLetterMessage.EventType, throwOnError: false);
        if (eventType is null)
        {
            return Result<DeadLetterMessageDto>.Fail(new DomainError(
                ErrorCodes.InvalidIntegrationEventPayload,
                $"Cannot resolve integration event type '{deadLetterMessage.EventType}'."));
        }

        if (JsonSerializer.Deserialize(deadLetterMessage.EventData, eventType) is not IntegrationEvent deserialized)
        {
            return Result<DeadLetterMessageDto>.Fail(new DomainError(
                ErrorCodes.InvalidIntegrationEventPayload,
                $"Cannot deserialize integration event payload for type '{deadLetterMessage.EventType}'."));
        }

        var now = DateTime.UtcNow;
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = deserialized.IdempotencyKey,
            EventType = deadLetterMessage.EventType,
            EventData = deadLetterMessage.EventData,
            CreatedAt = now,
            RetryCount = 0,
            LastError = null,
            IsDeadLettered = false,
            DeadLetteredAt = null,
            NextAttemptAt = null,
            ProcessedAt = null
        });

        deadLetterMessage.RequeuedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<DeadLetterMessageDto>.Ok(new DeadLetterMessageDto(
            deadLetterMessage.Id,
            deadLetterMessage.OutboxMessageId,
            deadLetterMessage.IdempotencyKey,
            deadLetterMessage.EventType,
            deadLetterMessage.RetryCount,
            deadLetterMessage.LastError,
            deadLetterMessage.FailedAt,
            deadLetterMessage.RequeuedAt));
    }
}

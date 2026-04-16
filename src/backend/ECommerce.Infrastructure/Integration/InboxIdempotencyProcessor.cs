using ECommerce.Contracts;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Integration;

/// <summary>
/// Executes integration event handling with inbox-based deduplication.
/// </summary>
public sealed class InboxIdempotencyProcessor(IntegrationPersistenceDbContext dbContext)
{
    public async Task ExecuteAsync<TEvent>(
        TEvent integrationEvent,
        Func<CancellationToken, Task> handleAsync,
        CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        var existing = await dbContext.InboxMessages
            .SingleOrDefaultAsync(x => x.IdempotencyKey == integrationEvent.IdempotencyKey, cancellationToken);

        if (existing?.ProcessedAt is not null)
            return;

        if (existing is null)
        {
            existing = new InboxMessage
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = integrationEvent.IdempotencyKey,
                EventType = typeof(TEvent).FullName ?? typeof(TEvent).Name,
                ReceivedAt = DateTime.UtcNow
            };

            dbContext.InboxMessages.Add(existing);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                existing = await dbContext.InboxMessages
                    .SingleAsync(x => x.IdempotencyKey == integrationEvent.IdempotencyKey, cancellationToken);

                if (existing.ProcessedAt is not null)
                    return;
            }
        }

        try
        {
            await handleAsync(cancellationToken);

            existing.AttemptCount += 1;
            existing.ProcessedAt = DateTime.UtcNow;
            existing.LastError = null;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            existing.AttemptCount += 1;
            var messageText = exception.Message;
            existing.LastError = messageText.Length > 2000 ? messageText[..2000] : messageText;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}

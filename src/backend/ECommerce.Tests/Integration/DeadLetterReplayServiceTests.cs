using System.Text.Json;
using ECommerce.Contracts;
using ECommerce.Infrastructure.Data;
using ECommerce.SharedKernel.Constants;
using ECommerce.Infrastructure.Integration;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Tests.Integration;

[TestClass]
public class DeadLetterReplayServiceTests
{
    [TestMethod]
    public async Task RequeueAsync_WhenMessageExists_EnqueuesOutboxAndMarksRequeued()
    {
        var options = new DbContextOptionsBuilder<IntegrationPersistenceDbContext>()
            .UseInMemoryDatabase($"dead-letter-requeue-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new IntegrationPersistenceDbContext(options);
        var service = new DeadLetterReplayService(dbContext);

        var integrationEvent = new ProductProjectionUpdatedIntegrationEvent(
            Guid.NewGuid(),
            "Replay Product",
            10.5m,
            false,
            DateTime.UtcNow);

        var deadLetter = CreateDeadLetterMessage(integrationEvent);
        dbContext.DeadLetterMessages.Add(deadLetter);
        await dbContext.SaveChangesAsync();

        var result = await service.RequeueAsync(deadLetter.Id, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        var data = result.GetDataOrThrow();
        Assert.IsNotNull(data.RequeuedAt);

        var persistedDeadLetter = await dbContext.DeadLetterMessages.SingleAsync(x => x.Id == deadLetter.Id);
        Assert.IsNotNull(persistedDeadLetter.RequeuedAt);

        var outboxMessage = await dbContext.OutboxMessages.SingleAsync(x => x.IdempotencyKey == integrationEvent.IdempotencyKey);
        Assert.AreEqual(deadLetter.EventType, outboxMessage.EventType);
        Assert.AreEqual(deadLetter.EventData, outboxMessage.EventData);
        Assert.AreEqual(0, outboxMessage.RetryCount);
    }

    [TestMethod]
    public async Task RequeueAsync_WhenAlreadyRequeued_ReturnsConflictError()
    {
        var options = new DbContextOptionsBuilder<IntegrationPersistenceDbContext>()
            .UseInMemoryDatabase($"dead-letter-requeue-conflict-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new IntegrationPersistenceDbContext(options);
        var service = new DeadLetterReplayService(dbContext);

        var integrationEvent = new ProductProjectionUpdatedIntegrationEvent(
            Guid.NewGuid(),
            "Replay Product",
            12m,
            true,
            DateTime.UtcNow);

        var deadLetter = CreateDeadLetterMessage(integrationEvent);
        deadLetter.RequeuedAt = DateTime.UtcNow;
        dbContext.DeadLetterMessages.Add(deadLetter);
        await dbContext.SaveChangesAsync();

        var result = await service.RequeueAsync(deadLetter.Id, CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorCodes.DeadLetterAlreadyRequeued, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task RequeueAsync_WhenMessageMissing_ReturnsNotFoundError()
    {
        var options = new DbContextOptionsBuilder<IntegrationPersistenceDbContext>()
            .UseInMemoryDatabase($"dead-letter-requeue-missing-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new IntegrationPersistenceDbContext(options);
        var service = new DeadLetterReplayService(dbContext);

        var result = await service.RequeueAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorCodes.DeadLetterMessageNotFound, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task RequeueAsync_WhenPayloadTypeIsInvalid_ReturnsValidationError()
    {
        var options = new DbContextOptionsBuilder<IntegrationPersistenceDbContext>()
            .UseInMemoryDatabase($"dead-letter-requeue-invalid-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new IntegrationPersistenceDbContext(options);
        var service = new DeadLetterReplayService(dbContext);

        var deadLetter = new DeadLetterMessage
        {
            Id = Guid.NewGuid(),
            OutboxMessageId = Guid.NewGuid(),
            IdempotencyKey = Guid.NewGuid(),
            EventType = "Missing.Type, Missing.Assembly",
            EventData = "{}",
            RetryCount = 5,
            LastError = "poison",
            FailedAt = DateTime.UtcNow
        };

        dbContext.DeadLetterMessages.Add(deadLetter);
        await dbContext.SaveChangesAsync();

        var result = await service.RequeueAsync(deadLetter.Id, CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorCodes.InvalidIntegrationEventPayload, result.GetErrorOrThrow().Code);
        Assert.AreEqual(0, await dbContext.OutboxMessages.CountAsync());
    }

    private static DeadLetterMessage CreateDeadLetterMessage(IntegrationEvent integrationEvent)
    {
        return new DeadLetterMessage
        {
            Id = Guid.NewGuid(),
            OutboxMessageId = Guid.NewGuid(),
            IdempotencyKey = integrationEvent.IdempotencyKey,
            EventType = integrationEvent.GetType().AssemblyQualifiedName
                       ?? integrationEvent.GetType().FullName
                       ?? integrationEvent.GetType().Name,
            EventData = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
            RetryCount = 5,
            LastError = "poison",
            FailedAt = DateTime.UtcNow
        };
    }
}

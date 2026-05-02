using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Integration;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> entity)
    {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.EventType).IsRequired().HasMaxLength(512);
        entity.Property(e => e.EventData).IsRequired();
        entity.Property(e => e.LastError).HasMaxLength(2000);

        entity.HasIndex(e => e.CreatedAt);
        entity.HasIndex(e => e.IdempotencyKey).IsUnique();
        entity.HasIndex(e => e.IsDeadLettered);
        entity.HasIndex(e => e.NextAttemptAt);
        entity.HasIndex(e => e.ProcessedAt);

        entity.ToTable("outbox_messages", "integration");
    }
}

public class DeadLetterMessageConfiguration : IEntityTypeConfiguration<DeadLetterMessage>
{
    public void Configure(EntityTypeBuilder<DeadLetterMessage> entity)
    {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.EventType).IsRequired().HasMaxLength(512);
        entity.Property(e => e.EventData).IsRequired();
        entity.Property(e => e.LastError).HasMaxLength(2000);

        entity.HasIndex(e => e.FailedAt);
        entity.HasIndex(e => e.OutboxMessageId);
        entity.HasIndex(e => e.RequeuedAt);

        entity.ToTable("dead_letter_messages", "integration");
    }
}

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> entity)
    {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.EventType).IsRequired().HasMaxLength(512);
        entity.Property(e => e.LastError).HasMaxLength(2000);

        entity.HasIndex(e => e.IdempotencyKey).IsUnique();
        entity.HasIndex(e => e.ProcessedAt);
        entity.HasIndex(e => e.ReceivedAt);

        entity.ToTable("inbox_messages", "integration");
    }
}

public class OrderFulfillmentSagaStateConfiguration : IEntityTypeConfiguration<OrderFulfillmentSagaState>
{
    public void Configure(EntityTypeBuilder<OrderFulfillmentSagaState> entity)
    {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.CurrentState).IsRequired().HasMaxLength(128);
        entity.Property(e => e.FailureReason).HasMaxLength(1000);

        entity.HasIndex(e => e.CorrelationId).IsUnique();
        entity.HasIndex(e => e.CurrentState);
        entity.HasIndex(e => e.OrderId).IsUnique();

        entity.ToTable("order_fulfillment_saga_states", "integration");
    }
}

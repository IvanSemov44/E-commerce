using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Payments.Infrastructure.Persistence.Configurations;

public sealed class PaymentsOutboxConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", schema: "payments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(512).IsRequired();
        builder.Property(x => x.EventData).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2000);

        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.HasIndex(x => x.IsDeadLettered);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.ProcessedAt);
        builder.HasIndex(x => x.NextAttemptAt);
    }
}

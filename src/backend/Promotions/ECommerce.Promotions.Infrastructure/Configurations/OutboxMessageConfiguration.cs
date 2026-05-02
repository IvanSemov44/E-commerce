using ECommerce.Promotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Promotions.Infrastructure.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.EventType).HasMaxLength(512).IsRequired();
        builder.Property(m => m.EventData).IsRequired();
        builder.Property(m => m.LastError).HasMaxLength(2000);

        builder.HasIndex(m => m.IdempotencyKey).IsUnique();
        builder.HasIndex(m => m.CreatedAt);
        builder.HasIndex(m => m.NextAttemptAt);
        builder.HasIndex(m => m.ProcessedAt);
    }
}

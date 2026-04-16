using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Catalog.Infrastructure.Configurations;

public class CatalogOutboxConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", schema: "catalog");

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.IdempotencyKey).IsUnique();
        builder.HasIndex(e => e.ProcessedAt);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.NextAttemptAt);
        builder.HasIndex(e => e.IsDeadLettered);

        builder.Property(e => e.EventType).IsRequired().HasMaxLength(512);
        builder.Property(e => e.EventData).IsRequired();
        builder.Property(e => e.LastError).HasMaxLength(2000);
    }
}

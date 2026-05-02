using ECommerce.Promotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Promotions.Infrastructure.Configurations;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.EventType).HasMaxLength(512).IsRequired();
        builder.Property(m => m.LastError).HasMaxLength(2000);

        builder.HasIndex(m => m.IdempotencyKey).IsUnique();
        builder.HasIndex(m => m.ProcessedAt);
        builder.HasIndex(m => m.ReceivedAt);
    }
}

using ECommerce.Promotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Promotions.Infrastructure.Configurations;

public class DeadLetterMessageConfiguration : IEntityTypeConfiguration<DeadLetterMessage>
{
    public void Configure(EntityTypeBuilder<DeadLetterMessage> builder)
    {
        builder.ToTable("dead_letter_messages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.EventType).HasMaxLength(512).IsRequired();
        builder.Property(m => m.EventData).IsRequired();
        builder.Property(m => m.LastError).HasMaxLength(2000);

        builder.HasIndex(m => m.FailedAt);
        builder.HasIndex(m => m.OutboxMessageId);
    }
}

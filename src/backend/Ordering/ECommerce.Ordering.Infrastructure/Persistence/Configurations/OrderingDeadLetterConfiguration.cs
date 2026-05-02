using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Ordering.Infrastructure.Persistence.Configurations;

public sealed class OrderingDeadLetterConfiguration : IEntityTypeConfiguration<DeadLetterMessage>
{
    public void Configure(EntityTypeBuilder<DeadLetterMessage> builder)
    {
        builder.ToTable("dead_letter_messages", schema: "ordering");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(512).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2000);

        builder.HasIndex(x => x.OutboxMessageId);
        builder.HasIndex(x => x.FailedAt);
    }
}

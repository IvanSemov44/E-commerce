using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Reviews.Infrastructure.Persistence.Configurations;

public class ReviewsDeadLetterConfiguration : IEntityTypeConfiguration<DeadLetterMessage>
{
    public void Configure(EntityTypeBuilder<DeadLetterMessage> builder)
    {
        builder.ToTable("dead_letter_messages", schema: "reviews");

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.OutboxMessageId);
        builder.HasIndex(e => e.FailedAt);

        builder.Property(e => e.EventType).IsRequired().HasMaxLength(512);
        builder.Property(e => e.EventData).IsRequired();
        builder.Property(e => e.LastError).HasMaxLength(2000);
    }
}

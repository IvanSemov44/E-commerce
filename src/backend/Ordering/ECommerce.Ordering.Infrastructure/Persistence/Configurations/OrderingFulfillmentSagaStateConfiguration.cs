using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Ordering.Infrastructure.Persistence.Configurations;

public sealed class OrderingFulfillmentSagaStateConfiguration : IEntityTypeConfiguration<OrderFulfillmentSagaState>
{
    public void Configure(EntityTypeBuilder<OrderFulfillmentSagaState> builder)
    {
        builder.ToTable("order_fulfillment_saga_states", schema: "ordering");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CurrentState).IsRequired().HasMaxLength(128);
        builder.Property(x => x.FailureReason).HasMaxLength(1000);

        builder.HasIndex(x => x.CorrelationId).IsUnique();
        builder.HasIndex(x => x.CurrentState);
        builder.HasIndex(x => x.OrderId).IsUnique();
    }
}

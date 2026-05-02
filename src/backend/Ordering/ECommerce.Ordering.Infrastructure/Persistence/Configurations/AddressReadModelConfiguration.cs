using ECommerce.Ordering.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Ordering.Infrastructure.Persistence.Configurations;

public sealed class AddressReadModelConfiguration : IEntityTypeConfiguration<AddressReadModel>
{
    public void Configure(EntityTypeBuilder<AddressReadModel> builder)
    {
        builder.ToTable("Addresses");
        builder.HasKey(x => x.Id);
    }
}

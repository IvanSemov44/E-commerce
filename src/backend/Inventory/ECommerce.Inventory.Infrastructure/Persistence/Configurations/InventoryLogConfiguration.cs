using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Inventory.Infrastructure.Persistence.Configurations;

// InventoryLog is an owned entity configured inside InventoryItemConfiguration.OwnsMany.
// This class intentionally does NOT implement IEntityTypeConfiguration<InventoryLog> to
// prevent ApplyConfigurationsFromAssembly from registering InventoryLog as a root entity,
// which conflicts with its owned-type registration and throws an EF model validation error.
public class InventoryLogConfiguration
{
}

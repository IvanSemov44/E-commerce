using ECommerce.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Infrastructure.Persistence;

public class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("ordering");
        modelBuilder.Entity<Order>().ToTable("Orders");
        modelBuilder.Entity<OrderItem>().ToTable("OrderItems");
    }
}

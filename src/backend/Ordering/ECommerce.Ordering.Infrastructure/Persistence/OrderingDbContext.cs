using ECommerce.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Infrastructure.Persistence;

public class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PromoCodeReadModel> PromoCodes => Set<PromoCodeReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("ordering");
        modelBuilder.Entity<Order>().ToTable("Orders");
        modelBuilder.Entity<OrderItem>().ToTable("OrderItems");
        modelBuilder.Entity<PromoCodeReadModel>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("PromoCodes", "promotions");
            entity.Property(x => x.Id).HasColumnName("Id");
            entity.Property(x => x.Code).HasColumnName("Code");
            entity.Property(x => x.IsActive).HasColumnName("IsActive");
            entity.Property(x => x.DiscountValue).HasColumnName("Discount");
        });
    }
}

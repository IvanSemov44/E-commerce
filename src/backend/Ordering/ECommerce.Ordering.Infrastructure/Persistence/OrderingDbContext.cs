using ECommerce.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Infrastructure.Persistence;

public class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ProductReadModel> Products => Set<ProductReadModel>();
    public DbSet<ProductImageReadModel> ProductImages => Set<ProductImageReadModel>();
    public DbSet<PromoCodeReadModel> PromoCodes => Set<PromoCodeReadModel>();
    public DbSet<AddressReadModel> Addresses => Set<AddressReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("ordering");
        modelBuilder.Entity<Order>().ToTable("Orders");
        modelBuilder.Entity<OrderItem>().ToTable("OrderItems");
        modelBuilder.Entity<ProductReadModel>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToTable("Products", "catalog");
            entity.Property(x => x.Id).HasColumnName("Id");
            entity.Property(x => x.Name).HasColumnName("Name");
            entity.Property(x => x.Price).HasColumnName("Price");
        });
        modelBuilder.Entity<ProductImageReadModel>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToTable("ProductImages", "catalog");
            entity.Property(x => x.Id).HasColumnName("Id");
            entity.Property(x => x.ProductId).HasColumnName("ProductId");
            entity.Property(x => x.Url).HasColumnName("Url");
            entity.Property(x => x.IsPrimary).HasColumnName("IsPrimary");
        });
        modelBuilder.Entity<PromoCodeReadModel>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("PromoCodes", "promotions");
            entity.Property(x => x.Id).HasColumnName("Id");
            entity.Property(x => x.Code).HasColumnName("Code");
            entity.Property(x => x.IsActive).HasColumnName("IsActive");
            entity.Property(x => x.DiscountValue).HasColumnName("Discount");
        });
        modelBuilder.Entity<AddressReadModel>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToTable("Addresses", "identity");
            entity.Property(x => x.Id).HasColumnName("Id");
            entity.Property(x => x.UserId).HasColumnName("UserId");
            entity.Property(x => x.StreetLine1).HasColumnName("StreetLine1");
            entity.Property(x => x.City).HasColumnName("City");
            entity.Property(x => x.Country).HasColumnName("Country");
            entity.Property(x => x.PostalCode).HasColumnName("PostalCode");
        });
    }
}

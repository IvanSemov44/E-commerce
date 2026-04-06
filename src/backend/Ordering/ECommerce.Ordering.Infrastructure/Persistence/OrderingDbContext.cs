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

        modelBuilder.HasDefaultSchema("public");
        modelBuilder.Entity<Order>().ToTable("Orders");
        modelBuilder.Entity<OrderItem>().ToTable("OrderItems");
        modelBuilder.Entity<ProductReadModel>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToTable("Products");
            entity.Property(x => x.Id).HasColumnName("Id");
            entity.Property(x => x.Name).HasColumnName("Name");
            entity.Property(x => x.Price).HasColumnName("Price");
            entity.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
        });
        modelBuilder.Entity<ProductImageReadModel>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToTable("ProductImages");
            entity.Property(x => x.Id).HasColumnName("Id");
            entity.Property(x => x.ProductId).HasColumnName("ProductId");
            entity.Property(x => x.Url).HasColumnName("Url");
            entity.Property(x => x.IsPrimary).HasColumnName("IsPrimary");
            entity.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
        });
        modelBuilder.Entity<PromoCodeReadModel>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToTable("PromoCodes");
            entity.Property(x => x.Id).HasColumnName("Id");
            entity.Property(x => x.Code).HasColumnName("Code");
            entity.Property(x => x.IsActive).HasColumnName("IsActive");
            entity.Property(x => x.DiscountValue).HasColumnName("DiscountValue");
            entity.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
        });
        modelBuilder.Entity<AddressReadModel>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToTable("Addresses");
            entity.Property(x => x.Id).HasColumnName("Id");
            entity.Property(x => x.UserId).HasColumnName("UserId");
            entity.Property(x => x.StreetLine1).HasColumnName("StreetLine1");
            entity.Property(x => x.City).HasColumnName("City");
            entity.Property(x => x.Country).HasColumnName("Country");
            entity.Property(x => x.PostalCode).HasColumnName("PostalCode");
            entity.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
        });
    }
}

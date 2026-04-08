using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Promotions.Infrastructure.Persistence;

public class PromotionsDbContext(DbContextOptions<PromotionsDbContext> options) : DbContext(options)
{
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("promotions");
        modelBuilder.ApplyConfiguration(new PromoCodeConfiguration());
        modelBuilder.Entity<PromoCode>().ToTable("PromoCodes");
    }
}

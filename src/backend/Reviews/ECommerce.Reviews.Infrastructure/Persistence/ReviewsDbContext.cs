using ECommerce.Reviews.Domain.Aggregates.Review;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Reviews.Infrastructure.Persistence;

public class ReviewsDbContext(DbContextOptions<ReviewsDbContext> options) : DbContext(options)
{
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ProductReadModel> Products => Set<ProductReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReviewsDbContext).Assembly);
    }
}

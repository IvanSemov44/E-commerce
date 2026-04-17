using ECommerce.Reviews.Domain.Aggregates.Review;
using ECommerce.Reviews.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Reviews.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews", "reviews");

        builder.HasKey(review => review.Id);

        builder.Property(review => review.ProductId)
            .IsRequired();

        builder.Property(review => review.UserId)
            .IsRequired();

        builder.Property(review => review.Rating)
            .HasConversion(
                value => value.Value,
                value => Rating.Create(value).GetDataOrThrow())
            .HasColumnName("Rating")
            .IsRequired();

        builder.OwnsOne(review => review.Content, content =>
        {
            content.Property(item => item.Title)
                .HasColumnName("Title")
                .HasMaxLength(100)
                .IsRequired(false);

            content.Property(item => item.Body)
                .HasColumnName("Body")
                .HasMaxLength(1000)
                .IsRequired();
        });

        builder.Property(review => review.Status)
            .HasConversion<string>()
            .HasColumnName("Status")
            .IsRequired();

        builder.Property(review => review.IsVerifiedPurchase)
            .HasColumnName("IsVerifiedPurchase")
            .IsRequired();

        builder.Property(review => review.HelpfulCount)
            .HasColumnName("HelpfulCount")
            .IsRequired();

        builder.Property(review => review.FlagCount)
            .HasColumnName("FlagCount")
            .IsRequired();

        builder.Property(review => review.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(review => review.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();

        builder.Property(review => review.DeletedAt)
            .HasColumnName("DeletedAt")
            .IsRequired(false);

        builder.HasQueryFilter(review => review.DeletedAt == null);

        builder.HasIndex(review => new { review.ProductId, review.UserId }).IsUnique();
        builder.HasIndex(review => review.Status);
        builder.HasIndex(review => review.FlagCount);
    }
}

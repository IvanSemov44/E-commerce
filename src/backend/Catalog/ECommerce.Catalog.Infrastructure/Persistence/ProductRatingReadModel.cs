namespace ECommerce.Catalog.Infrastructure.Persistence;

public class ProductRatingReadModel
{
    public Guid ProductId { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}

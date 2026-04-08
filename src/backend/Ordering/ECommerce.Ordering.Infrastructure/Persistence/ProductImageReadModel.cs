namespace ECommerce.Ordering.Infrastructure.Persistence;

public class ProductImageReadModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public DateTime UpdatedAt { get; set; }
}

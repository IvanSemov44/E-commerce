namespace ECommerce.Ordering.Infrastructure.Persistence;

public class ProductReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

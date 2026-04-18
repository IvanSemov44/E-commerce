namespace ECommerce.Shopping.Infrastructure.Persistence;

public class ProductReadModel
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
    public decimal Price { get; set; }
    public DateTime UpdatedAt { get; set; }
}

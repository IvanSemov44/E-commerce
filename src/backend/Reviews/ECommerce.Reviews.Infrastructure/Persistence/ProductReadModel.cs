namespace ECommerce.Reviews.Infrastructure.Persistence;

public class ProductReadModel
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
}

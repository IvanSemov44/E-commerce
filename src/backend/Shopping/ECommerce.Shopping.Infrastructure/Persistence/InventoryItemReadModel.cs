namespace ECommerce.Shopping.Infrastructure.Persistence;

public class InventoryItemReadModel
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime UpdatedAt { get; set; }
}

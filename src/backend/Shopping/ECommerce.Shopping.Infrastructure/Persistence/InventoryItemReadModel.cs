namespace ECommerce.Shopping.Infrastructure.Persistence;

public class InventoryItemReadModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

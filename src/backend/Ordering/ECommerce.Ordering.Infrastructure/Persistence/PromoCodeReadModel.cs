namespace ECommerce.Ordering.Infrastructure.Persistence;

public class PromoCodeReadModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
}

namespace ECommerce.Application.DTOs.Reviews;

public class CreateReviewDto
{
    public Guid ProductId { get; set; }
    public string? Title { get; set; }
    public string Comment { get; set; } = null!;
    public int Rating { get; set; }
}

namespace ECommerce.Reviews.Application.DTOs;

public class CreateReviewRequestDto
{
    public Guid ProductId { get; set; }
    public Guid? OrderId { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string Comment { get; set; } = string.Empty;
}

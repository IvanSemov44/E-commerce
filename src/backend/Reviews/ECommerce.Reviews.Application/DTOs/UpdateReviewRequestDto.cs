namespace ECommerce.Reviews.Application.DTOs;

public class UpdateReviewRequestDto
{
    public int? Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
}

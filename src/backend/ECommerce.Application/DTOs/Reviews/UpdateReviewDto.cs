namespace ECommerce.Application.DTOs.Reviews;

public class UpdateReviewDto
{
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public int? Rating { get; set; }
}
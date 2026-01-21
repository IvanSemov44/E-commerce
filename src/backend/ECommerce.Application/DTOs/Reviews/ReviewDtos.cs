namespace ECommerce.Application.DTOs.Reviews;

public class CreateReviewDto
{
    public Guid ProductId { get; set; }
    public string? Title { get; set; }
    public string Comment { get; set; } = null!;
    public int Rating { get; set; }
}

public class UpdateReviewDto
{
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public int? Rating { get; set; }
}

public class ReviewDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public int Rating { get; set; }
    public string? UserName { get; set; }
    public bool IsVerified { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

namespace ECommerce.Application.DTOs.Reviews;

public record ReviewDto
{
    public Guid Id { get; init; }
    public string? Title { get; init; }
    public string? Comment { get; init; }
    public int Rating { get; init; }
    public string? UserName { get; init; }
    public DateTime CreatedAt { get; init; }
}

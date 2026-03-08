namespace ECommerce.Application.DTOs.Reviews;

public record ReviewDetailDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid ProductId { get; init; }
    public string? Title { get; init; }
    public string? Comment { get; init; }
    public int Rating { get; init; }
    public string? UserName { get; init; }
    public bool IsVerified { get; init; }
    public bool IsApproved { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

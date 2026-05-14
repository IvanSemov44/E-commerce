namespace ECommerce.Identity.Application.DTOs;

public class UpdateProfileDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
}

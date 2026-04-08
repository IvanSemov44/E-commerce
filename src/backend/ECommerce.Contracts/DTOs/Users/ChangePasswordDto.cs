using System.ComponentModel.DataAnnotations;

namespace ECommerce.Contracts.DTOs.Users;

/// <summary>
/// DTO for changing user password.
/// </summary>
public class ChangePasswordDto
{
    [Required(ErrorMessage = "Old password is required")]
    public string OldPassword { get; set; } = null!;

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "New password must be between 8 and 100 characters")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Password confirmation is required")]
    public string ConfirmPassword { get; set; } = null!;
}


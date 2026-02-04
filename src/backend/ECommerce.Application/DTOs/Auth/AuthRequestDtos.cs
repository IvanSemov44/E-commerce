namespace ECommerce.Application.DTOs.Auth;

/// <summary>
/// Request DTO for refreshing authentication tokens.
/// </summary>
public class RefreshTokenRequest
{
    public string? Token { get; set; }
}

/// <summary>
/// Response DTO containing a new authentication token.
/// </summary>
public class TokenResponseDto
{
    public string Token { get; set; } = null!;
}

/// <summary>
/// Request DTO for initiating password reset.
/// </summary>
public class ForgotPasswordRequest
{
    public string Email { get; set; } = null!;
}

/// <summary>
/// Response DTO for password reset initiation.
/// </summary>
public class ForgotPasswordResponseDto
{
    public string? Token { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Request DTO for resetting password with token.
/// </summary>
public class ResetPasswordRequest
{
    public string Email { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

/// <summary>
/// Request DTO for changing password (authenticated users).
/// </summary>
public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

/// <summary>
/// Request DTO for email verification.
/// </summary>
public class VerifyEmailRequest
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = null!;
}

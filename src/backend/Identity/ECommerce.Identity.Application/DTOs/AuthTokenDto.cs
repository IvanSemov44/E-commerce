namespace ECommerce.Identity.Application.DTOs;

public record AuthTokenDto(
    string AccessToken,
    string RefreshToken,
    Guid   UserId
);

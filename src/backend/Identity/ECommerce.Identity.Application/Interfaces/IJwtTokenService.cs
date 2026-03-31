namespace ECommerce.Identity.Application.Interfaces;

/// <summary>
/// JWT token generation — infrastructure concern.
/// The domain doesn't know about JWT; this interface abstracts it away.
/// </summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(Domain.Aggregates.User.User user);
    string GenerateRefreshToken();
}

namespace ECommerce.Identity.Application.Interfaces;

/// <summary>
/// Password hashing abstraction — infrastructure concern.
/// The domain validates password policy; this interface produces the hash.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string rawPassword);
    bool   Verify(string rawPassword, string hash);
}

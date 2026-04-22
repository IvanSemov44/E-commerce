namespace ECommerce.Identity.Domain.Interfaces;

public interface IPasswordHasher
{
    string Hash(string rawPassword);
    bool   Verify(string rawPassword, string hash);
}

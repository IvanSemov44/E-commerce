namespace ECommerce.Identity.Infrastructure.Persistence.Converters;

public sealed class PasswordHashConverter() : ValueConverter<PasswordHash, string>(
    p => p.Hash,
    v => ParseFromProvider(v))
{
    private static PasswordHash ParseFromProvider(string value)
    {
        var hashResult = PasswordHash.FromHash(value);
        if (hashResult.IsSuccess)
            return hashResult.GetDataOrThrow();

        throw new InvalidOperationException(
            "Invalid password hash value found in identity.Users.PasswordHash.");
    }
}

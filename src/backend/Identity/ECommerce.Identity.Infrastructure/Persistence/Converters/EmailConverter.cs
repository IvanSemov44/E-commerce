namespace ECommerce.Identity.Infrastructure.Persistence.Converters;

public sealed class EmailConverter() : ValueConverter<Email, string>(
    e => e.Value,
    v => ParseFromProvider(v))
{
    private static Email ParseFromProvider(string value)
    {
        var emailResult = Email.Create(value);
        if (emailResult.IsSuccess)
            return emailResult.GetDataOrThrow();

        throw new InvalidOperationException(
            $"Invalid email value '{value}' found in identity.Users.Email.");
    }
}

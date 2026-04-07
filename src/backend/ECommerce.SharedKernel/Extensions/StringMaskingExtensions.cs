namespace ECommerce.SharedKernel.Extensions;

public static class StringMaskingExtensions
{
    public static string MaskEmail(this string? email, int visiblePrefixLength = 3)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "[null-email]";
        }

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0 || atIndex == email.Length - 1)
        {
            return "[invalid-email]";
        }

        var localPart = email[..atIndex];
        var domainPart = email[atIndex..];
        var visibleLength = Math.Clamp(visiblePrefixLength, 1, localPart.Length);
        var visiblePrefix = localPart[..visibleLength];

        return $"{visiblePrefix}***{domainPart}";
    }
}

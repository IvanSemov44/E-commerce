using ECommerce.SharedKernel.Results;

namespace ECommerce.Identity.Domain.Errors;

public static class IdentityErrors
{
    // Email
    public static readonly DomainError EmailEmpty     = new("EMAIL_EMPTY",      "Email is required.");
    public static readonly DomainError EmailTooLong   = new("EMAIL_TOO_LONG",   "Email must not exceed 256 characters.");
    public static readonly DomainError EmailInvalid   = new("EMAIL_INVALID",    "Email format is invalid.");
    public static readonly DomainError EmailAlreadyVerified = new("EMAIL_ALREADY_VERIFIED", "Email is already verified.");
    public static readonly DomainError EmailTokenInvalid    = new("EMAIL_TOKEN_INVALID",    "Email verification token is invalid.");

    // PersonName
    public static readonly DomainError NameFirstEmpty = new("NAME_FIRST_EMPTY", "First name is required.");
    public static readonly DomainError NameLastEmpty  = new("NAME_LAST_EMPTY",  "Last name is required.");
    public static readonly DomainError NameTooLong    = new("NAME_TOO_LONG",    "First and last name must not exceed 100 characters each.");

    // Password
    public static readonly DomainError PasswordEmpty    = new("PASSWORD_EMPTY",    "Password is required.");
    public static readonly DomainError PasswordTooShort = new("PASSWORD_TOO_SHORT","Password must be at least 8 characters.");
    public static readonly DomainError PasswordNoUpper  = new("PASSWORD_NO_UPPER", "Password must contain at least one uppercase letter.");
    public static readonly DomainError PasswordNoDigit  = new("PASSWORD_NO_DIGIT", "Password must contain at least one digit.");
    public static readonly DomainError PasswordHashEmpty = new("PASSWORD_HASH_EMPTY", "Password hash cannot be empty.");

    // Auth (aggregate-level — detectable without a repo lookup)
    public static readonly DomainError InvalidCredentials = new("INVALID_CREDENTIALS", "Invalid email or password.");
    public static readonly DomainError TokenInvalid       = new("TOKEN_INVALID",        "Refresh token is invalid or expired.");
    public static readonly DomainError TokenRevoked       = new("TOKEN_REVOKED",        "Refresh token has been revoked.");

    // NOTE: UserNotFound and EmailTaken are NOT here — they require a repository lookup,
    // making them application-layer concerns. They live in IdentityApplicationErrors (step-2).

    // Address (raised by User aggregate — no repo lookup needed)
    public static readonly DomainError AddressLimit        = new("ADDRESS_LIMIT",         "A user cannot have more than 5 addresses.");
    public static readonly DomainError AddressNotFound     = new("ADDRESS_NOT_FOUND",     "Address not found.");
    public static readonly DomainError AddressStreetEmpty  = new("ADDRESS_STREET_EMPTY",  "Street is required.");
    public static readonly DomainError AddressCityEmpty    = new("ADDRESS_CITY_EMPTY",    "City is required.");
    public static readonly DomainError AddressCountryEmpty = new("ADDRESS_COUNTRY_EMPTY", "Country is required.");
}

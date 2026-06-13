using ECommerce.SharedKernel.Results;

namespace ECommerce.Identity.Domain.Errors;

public static class IdentityErrors
{
    // Email
    public static readonly DomainError EmailEmpty          = new("EMAIL_EMPTY",           "Email is required.",                                              ErrorType.Validation);
    public static readonly DomainError EmailTooLong        = new("EMAIL_TOO_LONG",         "Email must not exceed 256 characters.",                          ErrorType.Validation);
    public static readonly DomainError EmailInvalid        = new("EMAIL_INVALID",          "Email format is invalid.",                                        ErrorType.Validation);
    public static readonly DomainError EmailAlreadyVerified = new("EMAIL_ALREADY_VERIFIED","Email is already verified.",                                      ErrorType.Validation);
    public static readonly DomainError EmailTokenInvalid   = new("EMAIL_TOKEN_INVALID",    "Email verification token is invalid.",                           ErrorType.Validation);

    // PersonName
    public static readonly DomainError NameFirstEmpty = new("NAME_FIRST_EMPTY", "First name is required.",                                                   ErrorType.Validation);
    public static readonly DomainError NameLastEmpty  = new("NAME_LAST_EMPTY",  "Last name is required.",                                                    ErrorType.Validation);
    public static readonly DomainError NameTooLong    = new("NAME_TOO_LONG",    "First and last name must not exceed 100 characters each.",                 ErrorType.Validation);

    // Password
    public static readonly DomainError PasswordEmpty     = new("PASSWORD_EMPTY",     "Password is required.",                                               ErrorType.Validation);
    public static readonly DomainError PasswordTooShort  = new("PASSWORD_TOO_SHORT", "Password must be at least 8 characters.",                             ErrorType.Validation);
    public static readonly DomainError PasswordNoUpper   = new("PASSWORD_NO_UPPER",  "Password must contain at least one uppercase letter.",                ErrorType.Validation);
    public static readonly DomainError PasswordNoDigit   = new("PASSWORD_NO_DIGIT",  "Password must contain at least one digit.",                           ErrorType.Validation);
    public static readonly DomainError PasswordHashEmpty = new("PASSWORD_HASH_EMPTY","Password hash cannot be empty.",                                      ErrorType.Validation);

    // Auth (aggregate-level — detectable without a repo lookup)
    public static readonly DomainError InvalidCredentials = new("INVALID_CREDENTIALS", "Invalid email or password.",              ErrorType.Unauthorized);
    public static readonly DomainError TokenInvalid       = new("TOKEN_INVALID",        "Refresh token is invalid or expired.",   ErrorType.Unauthorized);
    public static readonly DomainError TokenRevoked       = new("TOKEN_REVOKED",        "Refresh token has been revoked.",        ErrorType.Unauthorized);

    // NOTE: UserNotFound and EmailTaken are NOT here — they require a repository lookup,
    // making them application-layer concerns. They live in IdentityApplicationErrors (step-2).

    // Address (raised by User aggregate — no repo lookup needed)
    public static readonly DomainError AddressLimit        = new("ADDRESS_LIMIT",         "A user cannot have more than 5 addresses.", ErrorType.Validation);
    public static readonly DomainError AddressNotFound     = new("ADDRESS_NOT_FOUND",     "Address not found.",                       ErrorType.NotFound);
    public static readonly DomainError AddressStreetEmpty  = new("ADDRESS_STREET_EMPTY",  "Street is required.",                      ErrorType.Validation);
    public static readonly DomainError AddressCityEmpty    = new("ADDRESS_CITY_EMPTY",    "City is required.",                        ErrorType.Validation);
    public static readonly DomainError AddressCountryEmpty = new("ADDRESS_COUNTRY_EMPTY", "Country is required.",                     ErrorType.Validation);
}

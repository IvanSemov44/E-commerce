using ECommerce.SharedKernel.Results;

namespace ECommerce.Identity.Application.Errors;

/// <summary>
/// Application-layer errors that require a repository lookup.
/// Domain errors (IdentityErrors) are raised by the aggregate alone.
/// </summary>
public static class IdentityApplicationErrors
{
    public static readonly DomainError UserNotFound    = new("USER_NOT_FOUND",    "User not found.");
    public static readonly DomainError EmailTaken      = new("EMAIL_TAKEN",       "This email address is already registered.");
    public static readonly DomainError AddressNotFound = new("ADDRESS_NOT_FOUND", "Address not found.");
}

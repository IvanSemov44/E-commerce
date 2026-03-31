using ECommerce.Identity.Domain.Errors;
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Identity.Domain.ValueObjects;

/// <summary>
/// Multi-property value object — first + last name with computed equality.
/// Uses sealed class : ValueObject because equality spans two fields.
/// </summary>
public sealed class PersonName : ValueObject
{
    public string First { get; }
    public string Last { get; }
    public string FullName => $"{First} {Last}";

    private PersonName(string first, string last) => (First, Last) = (first, last);

    public static Result<PersonName> Create(string? first, string? last)
    {
        if (string.IsNullOrWhiteSpace(first)) return Result<PersonName>.Fail(IdentityErrors.NameFirstEmpty);
        if (string.IsNullOrWhiteSpace(last))  return Result<PersonName>.Fail(IdentityErrors.NameLastEmpty);

        var f = first.Trim();
        var l = last.Trim();

        if (f.Length > 100 || l.Length > 100)
            return Result<PersonName>.Fail(IdentityErrors.NameTooLong);

        return Result<PersonName>.Ok(new PersonName(f, l));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return First.ToLowerInvariant();
        yield return Last.ToLowerInvariant();
    }

    public override string ToString() => FullName;
}

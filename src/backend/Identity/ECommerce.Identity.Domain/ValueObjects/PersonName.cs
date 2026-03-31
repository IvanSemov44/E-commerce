using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Domain.Errors;

namespace ECommerce.Identity.Domain.ValueObjects;

// Multi-property VO → use sealed class : ValueObject (equality across two fields)
public sealed class PersonName : ValueObject
{
    public string First { get; private set; } = null!;
    public string Last  { get; private set; } = null!;
    public string FullName => $"{First} {Last}";

    private PersonName() { } // EF Core
    private PersonName(string first, string last) { First = first; Last = last; }

    public static Result<PersonName> Create(string first, string last)
    {
        if (string.IsNullOrWhiteSpace(first))
            return Result<PersonName>.Fail(IdentityErrors.NameFirstEmpty);
        if (string.IsNullOrWhiteSpace(last))
            return Result<PersonName>.Fail(IdentityErrors.NameLastEmpty);
        if (first.Trim().Length > 100 || last.Trim().Length > 100)
            return Result<PersonName>.Fail(IdentityErrors.NameTooLong);

        return Result<PersonName>.Ok(new PersonName(first.Trim(), last.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return First.ToLowerInvariant();
        yield return Last.ToLowerInvariant();
    }
}

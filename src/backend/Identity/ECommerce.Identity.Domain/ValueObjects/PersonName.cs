using ECommerce.Identity.Domain.Errors;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Identity.Domain.ValueObjects;

public sealed record PersonName
{
    public string First { get; }
    public string Last { get; }
    public string FullName => $"{First} {Last}";

    private PersonName(string first, string last) => (First, Last) = (first, last);

    public static Result<PersonName> Create(string? first, string? last)
    {
        if (string.IsNullOrWhiteSpace(first)) return Result<PersonName>.Fail(IdentityErrors.NameFirstEmpty);
        if (string.IsNullOrWhiteSpace(last))  return Result<PersonName>.Fail(IdentityErrors.NameLastEmpty);

        string f = first.Trim();
        string l = last.Trim();

        if (f.Length > 100 || l.Length > 100)
            return Result<PersonName>.Fail(IdentityErrors.NameTooLong);

        return Result<PersonName>.Ok(new PersonName(f, l));
    }

    public bool Equals(PersonName? other) =>
        other is not null &&
        First.Equals(other.First, StringComparison.OrdinalIgnoreCase) &&
        Last.Equals(other.Last, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() =>
        HashCode.Combine(First.ToLowerInvariant(), Last.ToLowerInvariant());

    public override string ToString() => FullName;
}

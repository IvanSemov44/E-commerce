using System.Text.RegularExpressions;
using ECommerce.Catalog.Domain.Errors;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Domain.ValueObjects;

public sealed record Slug
{
    public string Value { get; }

    private Slug(string value) => Value = value;

    public static Result<Slug> Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<Slug>.Fail(CatalogErrors.SlugEmpty);

        string slug = raw.Trim().ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        slug = Regex.Replace(slug, "[^a-z0-9\\-]", "");
        slug = Regex.Replace(slug, "-+", "-").Trim('-');

        if (slug.Length == 0)
            return Result<Slug>.Fail(CatalogErrors.SlugInvalid);

        return Result<Slug>.Ok(new Slug(slug));
    }
}

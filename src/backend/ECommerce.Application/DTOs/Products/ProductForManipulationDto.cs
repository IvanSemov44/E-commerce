namespace ECommerce.Application.DTOs.Products;

using System.ComponentModel.DataAnnotations;
using Common;

/// <summary>
/// Base class for product manipulation DTOs.
/// Contains common validation rules for create and update operations.
/// Reduces code duplication between CreateProductDto and UpdateProductDto.
/// </summary>
public abstract record ProductForManipulationDto
{
    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    [Required(ErrorMessage = "Product name is a required field.")]
    [MaxLength(100, ErrorMessage = "Maximum length for Name is 100 characters.")]
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    [Required(ErrorMessage = "Product description is a required field.")]
    [MaxLength(2000, ErrorMessage = "Maximum length for Description is 2000 characters.")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; init; }

    /// <summary>
    /// Gets or sets the stock quantity.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
    public int Stock { get; init; }

    /// <summary>
    /// Gets or sets the category ID.
    /// </summary>
    [Required(ErrorMessage = "Category ID is required.")]
    public Guid CategoryId { get; init; }

    /// <summary>
    /// Gets or sets whether the product is featured.
    /// </summary>
    public bool IsFeatured { get; init; }
}

/// <summary>
/// Query parameters for product filtering, searching, sorting, and pagination.
/// Inherits pagination functionality from RequestParameters.
/// </summary>
public class ProductRequestParameters : RequestParameters
{
    /// <summary>
    /// Gets or sets the minimum price filter.
    /// </summary>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Gets or sets the maximum price filter.
    /// </summary>
    public decimal? MaxPrice { get; set; } = decimal.MaxValue;

    /// <summary>
    /// Gets or sets the category ID filter.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the minimum rating filter.
    /// </summary>
    public decimal? MinRating { get; set; }

    /// <summary>
    /// Gets or sets whether to filter only featured products.
    /// </summary>
    public bool? IsFeatured { get; set; }

    /// <summary>
    /// Validates that the price range is valid (min <= max).
    /// </summary>
    public bool ValidPriceRange => !MaxPrice.HasValue || !MinPrice.HasValue || MaxPrice >= MinPrice;

    /// <summary>
    /// Throws an exception if the price range is invalid.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when MaxPrice < MinPrice.</exception>
    public void ValidatePriceRange()
    {
        if (!ValidPriceRange)
            throw new InvalidOperationException("MaxPrice cannot be less than MinPrice.");
    }
}

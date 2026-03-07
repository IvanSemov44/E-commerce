interface ActiveFilterParams {
  search: string;
  categorySelected: boolean;
  minPrice: number | undefined;
  maxPrice: number | undefined;
  minRating: number | undefined;
  isFeatured: boolean | undefined;
}

/**
 * Check if any filters are active
 */
export function hasActiveFilters({
  search,
  categorySelected,
  minPrice,
  maxPrice,
  minRating,
  isFeatured,
}: ActiveFilterParams): boolean {
  return (
    !!search ||
    categorySelected ||
    minPrice !== undefined ||
    maxPrice !== undefined ||
    minRating !== undefined ||
    isFeatured === true
  );
}

/**
 * Format price range display
 * @param minPrice - Minimum price
 * @param maxPrice - Maximum price
 * @returns Formatted price range
 */
export function formatPriceRange(minPrice: number | undefined, maxPrice: number | undefined): string {
  const min = minPrice || 0;
  const max = maxPrice || '∞';
  return `$${min} - $${max}`;
}

/**
 * Get rating display text
 * @param rating - Minimum rating
 * @returns Display text
 */
export function getRatingDisplayText(rating: number): string {
  return `${rating}+ stars`;
}

/**
 * Check if any filters are active
 * @param search - Search query
 * @param categorySelected - Whether category is selected
 * @param minPrice - Minimum price filter
 * @param maxPrice - Maximum price filter
 * @param minRating - Minimum rating filter
 * @param isFeatured - Featured filter
 * @returns True if any filter is active
 */
export function hasActiveFilters(
  search: string,
  categorySelected: boolean,
  minPrice: number | undefined,
  maxPrice: number | undefined,
  minRating: number | undefined,
  isFeatured: boolean | undefined
): boolean {
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

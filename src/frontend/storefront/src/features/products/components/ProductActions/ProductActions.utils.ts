/**
 * Check if stock is low
 * @param stockQuantity - Current stock
 * @param lowStockThreshold - Threshold for low stock warning
 * @returns True if stock is low
 */
export function isStockLow(stockQuantity: number, lowStockThreshold: number): boolean {
  return stockQuantity > 0 && stockQuantity <= lowStockThreshold;
}

/**
 * Check if item is in stock
 * @param stockQuantity - Current stock
 * @returns True if in stock
 */
export function isInStock(stockQuantity: number): boolean {
  return stockQuantity > 0;
}

/**
 * Get stock status message
 * @param stockQuantity - Current stock
 * @returns Status message
 */
export function getStockStatusMessage(stockQuantity: number): string {
  if (stockQuantity === 0) return 'Out of stock';
  return `${stockQuantity} in stock`;
}

/**
 * Get button text for add to cart
 * @param stockQuantity - Current stock
 * @param addedToCart - Whether item is added to cart
 * @returns Button text
 */
export function getAddToCartButtonText(stockQuantity: number, addedToCart: boolean): string {
  if (stockQuantity === 0) return 'Out of Stock';
  if (addedToCart) return '✓ Added to Cart!';
  return 'Add to Cart';
}

/**
 * Get wishlist button text
 * @param isInWishlist - Whether item is in wishlist
 * @returns Button text
 */
export function getWishlistButtonText(isInWishlist: boolean | undefined): string {
  return isInWishlist ? '♥ In Wishlist' : '♡ Add to Wishlist';
}

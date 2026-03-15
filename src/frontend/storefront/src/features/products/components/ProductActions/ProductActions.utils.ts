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

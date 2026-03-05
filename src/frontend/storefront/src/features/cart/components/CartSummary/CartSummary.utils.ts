/**
 * Calculate remaining amount for free shipping
 * @param subtotal - Current cart subtotal
 * @param threshold - Free shipping threshold
 * @returns Remaining amount needed for free shipping
 */
export function calculateFreeShippingRemaining(subtotal: number, threshold: number): number {
  return Math.max(0, threshold - subtotal);
}

/**
 * Check if free shipping message should be shown
 * @param subtotal - Current cart subtotal
 * @param threshold - Free shipping threshold
 * @returns True if message should be shown
 */
export function shouldShowFreeShippingMessage(subtotal: number, threshold: number): boolean {
  return subtotal > 0 && subtotal < threshold;
}

/**
 * Format currency value
 * @param amount - Amount to format
 * @returns Formatted currency string
 */
export function formatCurrency(amount: number): string {
  return amount.toFixed(2);
}

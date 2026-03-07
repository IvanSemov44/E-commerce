/**
 * Calculate subtotal for a cart item
 * @param price - Item price
 * @param quantity - Item quantity
 * @returns Formatted subtotal
 */
export function calculateSubtotal(price: number, quantity: number): string {
  return (price * quantity).toFixed(2);
}

export { formatPrice } from '@/shared/lib/utils/priceFormatter';

/**
 * Check if max stock is reached
 * @param quantity - Current quantity
 * @param maxStock - Maximum stock
 * @returns True if max stock reached
 */
export function isMaxStockReached(quantity: number, maxStock: number): boolean {
  return quantity >= maxStock;
}

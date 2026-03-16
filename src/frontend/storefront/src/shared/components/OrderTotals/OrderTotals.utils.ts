export { formatPrice as formatCurrency } from '@/shared/lib/utils/priceFormatter';

/**
 * Check if amount should be displayed as free
 * @param amount - Amount to check
 * @returns Boolean indicating if amount is free (0)
 */
export function isFree(amount: number): boolean {
  return amount === 0;
}

/**
 * Calculate total with components
 * @param subtotal - Order subtotal
 * @param discount - Discount amount
 * @param shipping - Shipping cost
 * @param tax - Tax amount
 * @returns Calculated total
 */
export function calculateTotal(
  subtotal: number,
  discount: number,
  shipping: number,
  tax: number
): number {
  return subtotal - discount + shipping + tax;
}

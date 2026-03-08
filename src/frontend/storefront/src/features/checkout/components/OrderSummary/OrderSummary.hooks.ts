/**
 * Hook for handling promo code operations
 * @param onPromoCodeChange - Callback when promo code input changes
 * @param onApplyPromoCode - Callback when apply button is clicked
 * @param onRemovePromoCode - Callback when remove button is clicked
 * @returns Object with promo code handlers and state
 */
export function usePromoCode(
  onPromoCodeChange: (code: string) => void,
  onApplyPromoCode: () => Promise<void>,
  onRemovePromoCode: () => void
) {
  const handlePromoCodeChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onPromoCodeChange(e.target.value);
  };

  const handleApplyPromoCode = async () => {
    await onApplyPromoCode();
  };

  const handleRemovePromoCode = () => {
    onRemovePromoCode();
  };

  return {
    handlePromoCodeChange,
    handleApplyPromoCode,
    handleRemovePromoCode,
  };
}

/**
 * Hook for calculating order totals with discount
 * @param subtotal - Order subtotal
 * @param discount - Discount amount
 * @param shipping - Shipping cost
 * @param tax - Tax amount
 * @returns Object with calculated totals
 */
export function useOrderCalculations(
  subtotal: number,
  discount: number,
  shipping: number,
  tax: number
) {
  const discountedSubtotal = subtotal - discount;
  const total = discountedSubtotal + shipping + tax;

  const hasDiscount = discount > 0;
  const freeShipping = shipping === 0;

  return {
    discountedSubtotal,
    total,
    hasDiscount,
    freeShipping,
  };
}

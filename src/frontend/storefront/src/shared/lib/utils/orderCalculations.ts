import { FREE_SHIPPING_THRESHOLD, STANDARD_SHIPPING_COST, DEFAULT_TAX_RATE } from './constants';

export interface OrderTotals {
  subtotal: number;
  shipping: number;
  tax: number;
  discount: number;
  total: number;
}

export function calculateOrderTotals(subtotal: number, discount = 0): OrderTotals {
  const shipping =
    subtotal >= FREE_SHIPPING_THRESHOLD || subtotal === 0 ? 0 : STANDARD_SHIPPING_COST;
  const tax = subtotal * DEFAULT_TAX_RATE;
  const total = subtotal - discount + shipping + tax;
  return { subtotal, shipping, tax, discount, total };
}

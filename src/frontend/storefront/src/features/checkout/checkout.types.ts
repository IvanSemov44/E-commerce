/**
 * Checkout Types
 * Shared type definitions for the checkout feature
 */

import type { CheckoutFormValues } from './schemas/checkoutSchemas';

export type ShippingFormData = CheckoutFormValues;

export interface PromoCodeValidation {
  isValid: boolean;
  discountAmount: number;
  message?: string;
}

export interface OrderTotals {
  subtotal: number;
  discount: number;
  shipping: number;
  tax: number;
  total: number;
}

export interface PromoCodeState {
  code: string;
  validation: PromoCodeValidation | null;
  isValidating: boolean;
  onChange: (code: string) => void;
  onApply: () => Promise<void>;
  onRemove: () => void;
}

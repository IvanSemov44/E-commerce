/**
 * Checkout Types
 * Shared type definitions for the checkout feature
 */

import type { FormEvent } from 'react';
import type { CartItem } from '@/features/cart/slices/cartSlice';
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

export interface UseCheckoutReturn {
  // Form state
  formData: ShippingFormData;
  setFormData: (data: Partial<ShippingFormData>) => void;
  errors: Partial<Record<keyof ShippingFormData, string>>;

  // Promo code state
  promoCode: string;
  setPromoCode: (code: string) => void;
  promoCodeValidation: PromoCodeValidation | null;
  validatingPromoCode: boolean;
  handleApplyPromoCode: () => Promise<void>;
  handleRemovePromoCode: () => void;

  // Order state
  orderComplete: boolean;
  orderNumber: string;
  error: string | null;
  isGuestOrder: boolean;

  // Cart info
  cartItems: CartItem[];
  subtotal: number;
  isLoading: boolean;

  // Totals
  discount: number;
  shipping: number;
  tax: number;
  total: number;

  // Payment method
  paymentMethod: string;
  setPaymentMethod: (method: string) => void;

  // Submit handler
  handleSubmit: (e: FormEvent) => Promise<void>;
}

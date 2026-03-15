import type { CartItem } from '@/features/cart/slices/cartSlice';

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

export interface OrderSummaryProps {
  cartItems: CartItem[];
  totals: OrderTotals;
  promoCode: PromoCodeState;
}

import type { CartItem } from '@/features/cart/slices/cartSlice';

export interface PromoCodeValidation {
  isValid: boolean;
  discountAmount: number;
  message?: string;
}

export interface OrderSummaryProps {
  cartItems: CartItem[];
  subtotal: number;
  discount: number;
  shipping: number;
  tax: number;
  total: number;
  promoCode: string;
  onPromoCodeChange: (code: string) => void;
  promoCodeValidation: PromoCodeValidation | null;
  validatingPromoCode: boolean;
  onApplyPromoCode: () => Promise<void>;
  onRemovePromoCode: () => void;
}

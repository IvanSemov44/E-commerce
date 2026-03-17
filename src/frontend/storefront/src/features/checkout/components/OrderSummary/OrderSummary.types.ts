import type { CartItem } from '@/features/cart/slices/cartSlice';
import type { PromoCodeValidation, OrderTotals, PromoCodeState } from '../../checkout.types';

export type { PromoCodeValidation, OrderTotals, PromoCodeState };

export interface OrderSummaryProps {
  cartItems: CartItem[];
  totals: OrderTotals;
  promoCode: PromoCodeState;
}

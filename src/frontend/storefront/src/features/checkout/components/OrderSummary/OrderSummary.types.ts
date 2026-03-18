import type { CartItem } from '@/features/cart/slices/cartSlice';
import type { OrderTotals, PromoCodeState } from '../../checkout.types';

export interface OrderSummaryProps {
  cart: {
    items: CartItem[];
  };
  totals: OrderTotals;
  promo: PromoCodeState;
}

import type { CartItem as CartItemType } from '@/features/cart/slices/cartSlice';

export interface CartItemProps {
  item: CartItemType;
  onUpdateQuantity: (id: string, quantity: number) => void;
  onRemove: (id: string) => void;
  readOnly?: boolean;
}

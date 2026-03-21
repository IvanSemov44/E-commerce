import { useAppDispatch, useAppSelector } from '@/shared/lib/store';
import { addItem, updateQuantity, removeItem } from '../slices/cartSlice';
import { useAddToCartMutation, useUpdateCartItemMutation, useRemoveFromCartMutation } from '../api';
import type { CartItem } from '../types';

/**
 * Single source of truth for all cart CRUD operations.
 * Routes each action to the correct channel based on auth state:
 *   - Guest: local Redux (persisted to localStorage)
 *   - Authenticated: backend API (RTK Query)
 *
 * Does not handle UI feedback — callers own their toasts/error state.
 */
export function useCartOperations() {
  const dispatch = useAppDispatch();
  const isAuthenticated = useAppSelector((s) => s.auth.isAuthenticated);

  const [addToCartApi] = useAddToCartMutation();
  const [updateCartItemApi] = useUpdateCartItemMutation();
  const [removeFromCartApi] = useRemoveFromCartMutation();

  async function add(item: CartItem): Promise<void> {
    if (isAuthenticated) {
      await addToCartApi({ productId: item.id, quantity: item.quantity }).unwrap();
    } else {
      dispatch(addItem(item));
    }
  }

  async function update(itemId: string, quantity: number): Promise<void> {
    if (quantity < 1) return;
    if (isAuthenticated) {
      await updateCartItemApi({ cartItemId: itemId, quantity }).unwrap();
    } else {
      dispatch(updateQuantity({ id: itemId, quantity }));
    }
  }

  async function remove(itemId: string): Promise<void> {
    if (isAuthenticated) {
      await removeFromCartApi(itemId).unwrap();
    } else {
      dispatch(removeItem(itemId));
    }
  }

  return { add, update, remove };
}

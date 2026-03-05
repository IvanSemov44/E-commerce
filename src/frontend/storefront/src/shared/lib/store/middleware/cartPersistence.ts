import type { Middleware } from '@reduxjs/toolkit';
import type { CartItem } from '@/features/cart/slices/cartSlice';
import { logger } from '@/shared/lib/utils/logger';

const CART_STORAGE_KEY = 'ecommerce_cart';
const CART_ACTIONS = ['cart/addItem', 'cart/removeItem', 'cart/updateQuantity', 'cart/clearCart'];

export const saveCartToLocalStorage = (items: CartItem[]): void => {
  if (typeof window === 'undefined') return;
  try {
    if (items.length === 0) {
      localStorage.removeItem(CART_STORAGE_KEY);
    } else {
      localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(items));
    }
  } catch (error) {
    logger.error('cartPersistence', 'Failed to save cart to localStorage', error);
  }
};

export const cartPersistenceMiddleware: Middleware = (store) => (next) => (action) => {
  const result = next(action);

  const actionObj = action as { type: string };
  if (CART_ACTIONS.includes(actionObj.type)) {
    const state = store.getState();
    saveCartToLocalStorage(state.cart.items);
  }

  return result;
};

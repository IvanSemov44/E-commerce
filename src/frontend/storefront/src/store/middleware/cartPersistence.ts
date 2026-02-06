import { Middleware } from '@reduxjs/toolkit';
import { RootState } from '../store';

const CART_STORAGE_KEY = 'ecommerce_cart';
const CART_ACTIONS = ['cart/addItem', 'cart/removeItem', 'cart/updateQuantity', 'cart/clearCart'];

export const saveCartToLocalStorage = (items: any[]): void => {
  if (typeof window === 'undefined') return;
  try {
    if (items.length === 0) {
      localStorage.removeItem(CART_STORAGE_KEY);
    } else {
      localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(items));
    }
  } catch (error) {
    console.error('Failed to save cart to localStorage:', error);
  }
};

export const cartPersistenceMiddleware: Middleware<{}, RootState> = (store) => (next) => (action) => {
  const result = next(action);

  if (CART_ACTIONS.includes(action.type)) {
    const state = store.getState();
    saveCartToLocalStorage(state.cart.items);
  }

  return result;
};

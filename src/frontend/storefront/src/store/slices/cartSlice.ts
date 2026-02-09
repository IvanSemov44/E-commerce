import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import type { RootState } from '../store';

export interface CartItem {
  id: string;
  name: string;
  slug: string;
  price: number;
  quantity: number;
  maxStock: number;
  image: string;
  compareAtPrice?: number;
}

export interface CartState {
  items: CartItem[];
  lastUpdated: number;
}

const CART_STORAGE_KEY = 'ecommerce_cart';

function loadCartFromLocalStorage(): CartItem[] {
  if (typeof window === 'undefined') return [];
  try {
    const stored = localStorage.getItem(CART_STORAGE_KEY);
    if (!stored) return [];
    const parsed = JSON.parse(stored);
    return Array.isArray(parsed) ? parsed : [];
  } catch (error) {
    console.error('Failed to load cart from localStorage:', error);
    return [];
  }
}

const initialState: CartState = {
  items: loadCartFromLocalStorage(),
  lastUpdated: Date.now(),
};

export const cartSlice = createSlice({
  name: 'cart',
  initialState,
  reducers: {
    addItem: (state, action: PayloadAction<CartItem>) => {
      const newItem = action.payload;
      const existingItem = state.items.find(item => item.id === newItem.id);

      if (existingItem) {
        const newQuantity = existingItem.quantity + newItem.quantity;
        if (newQuantity <= existingItem.maxStock) {
          existingItem.quantity = newQuantity;
        } else {
          existingItem.quantity = existingItem.maxStock;
        }
      } else {
        state.items.push(newItem);
      }

      state.lastUpdated = Date.now();
    },

    removeItem: (state, action: PayloadAction<string>) => {
      state.items = state.items.filter((item: CartItem) => item.id !== action.payload);
      state.lastUpdated = Date.now();
    },

    updateQuantity: (state, action: PayloadAction<{ id: string; quantity: number }>) => {
      const { id, quantity } = action.payload;
      const item = state.items.find((item: CartItem) => item.id === id);

      if (item) {
        if (quantity <= 0) {
          state.items = state.items.filter((item: CartItem) => item.id !== id);
        } else if (quantity <= item.maxStock) {
          item.quantity = quantity;
        } else {
          item.quantity = item.maxStock;
        }
      }

      state.lastUpdated = Date.now();
    },

    clearCart: (state) => {
      state.items = [];
      state.lastUpdated = Date.now();
    },
  },
});

export const { addItem, removeItem, updateQuantity, clearCart } = cartSlice.actions;
export const cartReducer = cartSlice.reducer;

// Selectors
export const selectCartItems = (state: RootState) => state.cart.items;

export const selectCartItemCount = (state: RootState) =>
  state.cart.items.reduce((sum: number, item: CartItem) => sum + item.quantity, 0);

export const selectCartSubtotal = (state: RootState) =>
  state.cart.items.reduce((sum: number, item: CartItem) => sum + item.price * item.quantity, 0);

export const selectCartItemById = (id: string) => (state: RootState) =>
  state.cart.items.find((item: CartItem) => item.id === id);

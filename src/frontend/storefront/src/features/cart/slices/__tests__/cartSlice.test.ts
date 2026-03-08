import { describe, it, expect, beforeEach } from 'vitest';
import { cartReducer, addItem, removeItem, updateQuantity, clearCart } from '../cartSlice';
import type { CartState, CartItem } from '../cartSlice';

describe('cartSlice', () => {
  let initialState: CartState;

  const mockItem: CartItem = {
    id: '1',
    name: 'Test Product',
    slug: 'test-product',
    price: 29.99,
    quantity: 1,
    maxStock: 10,
    image: '/test-image.jpg',
  };

  const mockItem2: CartItem = {
    id: '2',
    name: 'Test Product 2',
    slug: 'test-product-2',
    price: 49.99,
    quantity: 2,
    maxStock: 5,
    image: '/test-image-2.jpg',
    compareAtPrice: 59.99,
  };

  beforeEach(() => {
    initialState = {
      items: [],
      lastUpdated: Date.now(),
    };
  });

  describe('addItem', () => {
    it('should add a new item to an empty cart', () => {
      const state = cartReducer(initialState, addItem(mockItem));

      expect(state.items).toHaveLength(1);
      expect(state.items[0]).toEqual(mockItem);
    });

    it('should increment quantity when adding an existing item', () => {
      const stateWithItem = cartReducer(initialState, addItem(mockItem));
      const state = cartReducer(stateWithItem, addItem({ ...mockItem, quantity: 2 }));

      expect(state.items).toHaveLength(1);
      expect(state.items[0].quantity).toBe(3);
    });

    it('should not exceed maxStock when adding items', () => {
      const stateWithItem = cartReducer(initialState, addItem({ ...mockItem, quantity: 8 }));
      const state = cartReducer(stateWithItem, addItem({ ...mockItem, quantity: 5 }));

      expect(state.items[0].quantity).toBe(mockItem.maxStock);
    });

    it('should add multiple different items', () => {
      const state1 = cartReducer(initialState, addItem(mockItem));
      const state2 = cartReducer(state1, addItem(mockItem2));

      expect(state2.items).toHaveLength(2);
      expect(state2.items[0].id).toBe(mockItem.id);
      expect(state2.items[1].id).toBe(mockItem2.id);
    });

    it('should update lastUpdated timestamp', () => {
      const oldTimestamp = initialState.lastUpdated;
      const state = cartReducer(initialState, addItem(mockItem));

      expect(state.lastUpdated).toBeGreaterThanOrEqual(oldTimestamp);
    });
  });

  describe('removeItem', () => {
    it('should remove an item from the cart', () => {
      const stateWithItem = cartReducer(initialState, addItem(mockItem));
      const state = cartReducer(stateWithItem, removeItem(mockItem.id));

      expect(state.items).toHaveLength(0);
    });

    it('should only remove the specified item', () => {
      const state1 = cartReducer(initialState, addItem(mockItem));
      const state2 = cartReducer(state1, addItem(mockItem2));
      const state3 = cartReducer(state2, removeItem(mockItem.id));

      expect(state3.items).toHaveLength(1);
      expect(state3.items[0].id).toBe(mockItem2.id);
    });

    it('should handle removing non-existent item gracefully', () => {
      const stateWithItem = cartReducer(initialState, addItem(mockItem));
      const state = cartReducer(stateWithItem, removeItem('non-existent-id'));

      expect(state.items).toHaveLength(1);
      expect(state.items[0].id).toBe(mockItem.id);
    });

    it('should update lastUpdated timestamp', () => {
      const stateWithItem = cartReducer(initialState, addItem(mockItem));
      const oldTimestamp = stateWithItem.lastUpdated;
      const state = cartReducer(stateWithItem, removeItem(mockItem.id));

      expect(state.lastUpdated).toBeGreaterThanOrEqual(oldTimestamp);
    });
  });

  describe('updateQuantity', () => {
    beforeEach(() => {
      initialState = {
        items: [{ ...mockItem }],
        lastUpdated: Date.now(),
      };
    });

    it('should update item quantity', () => {
      const state = cartReducer(initialState, updateQuantity({ id: mockItem.id, quantity: 5 }));

      expect(state.items[0].quantity).toBe(5);
    });

    it('should remove item when quantity is set to 0', () => {
      const state = cartReducer(initialState, updateQuantity({ id: mockItem.id, quantity: 0 }));

      expect(state.items).toHaveLength(0);
    });

    it('should remove item when quantity is negative', () => {
      const state = cartReducer(initialState, updateQuantity({ id: mockItem.id, quantity: -1 }));

      expect(state.items).toHaveLength(0);
    });

    it('should not exceed maxStock', () => {
      const state = cartReducer(initialState, updateQuantity({ id: mockItem.id, quantity: 20 }));

      expect(state.items[0].quantity).toBe(mockItem.maxStock);
    });

    it('should handle updating non-existent item gracefully', () => {
      const state = cartReducer(initialState, updateQuantity({ id: 'non-existent', quantity: 5 }));

      expect(state.items).toHaveLength(1);
      expect(state.items[0].quantity).toBe(mockItem.quantity);
    });

    it('should update lastUpdated timestamp', () => {
      const oldTimestamp = initialState.lastUpdated;
      const state = cartReducer(initialState, updateQuantity({ id: mockItem.id, quantity: 3 }));

      expect(state.lastUpdated).toBeGreaterThanOrEqual(oldTimestamp);
    });
  });

  describe('clearCart', () => {
    beforeEach(() => {
      initialState = {
        items: [mockItem, mockItem2],
        lastUpdated: Date.now(),
      };
    });

    it('should remove all items from cart', () => {
      const state = cartReducer(initialState, clearCart());

      expect(state.items).toHaveLength(0);
    });

    it('should update lastUpdated timestamp', () => {
      const oldTimestamp = initialState.lastUpdated;
      const state = cartReducer(initialState, clearCart());

      expect(state.lastUpdated).toBeGreaterThanOrEqual(oldTimestamp);
    });

    it('should work on already empty cart', () => {
      const emptyState: CartState = {
        items: [],
        lastUpdated: Date.now(),
      };
      const state = cartReducer(emptyState, clearCart());

      expect(state.items).toHaveLength(0);
    });
  });
});

/**
 * useCart Hook
 * Manages cart state for the cart page:
 * - Syncs local and backend cart items
 * - Handles display format conversion
 * - Provides update and remove handlers
 * - Calculates cart totals and shipping
 */

import { useState, useEffect, useCallback } from 'react';
import { useAppSelector, useAppDispatch } from '../store/hooks';
import { selectCartItems, updateQuantity, removeItem } from '../store/slices/cartSlice';
import { useGetCartQuery, useUpdateCartItemMutation, useRemoveFromCartMutation } from '../store/api/cartApi';
import { FREE_SHIPPING_THRESHOLD, STANDARD_SHIPPING_COST, DEFAULT_TAX_RATE } from '../utils/constants';
import { useCartSync } from './useCartSync';

interface DisplayCartItem {
  id: string;
  name: string;
  slug: string;
  price: number;
  quantity: number;
  maxStock: number;
  image: string;
  compareAtPrice?: number;
  cartItemId?: string; // Backend cart item ID for updates
}

interface CartTotals {
  subtotal: number;
  shipping: number;
  tax: number;
  total: number;
}

interface UseCartReturn {
  displayItems: DisplayCartItem[];
  totals: CartTotals;
  isLoading: boolean;
  isAuthenticated: boolean;
  handleUpdateQuantity: (id: string, quantity: number) => Promise<void>;
  handleRemove: (id: string) => Promise<void>;
}

export function useCart(): UseCartReturn {
  const dispatch = useAppDispatch();
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const localCartItems = useAppSelector(selectCartItems);
  const [displayItems, setDisplayItems] = useState<DisplayCartItem[]>(localCartItems);
  const [isUpdating, setIsUpdating] = useState(false);

  // Use cart sync hook for backend synchronization
  const { backendCart, isLoading: cartLoading } = useCartSync({ enabled: isAuthenticated });

  // API mutations
  const [updateCartItem] = useUpdateCartItemMutation();
  const [removeFromCart] = useRemoveFromCartMutation();

  // Sync displayed items based on auth state
  useEffect(() => {
    if (isAuthenticated && backendCart?.items) {
      // Convert backend cart items to display format
      const convertedItems: DisplayCartItem[] = backendCart.items.map((item) => ({
        id: item.productId,
        name: item.productName,
        slug: '',
        price: item.price,
        quantity: item.quantity,
        maxStock: 999,
        image: item.imageUrl || '',
        cartItemId: item.cartItemId,
      }));
      setDisplayItems(convertedItems);
    } else {
      setDisplayItems(localCartItems);
    }
  }, [isAuthenticated, backendCart, localCartItems]);

  // Calculate cart totals
  const cartSubtotal = displayItems.reduce((sum, item) => sum + item.price * item.quantity, 0);
  const shipping = cartSubtotal > FREE_SHIPPING_THRESHOLD ? 0 : cartSubtotal > 0 ? STANDARD_SHIPPING_COST : 0;
  const tax = cartSubtotal * DEFAULT_TAX_RATE;
  const total = cartSubtotal + shipping + tax;

  const totals: CartTotals = {
    subtotal: cartSubtotal,
    shipping,
    tax,
    total,
  };

  // Handle quantity updates
  const handleUpdateQuantity = useCallback(
    async (id: string, quantity: number) => {
      if (quantity <= 0) {
        await handleRemove(id);
        return;
      }

      setIsUpdating(true);
      try {
        if (isAuthenticated) {
          const item = displayItems.find((i) => i.id === id);
          if (item?.cartItemId) {
            await updateCartItem({
              cartItemId: item.cartItemId,
              quantity,
            }).unwrap();
          }
        } else {
          dispatch(updateQuantity({ id, quantity }));
        }
      } catch (error) {
        console.error('Failed to update cart item:', error);
        alert('Failed to update item quantity');
      } finally {
        setIsUpdating(false);
      }
    },
    [isAuthenticated, displayItems, updateCartItem, dispatch]
  );

  // Handle item removal
  const handleRemove = useCallback(
    async (id: string) => {
      setIsUpdating(true);
      try {
        if (isAuthenticated) {
          const item = displayItems.find((i) => i.id === id);
          if (item?.cartItemId) {
            await removeFromCart(item.cartItemId).unwrap();
          }
        } else {
          dispatch(removeItem(id));
        }
      } catch (error) {
        console.error('Failed to remove cart item:', error);
        alert('Failed to remove item');
      } finally {
        setIsUpdating(false);
      }
    },
    [isAuthenticated, displayItems, removeFromCart, dispatch]
  );

  return {
    displayItems,
    totals,
    isLoading: cartLoading,
    isAuthenticated,
    handleUpdateQuantity,
    handleRemove,
  };
}

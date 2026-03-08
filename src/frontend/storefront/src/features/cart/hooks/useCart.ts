/**
 * useCart Hook
 * Provides cart functionality:
 * - Display items from local or backend cart
 * - Cart totals calculation
 * - Update quantity and remove item handlers
 */

import { useCallback, useMemo } from 'react';
import { useAppSelector, useAppDispatch } from '@/shared/lib/store';
import {
  selectCartItems,
  selectCartSubtotal,
  updateQuantity,
  removeItem,
} from '../slices/cartSlice';
import { authReducer } from '@/features/auth/slices/authSlice';
import {
  useGetCartQuery,
  useUpdateCartItemMutation,
  useRemoveFromCartMutation,
} from '../api/cartApi';
import { useCartSync } from './useCartSync';
import { calculateOrderTotals } from '@/shared/lib/utils/orderCalculations';
import { useToast } from '@/shared/hooks';

interface DisplayItem {
  id: string; // For authenticated: cartItemId, for guest: productId
  name: string;
  slug: string;
  price: number;
  quantity: number;
  maxStock: number;
  image: string;
}

interface CartTotals {
  subtotal: number;
  shipping: number;
  tax: number;
  total: number;
}

interface UseCartReturn {
  displayItems: DisplayItem[];
  totals: CartTotals;
  isLoading: boolean;
  isAuthenticated: boolean;
  handleUpdateQuantity: (itemId: string, quantity: number) => Promise<void>;
  handleRemove: (itemId: string) => Promise<void>;
}

// Selector for authentication state
const selectIsAuthenticated = (state: { auth: ReturnType<typeof authReducer> }) =>
  state.auth.isAuthenticated;

export function useCart(): UseCartReturn {
  const dispatch = useAppDispatch();
  const { success, error: showError } = useToast();
  const localCartItems = useAppSelector(selectCartItems);
  const localSubtotal = useAppSelector(selectCartSubtotal);
  const isAuthenticated = useAppSelector(selectIsAuthenticated);

  // Backend cart query (only used when authenticated)
  const { data: backendCart, isLoading: isCartLoading } = useGetCartQuery(undefined, {
    skip: !isAuthenticated,
  });

  // Sync local cart with backend on login
  const { isLoading: isSyncLoading } = useCartSync({
    enabled: isAuthenticated,
  });

  // Mutations
  const [updateCartItem] = useUpdateCartItemMutation();
  const [removeFromCartApi] = useRemoveFromCartMutation();

  // Determine which cart to display
  const displayItems: DisplayItem[] = useMemo(() => {
    if (isAuthenticated && backendCart?.items) {
      return backendCart.items.map((item) => ({
        id: item.id, // Backend returns 'id' as the cart item ID
        name: item.productName,
        slug: '', // Not available in CartItemDto
        price: item.price,
        quantity: item.quantity,
        maxStock: 99, // Default max stock
        image: item.productImage || item.imageUrl || '',
      }));
    }
    return localCartItems.map((item) => ({
      id: item.id,
      name: item.name,
      slug: item.slug,
      price: item.price,
      quantity: item.quantity,
      maxStock: item.maxStock,
      image: item.image,
    }));
  }, [isAuthenticated, backendCart, localCartItems]);

  // Calculate subtotal
  const subtotal = useMemo(() => {
    if (isAuthenticated && backendCart?.items) {
      return backendCart.items.reduce((sum, item) => sum + item.price * item.quantity, 0);
    }
    return localSubtotal;
  }, [isAuthenticated, backendCart, localSubtotal]);

  // Calculate totals
  const totals: CartTotals = useMemo(() => {
    const { shipping, tax, total } = calculateOrderTotals(subtotal);
    return { subtotal, shipping, tax, total };
  }, [subtotal]);

  // Handle quantity update
  const handleUpdateQuantity = useCallback(
    async (itemId: string, quantity: number) => {
      if (quantity < 1) return;

      try {
        if (isAuthenticated) {
          // For authenticated users, itemId is the cart item ID
          await updateCartItem({ cartItemId: itemId, quantity }).unwrap();
        } else {
          // For guest users, itemId is the product id
          dispatch(updateQuantity({ id: itemId, quantity }));
        }
        success('Cart updated');
      } catch (err) {
        showError('Failed to update cart');
        throw err;
      }
    },
    [isAuthenticated, updateCartItem, dispatch, success, showError]
  );

  // Handle item removal
  const handleRemove = useCallback(
    async (itemId: string) => {
      try {
        if (isAuthenticated) {
          // For authenticated users, itemId is the cart item ID
          // Find the product ID from backendCart to remove from local cart
          const cartItem = backendCart?.items.find((item) => item.id === itemId);
          const productId = cartItem?.productId;

          await removeFromCartApi(itemId).unwrap();

          // Also remove from local cart to prevent useCartSync from re-adding it
          // Use productId since local cart stores items by product ID
          if (productId) {
            dispatch(removeItem(productId));
          }
        } else {
          // For guest users, itemId is the product id
          dispatch(removeItem(itemId));
        }
        success('Item removed from cart');
      } catch (err) {
        showError('Failed to remove item');
        throw err;
      }
    },
    [isAuthenticated, removeFromCartApi, dispatch, backendCart, success, showError]
  );

  return {
    displayItems,
    totals,
    isLoading: isCartLoading || isSyncLoading,
    isAuthenticated,
    handleUpdateQuantity,
    handleRemove,
  };
}

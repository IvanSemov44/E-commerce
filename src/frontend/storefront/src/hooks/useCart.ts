/**
 * useCart Hook
 * Provides cart functionality:
 * - Display items from local or backend cart
 * - Cart totals calculation
 * - Update quantity and remove item handlers
 */

import { useCallback, useMemo } from 'react';
import { useAppSelector, useAppDispatch } from '../store/hooks';
import {
  selectCartItems,
  selectCartSubtotal,
  updateQuantity,
  removeItem
} from '../store/slices/cartSlice';
import { authReducer } from '../store/slices/authSlice';
import {
  useGetCartQuery,
  useUpdateCartItemMutation,
  useRemoveFromCartMutation
} from '../store/api/cartApi';
import { useCartSync } from './useCartSync';
import { FREE_SHIPPING_THRESHOLD, STANDARD_SHIPPING_COST, DEFAULT_TAX_RATE } from '../utils/constants';
import toast from 'react-hot-toast';

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
  const localCartItems = useAppSelector(selectCartItems);
  const localSubtotal = useAppSelector(selectCartSubtotal);
  const isAuthenticated = useAppSelector(selectIsAuthenticated);

  // Backend cart query (only used when authenticated)
  const { data: backendCart, isLoading: isCartLoading } = useGetCartQuery(undefined, {
    skip: !isAuthenticated
  });

  // Sync local cart with backend on login
  const { isLoading: isSyncLoading } = useCartSync({
    enabled: isAuthenticated
  });

  // Mutations
  const [updateCartItem] = useUpdateCartItemMutation();
  const [removeFromCartApi] = useRemoveFromCartMutation();

  // Determine which cart to display
  const displayItems: DisplayItem[] = useMemo(() => {
    if (isAuthenticated && backendCart?.items) {
      return backendCart.items.map(item => ({
        id: item.id, // Backend returns 'id' as the cart item ID
        name: item.productName,
        slug: '', // Not available in CartItemDto
        price: item.price,
        quantity: item.quantity,
        maxStock: 99, // Default max stock
        image: item.productImage || item.imageUrl || ''
      }));
    }
    return localCartItems.map(item => ({
      id: item.id,
      name: item.name,
      slug: item.slug,
      price: item.price,
      quantity: item.quantity,
      maxStock: item.maxStock,
      image: item.image
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
    const shipping = subtotal >= FREE_SHIPPING_THRESHOLD || subtotal === 0 ? 0 : STANDARD_SHIPPING_COST;
    const tax = subtotal * DEFAULT_TAX_RATE;
    const total = subtotal + shipping + tax;

    return {
      subtotal,
      shipping,
      tax,
      total
    };
  }, [subtotal]);

  // Handle quantity update
  const handleUpdateQuantity = useCallback(async (itemId: string, quantity: number) => {
    if (quantity < 1) return;

    try {
      if (isAuthenticated) {
        // For authenticated users, itemId is the cart item ID
        await updateCartItem({ cartItemId: itemId, quantity }).unwrap();
      } else {
        // For guest users, itemId is the product id
        dispatch(updateQuantity({ id: itemId, quantity }));
      }
      toast.success('Cart updated');
    } catch (error) {
      toast.error('Failed to update cart');
      throw error;
    }
  }, [isAuthenticated, updateCartItem, dispatch]);

  // Handle item removal
  const handleRemove = useCallback(async (itemId: string) => {
    try {
      if (isAuthenticated) {
        // For authenticated users, itemId is the cart item ID
        await removeFromCartApi(itemId).unwrap();
      } else {
        // For guest users, itemId is the product id
        dispatch(removeItem(itemId));
      }
      toast.success('Item removed from cart');
    } catch (error) {
      toast.error('Failed to remove item');
      throw error;
    }
  }, [isAuthenticated, removeFromCartApi, dispatch]);

  return {
    displayItems,
    totals,
    isLoading: isCartLoading || isSyncLoading,
    isAuthenticated,
    handleUpdateQuantity,
    handleRemove
  };
}

/**
 * useCheckoutCart Hook
 * Manages cart state - handles dual cart strategy (local Redux vs backend RTK Query)
 * and subtotal calculation
 */

import { useMemo } from 'react';
import { useAppSelector } from '@/shared/lib/store';
import { selectIsAuthenticated } from '@/features/auth/slices/authSlice';
import { selectCartItems, selectCartSubtotal } from '@/features/cart/slices/cartSlice';
import type { CartItem } from '@/features/cart/slices/cartSlice';
import { useGetCartQuery } from '@/features/cart/api';
import { useCartSync } from '@/features/cart/hooks/useCartSync';

interface UseCheckoutCartReturn {
  cartItems: CartItem[];
  subtotal: number;
  isLoading: boolean;
}

export function useCheckoutCart(): UseCheckoutCartReturn {
  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  const localCartItems = useAppSelector(selectCartItems);
  const localSubtotal = useAppSelector(selectCartSubtotal);

  // Backend cart query (only used when authenticated)
  const { data: backendCart, isLoading: isBackendCartLoading } = useGetCartQuery(undefined, {
    skip: !isAuthenticated,
  });

  // Sync local cart with backend on login
  useCartSync({
    enabled: isAuthenticated,
  });

  // Determine which cart items to use (backend for authenticated, local for guest)
  const cartItems: CartItem[] = useMemo(() => {
    if (isAuthenticated && backendCart?.items) {
      return backendCart.items.map((item) => ({
        id: item.productId,
        name: item.productName,
        slug: '', // not provided by CartItemDto
        price: item.price,
        quantity: item.quantity,
        maxStock: 99, // Backend cart API does not return stock; 99 is a safe display fallback
        image: item.productImage || item.imageUrl || '',
      }));
    }
    return localCartItems;
  }, [isAuthenticated, backendCart, localCartItems]);

  // Calculate subtotal
  const subtotal = useMemo(() => {
    if (isAuthenticated && backendCart?.items) {
      return backendCart.items.reduce((sum, item) => sum + item.price * item.quantity, 0);
    }
    return localSubtotal;
  }, [isAuthenticated, backendCart, localSubtotal]);

  return {
    cartItems,
    subtotal,
    isLoading: isAuthenticated && isBackendCartLoading,
  };
}

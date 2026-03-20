import { useMemo } from 'react';
import { useAppSelector } from '@/shared/lib/store';
import { selectCartItems, selectCartSubtotal } from '../slices/cartSlice';
import { authReducer } from '@/features/auth/slices/authSlice';
import { useGetCartQuery } from '../api/cartApi';
import { useCartSync } from './useCartSync';
import { calculateOrderTotals } from '@/shared/lib/utils/orderCalculations';

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
}

// Selector for authentication state
const selectIsAuthenticated = (state: { auth: ReturnType<typeof authReducer> }) =>
  state.auth.isAuthenticated;

export function useCart(): UseCartReturn {
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
        image: item.productImage ?? item.imageUrl ?? '',
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

  return {
    displayItems,
    totals,
    isLoading: isCartLoading || isSyncLoading,
    isAuthenticated,
  };
}

import { useMemo } from 'react';
import { useAppSelector } from '@/shared/lib/store';
import type { RootState } from '@/shared/lib/store';
import { selectCartItems, selectCartSubtotal } from '../slices/cartSlice';
import { useGetCartQuery } from '../api/cartApi';
import { calculateOrderTotals } from '@/shared/lib/utils/orderCalculations';
import type { CartItem } from '../types';

const EMPTY_ITEMS: CartItem[] = [];

interface CartTotals {
  subtotal: number;
  shipping: number;
  tax: number;
  total: number;
}

interface UseCartReturn {
  displayItems: CartItem[];
  totals: CartTotals;
  isLoading: boolean;
  isAuthenticated: boolean;
}

export function useCart(): UseCartReturn {
  const isAuthenticated = useAppSelector((state: RootState) => state.auth.isAuthenticated);

  // Only select local cart when guest (avoid unnecessary updates when authenticated)
  const localCartItems = useAppSelector((state: RootState) =>
    isAuthenticated ? EMPTY_ITEMS : selectCartItems(state)
  );
  const localSubtotal = useAppSelector((state: RootState) =>
    isAuthenticated ? 0 : selectCartSubtotal(state)
  );

  // Backend cart query (only used when authenticated)
  const { data: backendCart, isLoading: isCartLoading } = useGetCartQuery(undefined, {
    skip: !isAuthenticated,
  });

  const { displayItems, totals } = useMemo(() => {
    if (isAuthenticated && backendCart) {
      const items: CartItem[] = backendCart.items.map((item) => ({
        id: item.id,
        name: item.productName,
        slug: '', // TODO: Backend should provide product slug for navigation
        price: item.price,
        quantity: item.quantity,
        maxStock: 999, // Backend doesn't provide max stock; use high default
        image: item.productImage ?? item.imageUrl ?? '',
      }));
      const subtotal = backendCart.subtotal;
      const { shipping, tax, total } = calculateOrderTotals(subtotal);
      return { displayItems: items, totals: { subtotal, shipping, tax, total } };
    }

    const subtotal = localSubtotal;
    const { shipping, tax, total } = calculateOrderTotals(subtotal);
    return {
      displayItems: localCartItems,
      totals: { subtotal, shipping, tax, total },
    };
  }, [isAuthenticated, backendCart, localCartItems, localSubtotal]);

  return {
    displayItems,
    totals,
    isLoading: isCartLoading,
    isAuthenticated,
  };
}

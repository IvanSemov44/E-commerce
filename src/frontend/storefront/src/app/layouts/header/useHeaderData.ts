import { useMemo } from 'react';
import { useAppSelector } from '@/shared/lib/store';
import { selectCartItemCount } from '@/features/cart/slices/cartSlice';
import { useGetCartQuery } from '@/features/cart/api/cartApi';
import { useGetWishlistQuery } from '@/features/wishlist/api/wishlistApi';

export function useHeaderData(isAuthenticated: boolean) {
  const localCartItemCount = useAppSelector(selectCartItemCount);

  const { data: backendCart } = useGetCartQuery(undefined, {
    skip: !isAuthenticated,
  });

  const { data: wishlistData } = useGetWishlistQuery(undefined, {
    skip: !isAuthenticated,
  });

  const cartItemCount = useMemo(() => {
    if (isAuthenticated && backendCart?.items) {
      return backendCart.items.reduce((sum, item) => sum + item.quantity, 0);
    }

    return localCartItemCount;
  }, [isAuthenticated, backendCart, localCartItemCount]);

  const wishlistItemCount = useMemo(() => wishlistData?.items?.length ?? 0, [wishlistData]);

  return {
    cartItemCount,
    wishlistItemCount,
  };
}

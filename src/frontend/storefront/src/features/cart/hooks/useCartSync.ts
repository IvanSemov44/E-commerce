/**
 * useCartSync Hook
 * Synchronizes local cart with backend cart when user authenticates
 * Handles merging guest cart items with authenticated user's server cart
 */

import { useEffect, useRef } from 'react';
import { useAppDispatch, useAppSelector } from '@/shared/lib/store';
import { selectCartItems, clearCart } from '../slices/cartSlice';
import { useGetCartQuery, useAddToCartMutation } from '../api/cartApi';
import { logger } from '@/shared/lib/utils/logger';
import type { AddToCartRequest } from '@/shared/types';

interface UseCartSyncOptions {
  enabled?: boolean;
}

export function useCartSync(options: UseCartSyncOptions = {}) {
  const { enabled = true } = options;
  const dispatch = useAppDispatch();
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const localCartItems = useAppSelector(selectCartItems);
  // Ref so the sync always reads current local items without needing them as a dep.
  // This prevents the effect from re-running when clearCart() empties the local cart.
  const localCartItemsRef = useRef(localCartItems);
  localCartItemsRef.current = localCartItems;
  const syncInProgressRef = useRef(false);

  // Backend cart query - httpOnly cookies handle authentication
  const {
    data: backendCart,
    isLoading: cartLoading,
    refetch: refetchCart,
  } = useGetCartQuery(undefined, {
    skip: !isAuthenticated || !enabled,
  });

  // Add to cart mutation
  const [addToCart] = useAddToCartMutation();

  /**
   * Sync local cart items to backend when user logs in
   */
  useEffect(() => {
    if (!enabled || !isAuthenticated || !backendCart || cartLoading || syncInProgressRef.current) {
      return;
    }

    const syncCart = async () => {
      // Prevent multiple simultaneous syncs
      if (syncInProgressRef.current) return;
      syncInProgressRef.current = true;

      try {
        // Get items from local cart that aren't in backend cart
        // Note: local items use 'id' for productId, backend items use 'productId' field
        const backendProductIds = new Set((backendCart.items ?? []).map((item) => item.productId));
        const itemsToSync = localCartItemsRef.current.filter(
          (item) => !backendProductIds.has(item.id)
        );

        let syncedCount = 0;
        const failedItems: string[] = [];

        // Sync each local item that isn't already in the backend
        for (const item of itemsToSync) {
          try {
            const syncPayload: AddToCartRequest = {
              productId: item.id,
              quantity: item.quantity,
            };

            await addToCart(syncPayload).unwrap();
            syncedCount++;
          } catch (error: unknown) {
            logger.warn('useCartSync', `Failed to sync item ${item.name} (${item.id})`, error);
            failedItems.push(item.name);
          }
        }

        // Refetch cart to get latest state if any items were synced
        if (syncedCount > 0) {
          await refetchCart();
        }

        // Notify user if items were removed
        if (failedItems.length > 0) {
          logger.info(
            'useCartSync',
            `Removed ${failedItems.length} unavailable item(s) from cart: ${failedItems.join(', ')}`
          );
        }
      } catch (error) {
        logger.error('useCartSync', 'Cart sync failed', error);
      } finally {
        // Backend is always source of truth — clear local cart even if sync partially failed
        dispatch(clearCart());
        syncInProgressRef.current = false;
      }
    };

    syncCart();
     
    // reading it via ref prevents the effect from re-running when clearCart() empties the cart,
    // which would otherwise create an infinite sync loop.
  }, [enabled, isAuthenticated, backendCart, cartLoading, addToCart, refetchCart, dispatch]);

  return {
    isLoading: cartLoading,
  };
}

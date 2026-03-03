/**
 * useCartSync Hook
 * Synchronizes local cart with backend cart when user authenticates
 * Handles merging guest cart items with authenticated user's server cart
 */

import { useEffect, useRef } from 'react';
import { useAppDispatch, useAppSelector } from '@/shared/lib/store';
import { selectCartItems, removeItem } from '../slices/cartSlice';
import {
  useGetCartQuery,
  useAddToCartMutation,
} from '../api/cartApi';
import { useErrorHandler } from '@/shared/hooks/useErrorHandler';
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
  const { handleError } = useErrorHandler();
  const syncInProgressRef = useRef(false);

  // Backend cart query - httpOnly cookies handle authentication
  const {
    data: backendCart,
    isLoading: cartLoading,
    error: cartError,
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
    if (
      !enabled ||
      !isAuthenticated ||
      !backendCart ||
      cartLoading ||
      syncInProgressRef.current
    ) {
      return;
    }

    const syncCart = async () => {
      // Prevent multiple simultaneous syncs
      if (syncInProgressRef.current) return;
      syncInProgressRef.current = true;

      try {
        // Get items from local cart that aren't in backend cart
        const backendProductIds = new Set(
          backendCart.items.map((item) => item.productId)
        );
        const itemsToSync = localCartItems.filter(
          (item) => !backendProductIds.has(item.id)
        );

        if (itemsToSync.length === 0) {
          syncInProgressRef.current = false;
          return;
        }

        let syncedCount = 0;
        const failedItems: string[] = [];

        // Sync each local item to backend individually
        for (const item of itemsToSync) {
          try {
            const syncPayload: AddToCartRequest = {
              productId: item.id,
              quantity: item.quantity,
            };

            await addToCart(syncPayload).unwrap();
            syncedCount++;
          } catch (error: unknown) {
            // Product not found or other error - remove from local cart
            logger.warn('useCartSync', `Failed to sync item ${item.name} (${item.id})`, error);
            handleError(error);
            dispatch(removeItem(item.id));
            failedItems.push(item.name);
          }
        }

        // Refetch cart to get latest state if any items were synced
        if (syncedCount > 0) {
          await refetchCart();
        }

        // Notify user if items were removed
        if (failedItems.length > 0) {
          logger.info('useCartSync', `Removed ${failedItems.length} unavailable item(s) from cart: ${failedItems.join(', ')}`);
        }
      } catch (error) {
        logger.error('useCartSync', 'Cart sync failed', error);
        handleError(error);
      } finally {
        syncInProgressRef.current = false;
      }
    };

    syncCart();
  }, [
    enabled,
    isAuthenticated,
    backendCart,
    cartLoading,
    localCartItems,
    addToCart,
    refetchCart,
    dispatch,
    handleError,
  ]);

  return {
    backendCart,
    isLoading: cartLoading,
    error: cartError,
    isSyncing: syncInProgressRef.current,
    refetch: refetchCart,
  };
}

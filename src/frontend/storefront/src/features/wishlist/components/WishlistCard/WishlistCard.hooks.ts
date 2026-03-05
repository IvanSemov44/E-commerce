/**
 * WishlistCard Hooks
 * Component-specific hooks for WishlistCard functionality
 */

import { useTranslation } from 'react-i18next';
import { useRemoveFromWishlistMutation } from '../../api/wishlistApi';
import { useAddToCartMutation } from '@/features/cart/api/cartApi';
import { useApiErrorHandler } from '@/shared/hooks';

/**
 * Hook for handling wishlist item removal
 * @param productId - Product ID to remove
 */
export function useWishlistRemove(productId: string) {
  const { t } = useTranslation();
  const [removeFromWishlist] = useRemoveFromWishlistMutation();
  const { handleError } = useApiErrorHandler();

  return async () => {
    try {
      await removeFromWishlist(productId).unwrap();
    } catch (err) {
      handleError(err, t('common.errorOccurred'));
    }
  };
}

/**
 * Hook for adding wishlist item to cart
 * @param productId - Product ID to add to cart
 */
export function useWishlistAddToCart(productId: string) {
  const { t } = useTranslation();
  const [addToCart] = useAddToCartMutation();
  const { handleError } = useApiErrorHandler();

  return async () => {
    try {
      await addToCart({ productId, quantity: 1 }).unwrap();
    } catch (err) {
      handleError(err, t('common.errorOccurred'));
    }
  };
}

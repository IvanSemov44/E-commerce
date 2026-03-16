/**
 * ProductCard Hooks
 * Component-specific hooks for ProductCard functionality
 */

import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useAppDispatch, useAppSelector } from '@/shared/lib/store';
import { useApiErrorHandler, useToast } from '@/shared/hooks';
import { addItem, type CartItem } from '@/features/cart/slices/cartSlice';
import { useAddToWishlistMutation, useRemoveFromWishlistMutation } from '@/features/wishlist/api';
import { useAddToCartMutation } from '@/features/cart/api';
import { DEFAULT_PRODUCT_IMAGE } from '@/shared/lib/utils/constants';
import { QUICK_ADD_RESET_MS } from '@/features/products/constants';

interface UseWishlistToggleParams {
  id: string;
  isInWishlist: boolean;
}

/**
 * Hook for handling wishlist toggle functionality
 */
export function useWishlistToggle({ id, isInWishlist }: UseWishlistToggleParams) {
  const { t } = useTranslation();
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const [addToWishlist, { isLoading: isAdding }] = useAddToWishlistMutation();
  const [removeFromWishlist, { isLoading: isRemoving }] = useRemoveFromWishlistMutation();
  const { handleError } = useApiErrorHandler();
  const { success, error } = useToast();

  const isWishlistLoading = isAdding || isRemoving;

  const handleWishlistToggle = useCallback(
    async (event: React.MouseEvent) => {
      event.preventDefault();
      event.stopPropagation();

      if (!isAuthenticated) {
        error(t('wishlist.signInRequired'));
        return;
      }

      if (isWishlistLoading) return;

      try {
        if (isInWishlist) {
          await removeFromWishlist(id).unwrap();
          success(t('wishlist.removedFromWishlist'));
        } else {
          await addToWishlist(id).unwrap();
          success(t('wishlist.addedToWishlist'));
        }
      } catch (err) {
        handleError(err, 'Failed to update wishlist');
      }
    },
    [
      t,
      id,
      isAuthenticated,
      isInWishlist,
      isWishlistLoading,
      addToWishlist,
      removeFromWishlist,
      handleError,
      success,
      error,
    ]
  );

  return { handleWishlistToggle, isWishlistLoading };
}

interface UseAddToCartParams {
  id: string;
  name: string;
  slug: string;
  price: number;
  imageUrl: string;
  stockQuantity: number;
  isInStock: boolean;
  setIsAddingToCart: (value: boolean) => void;
}

/**
 * Hook for handling add to cart functionality
 */
export function useAddToCart({
  id,
  name,
  slug,
  price,
  imageUrl,
  stockQuantity,
  isInStock,
  setIsAddingToCart,
}: UseAddToCartParams) {
  const { t } = useTranslation();
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const dispatch = useAppDispatch();
  const [addToCartBackend] = useAddToCartMutation();
  const { handleError } = useApiErrorHandler();
  const { success, error } = useToast();

  const handleAddToCart = useCallback(
    async (event: React.MouseEvent) => {
      event.preventDefault();
      event.stopPropagation();

      if (!isInStock) {
        error(t('common.outOfStock'));
        return;
      }

      setIsAddingToCart(true);

      try {
        if (isAuthenticated) {
          await addToCartBackend({ productId: id, quantity: 1 }).unwrap();
        } else {
          const cartItem: CartItem = {
            id,
            name,
            slug,
            price,
            quantity: 1,
            maxStock: stockQuantity,
            image: imageUrl || DEFAULT_PRODUCT_IMAGE,
          };
          dispatch(addItem(cartItem));
        }
        success(t('products.addedToCartSuccess'));
      } catch (err) {
        handleError(err, t('products.addToCartError'));
      } finally {
        setTimeout(() => setIsAddingToCart(false), QUICK_ADD_RESET_MS);
      }
    },
    [
      t,
      id,
      name,
      slug,
      price,
      imageUrl,
      stockQuantity,
      isInStock,
      isAuthenticated,
      addToCartBackend,
      dispatch,
      setIsAddingToCart,
      handleError,
      success,
      error,
    ]
  );

  return { handleAddToCart };
}

/**
 * Hook for handling image error state
 */
export function useImageError(setImageError: (value: boolean) => void) {
  const handleImageError = useCallback(() => {
    setImageError(true);
  }, [setImageError]);

  return { handleImageError };
}

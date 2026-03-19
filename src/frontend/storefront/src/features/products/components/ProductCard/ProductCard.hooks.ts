import { useCallback, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useAppDispatch, useAppSelector } from '@/shared/lib/store';
import { useApiErrorHandler, useToast } from '@/shared/hooks';
import { addItem, type CartItem } from '@/features/cart/slices/cartSlice';
import {
  useGetWishlistQuery,
  useAddToWishlistMutation,
  useRemoveFromWishlistMutation,
} from '@/features/wishlist/api';
import { useAddToCartMutation } from '@/features/cart/api';
import { DEFAULT_PRODUCT_IMAGE } from '@/shared/lib/utils/constants';
import { QUICK_ADD_RESET_MS } from '@/features/products/constants';

export function useWishlistToggle(id: string) {
  const { t } = useTranslation();
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const { data: wishlist } = useGetWishlistQuery(undefined, { skip: !isAuthenticated });
  const isInWishlist = wishlist?.items.some((item) => item.productId === id) ?? false;
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
        handleError(err, t('wishlist.updateError'));
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

  return { handleWishlistToggle, isWishlistLoading, isInWishlist, isAuthenticated };
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
  const resetTimerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  useEffect(() => () => clearTimeout(resetTimerRef.current), []);

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
        resetTimerRef.current = setTimeout(() => setIsAddingToCart(false), QUICK_ADD_RESET_MS);
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

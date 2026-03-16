import { useState, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useAppDispatch, useAppSelector } from '@/shared/lib/store';
import { useToast } from '@/shared/hooks';
import { addItem } from '@/features/cart/slices/cartSlice';
import { selectCartItemById } from '@/features/cart/slices/cartSlice';
import {
  useGetWishlistQuery,
  useAddToWishlistMutation,
  useRemoveFromWishlistMutation,
} from '@/features/wishlist/api';
import { useAddToCartMutation } from '@/features/cart/api';
import { DEFAULT_PRODUCT_IMAGE } from '@/shared/lib/utils/constants';
import { ADDED_TO_CART_RESET_MS } from '@/features/products/constants';
import { logger } from '@/shared/lib/utils/logger';

// ---------------------------------------------------------------------------
// useWishlistActions
// ---------------------------------------------------------------------------

export function useWishlistActions(productId: string | undefined) {
  const { t } = useTranslation();
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const { data: wishlist } = useGetWishlistQuery(undefined, { skip: !isAuthenticated });
  const [addToWishlist, { isLoading: isAdding }] = useAddToWishlistMutation();
  const [removeFromWishlist, { isLoading: isRemoving }] = useRemoveFromWishlistMutation();
  const { success } = useToast();

  const isInWishlist = wishlist?.items.some((item) => item.productId === productId) ?? false;

  const toggleWishlist = async () => {
    if (!productId || !isAuthenticated) return;
    try {
      if (isInWishlist) {
        await removeFromWishlist(productId).unwrap();
        success(t('wishlist.removedFromWishlist'));
      } else {
        await addToWishlist(productId).unwrap();
        success(t('wishlist.addedToWishlist'));
      }
    } catch {
      // mutation error state handled by isAdding / isRemoving
    }
  };

  return { isInWishlist, toggleWishlist, isAdding, isRemoving };
}

// ---------------------------------------------------------------------------
// useCartActions
// ---------------------------------------------------------------------------

interface ProductForCart {
  id: string;
  name: string;
  slug: string;
  price: number;
  compareAtPrice?: number;
  stockQuantity: number;
  images: { url: string }[];
}

export function useCartActions(product: ProductForCart | undefined) {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const cartItem = useAppSelector(product ? selectCartItemById(product.id) : () => undefined);
  const [addToCartBackend, { isLoading: isAdding }] = useAddToCartMutation();

  const [quantity, setQuantity] = useState(1);
  const [addedToCart, setAddedToCart] = useState(false);
  const [cartError, setCartError] = useState<string | null>(null);
  const resetTimerRef = useRef<ReturnType<typeof setTimeout>>();

  // Cleanup timer on unmount
  useEffect(() => () => clearTimeout(resetTimerRef.current), []);

  const dismissCartError = () => setCartError(null);

  const addToCart = async () => {
    if (!product) return;

    setCartError(null);
    const currentInCart = cartItem?.quantity ?? 0;
    const totalQuantity = currentInCart + quantity;

    if (totalQuantity > product.stockQuantity) {
      setCartError(
        t('products.stockLimitError', {
          available: product.stockQuantity,
          inCart: currentInCart,
        })
      );
      return;
    }

    try {
      dispatch(
        addItem({
          id: product.id,
          name: product.name,
          slug: product.slug,
          price: product.price,
          quantity,
          maxStock: product.stockQuantity,
          image: product.images[0]?.url || DEFAULT_PRODUCT_IMAGE,
          compareAtPrice: product.compareAtPrice,
        })
      );

      if (isAuthenticated) {
        await addToCartBackend({ productId: product.id, quantity }).unwrap();
      }

      setAddedToCart(true);
      clearTimeout(resetTimerRef.current);
      resetTimerRef.current = setTimeout(() => setAddedToCart(false), ADDED_TO_CART_RESET_MS);
      setQuantity(1);
    } catch (error: unknown) {
      logger.error('useCartActions', 'Failed to add to cart', error);
      const err = error as { data?: { message?: string }; message?: string };
      setCartError(err?.data?.message ?? err?.message ?? t('products.addToCartError'));
    }
  };

  return { quantity, setQuantity, addedToCart, cartError, dismissCartError, addToCart, isAdding };
}

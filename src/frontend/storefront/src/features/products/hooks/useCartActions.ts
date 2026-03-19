import { useState, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useAppDispatch, useAppSelector } from '@/shared/lib/store';
import { addItem, selectCartItemById } from '@/features/cart/slices/cartSlice';
import { useAddToCartMutation } from '@/features/cart/api';
import { DEFAULT_PRODUCT_IMAGE } from '@/shared/lib/utils/constants';
import { ADDED_TO_CART_RESET_MS } from '@/features/products/constants';
import { logger } from '@/shared/lib/utils/logger';

export interface ProductForCart {
  id: string;
  name: string;
  slug: string;
  price: number;
  compareAtPrice?: number;
  stockQuantity: number;
  images: { url: string }[];
}

export function useCartActions(product: ProductForCart) {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const cartItem = useAppSelector(selectCartItemById(product.id));
  const [addToCartBackend, { isLoading: isAdding }] = useAddToCartMutation();

  const [quantity, setQuantity] = useState(1);
  const [addedToCart, setAddedToCart] = useState(false);
  const [cartError, setCartError] = useState<string | null>(null);
  const resetTimerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  useEffect(() => () => clearTimeout(resetTimerRef.current), []);

  const dismissCartError = () => setCartError(null);
  const isInStock = product.stockQuantity > 0;

  const addToCart = async () => {
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

  return {
    quantity,
    setQuantity,
    addedToCart,
    cartError,
    dismissCartError,
    addToCart,
    isAdding,
    isInStock,
  };
}

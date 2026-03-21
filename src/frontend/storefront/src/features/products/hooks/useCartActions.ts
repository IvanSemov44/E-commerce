import { useState, useEffect, useRef, useTransition } from 'react';
import { useTranslation } from 'react-i18next';
import { useAppSelector } from '@/shared/lib/store';
import { selectCartItemById } from '@/features/cart/slices/cartSlice';
import { useCartOperations } from '@/features/cart/hooks';
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
  const { add } = useCartOperations();
  const isAuthenticated = useAppSelector((s) => s.auth.isAuthenticated);
  // Only read local Redux cart for guests — authenticated cart lives on the backend
  const localCartItem = useAppSelector((state) =>
    isAuthenticated ? undefined : selectCartItemById(product.id)(state)
  );

  const [quantity, setQuantity] = useState(1);
  const [addedToCart, setAddedToCart] = useState(false);
  const [cartError, setCartError] = useState<string | null>(null);
  const resetTimerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  // React 19: useTransition tracks the async add operation;
  // isPending stays true until the promise resolves (no manual isAdding state needed)
  const [isAdding, startTransition] = useTransition();

  useEffect(() => () => clearTimeout(resetTimerRef.current), []);

  const dismissCartError = () => setCartError(null);
  const isInStock = product.stockQuantity > 0;

  const addToCart = () => {
    startTransition(async () => {
      setCartError(null);

      // Guest: validate stock client-side (backend validates for authenticated users)
      if (!isAuthenticated) {
        const currentInCart = localCartItem?.quantity ?? 0;
        if (currentInCart + quantity > product.stockQuantity) {
          setCartError(
            t('products.stockLimitError', {
              available: product.stockQuantity,
              inCart: currentInCart,
            })
          );
          return;
        }
      }

      try {
        await add({
          id: product.id,
          name: product.name,
          slug: product.slug,
          price: product.price,
          quantity,
          maxStock: product.stockQuantity,
          image: product.images[0]?.url || DEFAULT_PRODUCT_IMAGE,
          compareAtPrice: product.compareAtPrice,
        });

        setAddedToCart(true);
        clearTimeout(resetTimerRef.current);
        resetTimerRef.current = setTimeout(() => setAddedToCart(false), ADDED_TO_CART_RESET_MS);
        setQuantity(1);
      } catch (error: unknown) {
        logger.error('useCartActions', 'Failed to add to cart', error);
        const err = error as { data?: { message?: string }; message?: string };
        setCartError(err?.data?.message ?? err?.message ?? t('products.addToCartError'));
      }
    });
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
    inCartQuantity: localCartItem?.quantity,
  };
}

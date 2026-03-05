/**
 * ProductCard Hooks
 * Component-specific hooks for ProductCard functionality
 */

import { useCallback } from 'react';
import toast from 'react-hot-toast';
import { useAppDispatch } from '@/shared/lib/store';
import { useApiErrorHandler } from '@/shared/hooks';
import { addItem, type CartItem } from '@/features/cart/slices/cartSlice';
import {
  useAddToWishlistMutation,
  useRemoveFromWishlistMutation,
} from '@/features/wishlist/api';
import { useAddToCartMutation } from '@/features/cart/api';
import { DEFAULT_PRODUCT_IMAGE } from './ProductCard.types';

interface UseWishlistToggleParams {
  id: string;
  isAuthenticated: boolean;
  isInWishlist: boolean;
  isWishlistLoading: boolean;
  refetchWishlist: () => void;
}

/**
 * Hook for handling wishlist toggle functionality
 */
export function useWishlistToggle({
  id,
  isAuthenticated,
  isInWishlist,
  isWishlistLoading,
  refetchWishlist,
}: UseWishlistToggleParams) {
  const [addToWishlist] = useAddToWishlistMutation();
  const [removeFromWishlist] = useRemoveFromWishlistMutation();
  const { handleError } = useApiErrorHandler();

  const handleWishlistToggle = useCallback(
    async (event: React.MouseEvent) => {
      event.preventDefault();
      event.stopPropagation();

      if (!isAuthenticated) {
        toast.error('Please sign in to add items to your wishlist');
        return;
      }

      if (isWishlistLoading) return;

      try {
        if (isInWishlist) {
          await removeFromWishlist(id).unwrap();
          toast.success('Removed from wishlist');
        } else {
          await addToWishlist(id).unwrap();
          toast.success('Added to wishlist');
        }
        refetchWishlist();
      } catch (err) {
        handleError(err, 'Failed to update wishlist');
      }
    },
    [id, isAuthenticated, isInWishlist, isWishlistLoading, addToWishlist, removeFromWishlist, refetchWishlist, handleError]
  );

  return { handleWishlistToggle };
}

interface UseAddToCartParams {
  id: string;
  name: string;
  slug: string;
  price: number;
  imageUrl: string;
  stockQuantity: number;
  isInStock: boolean;
  isAuthenticated: boolean;
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
  isAuthenticated,
  setIsAddingToCart,
}: UseAddToCartParams) {
  const dispatch = useAppDispatch();
  const [addToCartBackend] = useAddToCartMutation();
  const { handleError } = useApiErrorHandler();

  const handleAddToCart = useCallback(
    async (event: React.MouseEvent) => {
      event.preventDefault();
      event.stopPropagation();

      if (!isInStock) {
        toast.error('This item is out of stock');
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
        toast.success('Added to cart!', { icon: '🛒' });
      } catch (err) {
        handleError(err, 'Failed to add to cart');
      } finally {
        setTimeout(() => setIsAddingToCart(false), 300);
      }
    },
    [id, name, slug, price, imageUrl, stockQuantity, isInStock, isAuthenticated, addToCartBackend, dispatch, setIsAddingToCart, handleError]
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

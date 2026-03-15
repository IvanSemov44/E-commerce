import { useState } from 'react';
import { useGetProductBySlugQuery, useGetProductReviewsQuery } from '@/features/products/api';
import {
  useAddToWishlistMutation,
  useRemoveFromWishlistMutation,
  useGetWishlistQuery,
} from '@/features/wishlist/api';
import { useAddToCartMutation } from '@/features/cart/api';
import { useAppSelector, useAppDispatch } from '@/shared/lib/store';
import { addItem, selectCartItemById } from '@/features/cart/slices/cartSlice';
import { DEFAULT_PRODUCT_IMAGE } from '@/shared/lib/utils/constants';
import { logger } from '@/shared/lib/utils/logger';

export function useProductDetails(slug: string) {
  const dispatch = useAppDispatch();
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const [quantity, setQuantity] = useState(1);
  const [addedToCart, setAddedToCart] = useState(false);
  const [cartError, setCartError] = useState<string | null>(null);

  // Product query
  const {
    data: product,
    isLoading: productLoading,
    error: productError,
  } = useGetProductBySlugQuery(slug);

  // Reviews query
  const {
    data: reviews,
    isLoading: reviewsLoading,
    error: reviewsError,
    refetch: refetchReviews,
  } = useGetProductReviewsQuery(product?.id || '', {
    skip: !product?.id,
  });

  // Wishlist state — derive from cached full wishlist (avoids redundant per-product request)
  const { data: wishlist } = useGetWishlistQuery(undefined, { skip: !isAuthenticated });
  const isInWishlist = wishlist?.items.some((item) => item.productId === product?.id) ?? false;

  // Mutations
  const [addToWishlist, { isLoading: addingToWishlist }] = useAddToWishlistMutation();
  const [removeFromWishlist, { isLoading: removingFromWishlist }] = useRemoveFromWishlistMutation();
  const [addToCartBackend, { isLoading: addingToCartBackend }] = useAddToCartMutation();

  // Get current cart item
  const cartItem = useAppSelector((state) => {
    if (!product?.id) return undefined;
    return selectCartItemById(product.id)(state);
  });

  // Toggle wishlist
  const toggleWishlist = async () => {
    if (!product || !isAuthenticated) return;

    try {
      if (isInWishlist) {
        await removeFromWishlist(product.id).unwrap();
      } else {
        await addToWishlist(product.id).unwrap();
      }
    } catch {
      // Error handled by mutation state
    }
  };

  // Add to cart
  const addToCart = async () => {
    if (!product) return;

    setCartError(null);
    const currentInCart = cartItem?.quantity || 0;
    const totalQuantity = currentInCart + quantity;

    if (totalQuantity > product.stockQuantity) {
      setCartError(
        `Only ${product.stockQuantity} items available. You already have ${currentInCart} in cart.`
      );
      return;
    }

    try {
      // Add to local cart
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

      // Sync to backend if authenticated
      if (isAuthenticated) {
        await addToCartBackend({
          productId: product.id,
          quantity,
        }).unwrap();
      }

      setAddedToCart(true);
      setTimeout(() => setAddedToCart(false), 2000);
      setQuantity(1);
    } catch (error: unknown) {
      logger.error('useProductDetails', 'Failed to add to cart', error);
      // Parse backend error message
      const err = error as { data?: { message?: string }; message?: string };
      const errorMessage =
        err?.data?.message || err?.message || 'Failed to add item to cart. Please try again.';
      setCartError(errorMessage);
    }
  };

  return {
    // Product data
    product,
    productLoading,
    productError,

    // Reviews data
    reviews,
    reviewsLoading,
    reviewsError,
    refetchReviews,

    // Wishlist state
    isInWishlist,
    addingToWishlist,
    removingFromWishlist,
    toggleWishlist,

    // Cart state
    quantity,
    setQuantity,
    addedToCart,
    cartError,
    setCartError,
    cartItem,
    addingToCartBackend,
    addToCart,

    // Auth
    isAuthenticated,
  };
}

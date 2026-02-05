import { useState } from 'react';
import { useGetProductBySlugQuery } from '../store/api/productApi';
import { useGetProductReviewsQuery } from '../store/api/reviewsApi';
import {
  useAddToWishlistMutation,
  useRemoveFromWishlistMutation,
  useCheckInWishlistQuery,
} from '../store/api/wishlistApi';
import { useAddToCartMutation } from '../store/api/cartApi';
import { useAppSelector, useAppDispatch } from '../store/hooks';
import { addItem, selectCartItemById } from '../store/slices/cartSlice';
import { DEFAULT_PRODUCT_IMAGE } from '../utils/constants';

export default function useProductDetails(slug: string) {
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

  // Wishlist state
  const { data: isInWishlist, refetch: refetchWishlist } = useCheckInWishlistQuery(
    product?.id || '',
    {
      skip: !product?.id || !isAuthenticated,
    }
  );

  // Mutations
  const [addToWishlist, { isLoading: addingToWishlist }] = useAddToWishlistMutation();
  const [removeFromWishlist, { isLoading: removingFromWishlist }] =
    useRemoveFromWishlistMutation();
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
      await refetchWishlist();
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
      console.error('Failed to add to cart:', error);
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

import { memo, useCallback, useState } from 'react';
import { Link } from 'react-router-dom';
import toast from 'react-hot-toast';
import { useAppSelector, useAppDispatch } from '@/shared/lib/store';
import { addItem, type CartItem } from '@/features/cart/slices/cartSlice';
import {
  useAddToWishlistMutation,
  useRemoveFromWishlistMutation,
  useCheckInWishlistQuery,
} from '@/features/wishlist/api';
import { useAddToCartMutation } from '@/features/cart/api';
import styles from './ProductCard.module.css';

/**
 * ProductCard Props Interface
 */
interface ProductCardProps {
  id: string;
  name: string;
  slug: string;
  price: number;
  compareAtPrice?: number;
  imageUrl: string;
  rating?: number;
  reviewCount?: number;
  stockQuantity?: number;
}

const DEFAULT_PRODUCT_IMAGE = 'https://placehold.co/400x400/f1f5f9/64748b?text=Product';

/**
 * ProductCard Component
 * 
 * A polished, production-ready product card with:
 * - Smooth hover animations and micro-interactions
 * - Quick add to cart with visual feedback
 * - Wishlist functionality
 * - Responsive design
 * - Full accessibility support
 */
const ProductCard = memo(function ProductCard({
  id,
  name,
  slug,
  price,
  compareAtPrice,
  imageUrl,
  rating = 0,
  reviewCount = 0,
  stockQuantity = 99,
}: ProductCardProps) {
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const dispatch = useAppDispatch();
  
  const [imageError, setImageError] = useState(false);
  const [isHovered, setIsHovered] = useState(false);
  const [isAddingToCart, setIsAddingToCart] = useState(false);
  
  // Wishlist hooks
  const { data: isInWishlist = false, refetch: refetchWishlist } = useCheckInWishlistQuery(id, {
    skip: !isAuthenticated,
    refetchOnMountOrArgChange: false,
  });
  
  const [addToWishlist, { isLoading: isAddingToWishlist }] = useAddToWishlistMutation();
  const [removeFromWishlist, { isLoading: isRemovingFromWishlist }] = useRemoveFromWishlistMutation();
  const [addToCartBackend] = useAddToCartMutation();
  
  const isWishlistLoading = isAddingToWishlist || isRemovingFromWishlist;

  // Calculations
  const discountPercentage = compareAtPrice && compareAtPrice > price
    ? Math.round(((compareAtPrice - price) / compareAtPrice) * 100)
    : 0;
  
  const showDiscountBadge = discountPercentage >= 10;
  const isInStock = stockQuantity > 0;

  // Handlers
  const handleWishlistToggle = useCallback(async (event: React.MouseEvent) => {
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
    } catch {
      toast.error('Failed to update wishlist');
    }
  }, [id, isAuthenticated, isInWishlist, isWishlistLoading, addToWishlist, removeFromWishlist, refetchWishlist]);

  const handleAddToCart = useCallback(async (event: React.MouseEvent) => {
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
    } catch {
      toast.error('Failed to add to cart');
    } finally {
      setTimeout(() => setIsAddingToCart(false), 300);
    }
  }, [id, name, slug, price, imageUrl, stockQuantity, isInStock, isAuthenticated, addToCartBackend, dispatch]);

  const handleImageError = useCallback(() => setImageError(true), []);

  const imageSrc = imageError ? DEFAULT_PRODUCT_IMAGE : (imageUrl || DEFAULT_PRODUCT_IMAGE);

  return (
    <article 
      className={styles.card}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      <Link 
        to={`/products/${slug}`} 
        className={styles.cardLink}
        aria-label={`View details for ${name}`}
      >
        {/* Image Container */}
        <div className={styles.imageContainer}>
          <img
            src={imageSrc}
            alt={name}
            className={styles.productImage}
            onError={handleImageError}
            loading="lazy"
            decoding="async"
          />
          
          {/* Quick Add Button - Shows on Hover */}
          <div className={`${styles.quickAddWrapper} ${isHovered && isInStock ? styles.visible : ''}`}>
            <button
              type="button"
              onClick={handleAddToCart}
              className={`${styles.quickAddButton} ${isAddingToCart ? styles.adding : ''}`}
              disabled={!isInStock || isAddingToCart}
              aria-label="Quick add to cart"
            >
              {isAddingToCart ? (
                <svg className={styles.spinner} viewBox="0 0 24 24" aria-hidden="true">
                  <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="3" fill="none" strokeLinecap="round" />
                </svg>
              ) : (
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                  <path d="M12 5v14M5 12h14" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
              )}
            </button>
          </div>
          
          {/* Badges */}
          {showDiscountBadge && (
            <span className={styles.discountBadge}>
              -{discountPercentage}%
            </span>
          )}
          
          {!isInStock && (
            <div className={styles.outOfStockOverlay}>
              <span>Sold Out</span>
            </div>
          )}
          
          {/* Wishlist Button */}
          {isAuthenticated && (
            <button
              type="button"
              onClick={handleWishlistToggle}
              className={`${styles.wishlistButton} ${isInWishlist ? styles.active : ''}`}
              disabled={isWishlistLoading}
              aria-label={isInWishlist ? 'Remove from wishlist' : 'Add to wishlist'}
              aria-pressed={isInWishlist}
            >
              <svg viewBox="0 0 24 24" fill={isInWishlist ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="2" aria-hidden="true">
                <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z" />
              </svg>
            </button>
          )}
        </div>

        {/* Product Info */}
        <div className={styles.productInfo}>
          <h3 className={styles.productName} title={name}>
            {name}
          </h3>
          
          <div className={styles.priceRow}>
            <div className={styles.priceContainer}>
              <span className={styles.pricePrimary}>
                ${price.toFixed(2)}
              </span>
              {compareAtPrice && compareAtPrice > price && (
                <span className={styles.priceCompare}>
                  ${compareAtPrice.toFixed(2)}
                </span>
              )}
            </div>
            
            {rating > 0 && (
              <div className={styles.ratingBadge}>
                <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" />
                </svg>
                <span>{rating.toFixed(1)}</span>
              </div>
            )}
          </div>
          
          {rating > 0 && reviewCount > 0 && (
            <p className={styles.reviewCount}>
              {reviewCount} {reviewCount === 1 ? 'review' : 'reviews'}
            </p>
          )}
        </div>
      </Link>
    </article>
  );
});

export default ProductCard;

import { memo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAppSelector } from '@/shared/lib/store';
import { SpinnerIcon, PlusIcon, HeartIcon, StarIcon } from '@/shared/components/icons';
import { useCheckInWishlistQuery } from '@/features/wishlist/api';
import { useAddToWishlistMutation, useRemoveFromWishlistMutation } from '@/features/wishlist/api';
import type { ProductCardProps } from './ProductCard.types';
import { DEFAULT_PRODUCT_IMAGE } from './ProductCard.types';
import { useWishlistToggle, useAddToCart, useImageError } from './ProductCard.hooks';
import { formatPrice } from '@/shared/lib/utils/priceFormatter';
import styles from './ProductCard.module.css';

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
// eslint-disable-next-line complexity -- JSX conditional rendering inflates the branch count; logic is delegated to custom hooks
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

  const [imageError, setImageError] = useState(false);
  const [isHovered, setIsHovered] = useState(false);
  const [isAddingToCart, setIsAddingToCart] = useState(false);

  // Wishlist hooks
  const { data: isInWishlist = false, refetch: refetchWishlist } = useCheckInWishlistQuery(id, {
    skip: !isAuthenticated,
    refetchOnMountOrArgChange: false,
  });

  const [, { isLoading: isAddingToWishlist }] = useAddToWishlistMutation();
  const [, { isLoading: isRemovingFromWishlist }] = useRemoveFromWishlistMutation();

  const isWishlistLoading = isAddingToWishlist || isRemovingFromWishlist;

  // Calculations
  const discountPercentage =
    compareAtPrice && compareAtPrice > price
      ? Math.round(((compareAtPrice - price) / compareAtPrice) * 100)
      : 0;

  const showDiscountBadge = discountPercentage >= 10;
  const isInStock = stockQuantity > 0;

  // Custom hooks
  const { handleWishlistToggle } = useWishlistToggle({
    id,
    isAuthenticated,
    isInWishlist,
    isWishlistLoading,
    refetchWishlist,
  });

  const { handleAddToCart } = useAddToCart({
    id,
    name,
    slug,
    price,
    imageUrl,
    stockQuantity,
    isInStock,
    isAuthenticated,
    setIsAddingToCart,
  });

  const { handleImageError } = useImageError(setImageError);

  const imageSrc = imageError ? DEFAULT_PRODUCT_IMAGE : imageUrl || DEFAULT_PRODUCT_IMAGE;

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
          <div
            className={`${styles.quickAddWrapper} ${isHovered && isInStock ? styles.visible : ''}`}
          >
            <button
              type="button"
              onClick={handleAddToCart}
              className={`${styles.quickAddButton} ${isAddingToCart ? styles.adding : ''}`}
              disabled={!isInStock || isAddingToCart}
              aria-label="Quick add to cart"
            >
              {isAddingToCart ? (
                <SpinnerIcon className={styles.spinner} aria-hidden="true" />
              ) : (
                <PlusIcon aria-hidden="true" />
              )}
            </button>
          </div>

          {/* Badges */}
          {showDiscountBadge && (
            <span className={styles.discountBadge}>-{discountPercentage}%</span>
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
              <HeartIcon filled={isInWishlist} />
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
              <span className={styles.pricePrimary}>{formatPrice(price)}</span>
              {compareAtPrice && compareAtPrice > price && (
                <span className={styles.priceCompare}>{formatPrice(compareAtPrice)}</span>
              )}
            </div>

            {rating > 0 && (
              <div className={styles.ratingBadge}>
                <StarIcon fill="currentColor" aria-hidden="true" />
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

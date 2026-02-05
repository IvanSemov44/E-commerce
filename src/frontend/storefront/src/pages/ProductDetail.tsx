import { useState } from 'react';
import { useParams } from 'react-router-dom';

import { DEFAULT_PRODUCT_IMAGE } from '../utils/constants';
import useProductDetails from '../hooks/useProductDetails';
import Button from '../components/ui/Button';
import Card from '../components/ui/Card';
import ErrorAlert from '../components/ErrorAlert';
import LoadingSkeleton from '../components/LoadingSkeleton';
import EmptyState from '../components/EmptyState';
import ReviewForm from '../components/ReviewForm';
import ReviewList from '../components/ReviewList';

import styles from './ProductDetail.module.css';

export default function ProductDetail() {
  const { slug = '' } = useParams();
  const [selectedImageIndex, setSelectedImageIndex] = useState(0);

  const {
    product,
    productLoading: isLoading,
    productError: error,
    reviews,
    reviewsLoading,
    reviewsError,
    refetchReviews,
    isInWishlist,
    addingToWishlist,
    removingFromWishlist,
    toggleWishlist,
    quantity,
    setQuantity,
    addedToCart,
    cartError,
    setCartError,
    cartItem,
    addingToCartBackend,
    addToCart,
    isAuthenticated,
  } = useProductDetails(slug);

  if (isLoading) {
    return (
      <div className={styles.container}>
        <div className={styles.content}>
          <LoadingSkeleton count={1} type="image" />
        </div>
      </div>
    );
  }
  if (error) {
    return (
      <div className={styles.container}>
        <div className={styles.content}>
          <ErrorAlert message="Failed to load product. Please try again." />
        </div>
      </div>
    );
  }
  if (!product) {
    return (
      <div className={styles.container}>
        <div className={styles.content}>
          <EmptyState title="Product not found" description="The product you're looking for doesn't exist." />
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.content}>
        <Card variant="default" padding="lg">
          <div className={styles.grid}>
            {/* Images */}
            <div className={styles.imageSection}>
              <div className={styles.mainImage}>
                <img
                  src={product.images[selectedImageIndex]?.url}
                  alt={product.name}
                  onError={(e) => { e.currentTarget.src = DEFAULT_PRODUCT_IMAGE }}
                />
              </div>
              {product.images.length > 1 && (
                <div className={styles.thumbnailGrid}>
                  {product.images.map((img, index) => (
                    <button
                      key={img.id}
                      onClick={() => setSelectedImageIndex(index)}
                      className={`${styles.thumbnail} ${selectedImageIndex === index ? styles.thumbnailActive : ''}`}
                      aria-label={`View product image ${index + 1}`}
                      type="button"
                    >
                      <img
                        src={img.url}
                        alt={img.altText || `Product image ${index + 1}`}
                        onError={(e) => { e.currentTarget.src = DEFAULT_PRODUCT_IMAGE }}
                      />
                    </button>
                  ))}
                </div>
              )}
            </div>

            {/* Details */}
            <div className={styles.details}>
              <h1>{product.name}</h1>

              <div className={styles.rating}>
                <div className={styles.ratingContainer}>
                  <span className={styles.ratingIcon}>★</span>
                  <span className={styles.ratingValue}>{product.averageRating}</span>
                </div>
                <span className={styles.ratingCount}>({product.reviewCount} reviews)</span>
              </div>

              <div className={styles.priceSection}>
                <div className={styles.priceContainer}>
                  <span className={styles.pricePrimary}>${product.price.toFixed(2)}</span>
                  {product.compareAtPrice && (
                    <span className={styles.priceCompare}>${product.compareAtPrice.toFixed(2)}</span>
                  )}
                </div>
              </div>

              <p className={styles.description}>{product.description}</p>

              <div className={styles.stockSection}>
                <p className={`${styles.stockLabel} ${product.stockQuantity > 0 ? styles.inStock : styles.outOfStock}`}>
                  {product.stockQuantity > 0 ? `${product.stockQuantity} in stock` : 'Out of stock'}
                </p>
                {product.stockQuantity > 0 && product.stockQuantity <= product.lowStockThreshold && (
                  <p className={styles.lowStockWarning}>⚠ Only {product.stockQuantity} left!</p>
                )}
              </div>

              <div className={styles.quantitySection}>
                <label className={styles.quantityLabel}>Quantity:</label>
                <div className={styles.quantityControls}>
                  <div className={styles.quantityButtonGroup}>
                    <button
                      onClick={() => setQuantity(Math.max(1, quantity - 1))}
                      className={styles.quantityButton}
                    >
                      −
                    </button>
                    <input
                      type="number"
                      value={quantity}
                      onChange={(e) => {
                        const val = parseInt(e.target.value) || 1;
                        setQuantity(Math.min(product.stockQuantity, Math.max(1, val)));
                      }}
                      className={styles.quantityInput}
                      min="1"
                      max={product.stockQuantity}
                    />
                    <button
                      onClick={() => setQuantity(Math.min(product.stockQuantity, quantity + 1))}
                      disabled={quantity >= product.stockQuantity}
                      className={styles.quantityButton}
                    >
                      +
                    </button>
                  </div>
                  {cartItem && (
                    <span className={styles.cartHint}>
                      ({cartItem.quantity} in cart)
                    </span>
                  )}
                </div>
              </div>

              {cartError && (
                <div style={{ marginBottom: '16px' }}>
                  <ErrorAlert message={cartError} onDismiss={() => setCartError(null)} />
                </div>
              )}

              <div className={styles.actions}>
                <Button
                  onClick={addToCart}
                  disabled={product.stockQuantity === 0 || addedToCart || addingToCartBackend}
                  size="lg"
                >
                  {product.stockQuantity === 0 ? 'Out of Stock' : addedToCart ? '✓ Added to Cart!' : 'Add to Cart'}
                </Button>
                {isAuthenticated && (
                  <Button
                    variant={isInWishlist ? 'primary' : 'secondary'}
                    size="lg"
                    onClick={toggleWishlist}
                    disabled={addingToWishlist || removingFromWishlist}
                  >
                    {isInWishlist ? '♥ In Wishlist' : '♡ Add to Wishlist'}
                  </Button>
                )}
              </div>
            </div>
          </div>

          {/* Reviews */}
          <div className={styles.reviewsSection}>
            <h2 className={styles.reviewsTitle}>Customer Reviews</h2>

            {isAuthenticated && (
              <div style={{ marginBottom: '2rem' }}>
                <ReviewForm productId={product.id} onSuccess={() => refetchReviews()} />
              </div>
            )}

            <ReviewList
              reviews={reviews || []}
              isLoading={reviewsLoading}
              error={reviewsError}
              onReviewDeleted={() => refetchReviews()}
            />
          </div>
        </Card>
      </div>
    </div>
  );
}

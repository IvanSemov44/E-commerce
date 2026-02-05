import { useParams } from 'react-router-dom';

import useProductDetails from '../hooks/useProductDetails';
import Card from '../components/ui/Card';
import ErrorAlert from '../components/ErrorAlert';
import LoadingSkeleton from '../components/LoadingSkeleton';
import EmptyState from '../components/EmptyState';
import ReviewForm from '../components/ReviewForm';
import ReviewList from '../components/ReviewList';
import { ProductImageGallery, ProductInfo, ProductActions } from './components/ProductDetail';

import styles from './ProductDetail.module.css';

export default function ProductDetail() {
  const { slug = '' } = useParams();

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
            <ProductImageGallery images={product.images} productName={product.name} />

            <div className={styles.details}>
              <ProductInfo
                name={product.name}
                description={product.description}
                averageRating={product.averageRating}
                reviewCount={product.reviewCount}
                price={product.price}
                compareAtPrice={product.compareAtPrice}
              />

              <ProductActions
                stockQuantity={product.stockQuantity}
                lowStockThreshold={product.lowStockThreshold}
                quantity={quantity}
                cartItem={cartItem}
                addedToCart={addedToCart}
                addingToCartBackend={addingToCartBackend}
                cartError={cartError}
                isAuthenticated={isAuthenticated}
                isInWishlist={isInWishlist}
                addingToWishlist={addingToWishlist}
                removingFromWishlist={removingFromWishlist}
                onQuantityChange={setQuantity}
                onAddToCart={addToCart}
                onToggleWishlist={toggleWishlist}
                onDismissError={() => setCartError(null)}
              />
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

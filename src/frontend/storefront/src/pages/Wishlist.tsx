import { Link } from 'react-router-dom';
import { useGetWishlistQuery, useRemoveFromWishlistMutation } from '../store/api/wishlistApi';
import { useGetProductsQuery } from '../store/api/productApi';
import Button from '../components/ui/Button';
import PageHeader from '../components/PageHeader';
import EmptyState from '../components/EmptyState';
import ErrorAlert from '../components/ErrorAlert';
import LoadingSkeleton from '../components/LoadingSkeleton';
import styles from './Wishlist.module.css';

export default function Wishlist() {
  const { data: wishlist, isLoading: wishlistLoading, error: wishlistError } =
    useGetWishlistQuery();
  const [removeFromWishlist] = useRemoveFromWishlistMutation();

  const productIds = wishlist?.items.map((item) => item.productId) || [];
  const pageSize = 100;

  // Fetch all wishlist products (simplified - in production would need pagination)
  const { data: allProducts, isLoading: productsLoading } = useGetProductsQuery({
    page: 1,
    pageSize,
  });

  const wishlistProducts = allProducts?.items.filter((p) => productIds.includes(p.id)) || [];

  const handleRemoveFromWishlist = async (productId: string) => {
    try {
      await removeFromWishlist(productId).unwrap();
    } catch {
      // Error handled by mutation state
    }
  };

  const isLoading = wishlistLoading || productsLoading;

  return (
    <div style={{ maxWidth: '1200px', margin: '0 auto', padding: '0 1rem' }}>
      <PageHeader title="My Wishlist" />

      {wishlistError ? (
        <ErrorAlert message="Failed to load wishlist. Please try again later." />
      ) : isLoading ? (
        <div className={styles.grid}>
          <LoadingSkeleton count={6} type="card" />
        </div>
      ) : wishlistProducts.length > 0 ? (
        <div className={styles.grid}>
          {wishlistProducts.map((product) => (
            <div key={product.id} className={styles.card}>
              <Link
                to={`/products/${product.slug}`}
                style={{ textDecoration: 'none', color: 'inherit' }}
              >
                <div className={styles.imageContainer}>
                  {product.images[0]?.url && (
                    <img
                      src={product.images[0].url}
                      alt={product.name}
                      className={styles.image}
                    />
                  )}
                </div>
                <div className={styles.content}>
                  <h3 className={styles.name}>{product.name}</h3>
                  {product.shortDescription && (
                    <p className={styles.description}>{product.shortDescription}</p>
                  )}
                </div>
              </Link>

              <div className={styles.footer}>
                <div className={styles.priceSection}>
                  <span className={styles.price}>${product.price.toFixed(2)}</span>
                  {product.compareAtPrice && product.compareAtPrice > product.price && (
                    <span className={styles.comparePrice}>
                      ${product.compareAtPrice.toFixed(2)}
                    </span>
                  )}
                </div>

                <div className={styles.actions}>
                  <Button
                    size="sm"
                    variant="secondary"
                    onClick={() => handleRemoveFromWishlist(product.id)}
                  >
                    Remove
                  </Button>
                </div>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <EmptyState
          icon={
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"
              />
            </svg>
          }
          title="Your wishlist is empty"
          description="Start adding products you love to your wishlist"
          action={
            <Link to="/products">
              <Button>Browse Products</Button>
            </Link>
          }
        />
      )}
    </div>
  );
}

import { Link } from 'react-router-dom';
import { useGetWishlistQuery, useRemoveFromWishlistMutation } from '../store/api/wishlistApi';
import { useGetProductsQuery } from '../store/api/productApi';
import Button from '../components/ui/Button';
import PageHeader from '../components/PageHeader';
import QueryRenderer from '../components/QueryRenderer';
import { WishlistCard } from './components/Wishlist';
import styles from './Wishlist.module.css';

// Heart Icon
const HeartIcon = () => (
  <svg fill="currentColor" viewBox="0 0 24 24">
    <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>
  </svg>
);

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
    <div className={styles.container}>
      <PageHeader 
        title="My Saved Favorites" 
        subtitle="Your personal collection of products you love. Keep track of items you want to purchase later."
        icon={<HeartIcon />}
        badge="Your Collection"
      />

      <QueryRenderer
        isLoading={isLoading}
        error={wishlistError}
        data={wishlistProducts}
        errorMessage="Failed to load wishlist. Please try again later."
        emptyState={{
          icon: (
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"
              />
            </svg>
          ),
          title: "Your wishlist is empty",
          description: "Start adding products you love to your wishlist",
          action: (
            <Link to="/products">
              <Button>Browse Products</Button>
            </Link>
          ),
        }}
      >
        {(products) => (
          <div className={styles.grid}>
            {products.map((product) => (
              <WishlistCard
                key={product.id}
                product={product}
                onRemove={handleRemoveFromWishlist}
              />
            ))}
          </div>
        )}
      </QueryRenderer>
    </div>
  );
}

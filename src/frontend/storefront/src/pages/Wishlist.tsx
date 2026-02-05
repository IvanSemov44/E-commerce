import { Link } from 'react-router-dom';
import { useGetWishlistQuery, useRemoveFromWishlistMutation } from '../store/api/wishlistApi';
import { useGetProductsQuery } from '../store/api/productApi';
import Button from '../components/ui/Button';
import PageHeader from '../components/PageHeader';
import QueryRenderer from '../components/QueryRenderer';
import { WishlistCard } from './components/Wishlist';
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
    <div className={styles.container}>
      <PageHeader title="My Wishlist" />

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

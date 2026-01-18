import { Link } from 'react-router-dom';
import { useGetFeaturedProductsQuery } from '../store/api/productApi';
import Button from '../components/ui/Button';
import ProductCard from '../components/ProductCard';
import PageHeader from '../components/PageHeader';
import EmptyState from '../components/EmptyState';
import ErrorAlert from '../components/ErrorAlert';
import LoadingSkeleton from '../components/LoadingSkeleton';
import styles from './Home.module.css';

export default function Home() {
  const { data: featured, isLoading, error } = useGetFeaturedProductsQuery(10);

  return (
    <div className={styles.container}>
      {/* Hero Section */}
      <section className={styles.hero}>
        <div className={styles.heroContent}>
          <h1 className={styles.heroTitle}>
            Discover Premium Products
          </h1>
          <p className={styles.heroSubtitle}>
            Curated selection of quality items at exceptional prices
          </p>
          <Link to="/products">
            <Button size="lg">
              Explore Products
            </Button>
          </Link>
        </div>
      </section>

      {/* Featured Products */}
      <section className={styles.featuredSection}>
        <PageHeader title="Featured Products" />

        {error ? (
          <ErrorAlert message="Failed to load featured products. Please try again later." />
        ) : isLoading ? (
          <div className={styles.grid}>
            <LoadingSkeleton count={4} type="card" />
          </div>
        ) : featured && featured.length > 0 ? (
          <div className={styles.grid}>
            {featured.map((product) => (
              <ProductCard
                key={product.id}
                id={product.id}
                name={product.name}
                slug={product.slug}
                price={product.price}
                compareAtPrice={product.compareAtPrice}
                imageUrl={product.images[0]?.url}
                rating={Math.round(product.averageRating)}
                reviewCount={product.reviewCount}
              />
            ))}
          </div>
        ) : (
          <EmptyState
            icon={
              <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
              </svg>
            }
            title="No featured products available"
          />
        )}
      </section>
    </div>
  );
}

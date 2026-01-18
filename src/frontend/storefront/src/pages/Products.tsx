import { useState } from 'react';
import { useGetProductsQuery } from '../store/api/productApi';
import Button from '../components/ui/Button';
import ProductCard from '../components/ProductCard';
import PageHeader from '../components/PageHeader';
import EmptyState from '../components/EmptyState';
import ErrorAlert from '../components/ErrorAlert';
import LoadingSkeleton from '../components/LoadingSkeleton';
import styles from './Products.module.css';

export default function Products() {
  const [page, setPage] = useState(1);
  const { data: result, isLoading, error } = useGetProductsQuery({ page, pageSize: 20 });

  return (
    <div className={styles.container}>
      <div className={styles.content}>
        <PageHeader title="All Products" />

        {error ? (
          <ErrorAlert message="Failed to load products. Please try again later." />
        ) : isLoading ? (
          <div className={styles.grid}>
            <LoadingSkeleton count={8} type="card" />
          </div>
        ) : result && result.items.length > 0 ? (
          <>
            <div className={styles.grid}>
              {result.items.map((product) => (
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

            {/* Pagination */}
            <div className={styles.pagination}>
              <Button
                variant="secondary"
                onClick={() => setPage(Math.max(1, page - 1))}
                disabled={page === 1}
              >
                Previous
              </Button>
              <span className={styles.pageNumber}>Page {page}</span>
              <Button
                variant="secondary"
                onClick={() => setPage(page + 1)}
                disabled={!result || result.items.length < 20}
              >
                Next
              </Button>
            </div>
          </>
        ) : (
          <EmptyState
            icon={
              <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
              </svg>
            }
            title="No products available"
          />
        )}
      </div>
    </div>
  );
}

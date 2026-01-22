import { useState, useEffect } from 'react';
import { useGetProductsQuery } from '../store/api/productApi';
import Button from '../components/ui/Button';
import ProductCard from '../components/ProductCard';
import CategoryFilter from '../components/CategoryFilter';
import PageHeader from '../components/PageHeader';
import EmptyState from '../components/EmptyState';
import ErrorAlert from '../components/ErrorAlert';
import LoadingSkeleton from '../components/LoadingSkeleton';
import styles from './Products.module.css';

export default function Products() {
  const [page, setPage] = useState(1);
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | undefined>();
  const [searchInput, setSearchInput] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');

  // Debounce search input (500ms)
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchInput);
    }, 500);

    return () => clearTimeout(timer);
  }, [searchInput]);

  // Reset to page 1 when filters change
  useEffect(() => {
    setPage(1);
  }, [selectedCategoryId, debouncedSearch]);

  const { data: result, isLoading, error } = useGetProductsQuery({
    page,
    pageSize: 12,
    categoryId: selectedCategoryId,
    search: debouncedSearch,
  });

  const hasActiveFilters = selectedCategoryId || debouncedSearch;

  const handleClearFilters = () => {
    setSelectedCategoryId(undefined);
    setSearchInput('');
    setPage(1);
  };

  return (
    <div className={styles.container}>
      <PageHeader title="All Products" />

      <div style={{ display: 'flex', gap: '2rem', maxWidth: '1400px', margin: '0 auto', padding: '0 1rem' }}>
        {/* Sidebar */}
        <div style={{ width: '280px', flexShrink: 0 }}>
          <CategoryFilter
            selectedCategoryId={selectedCategoryId}
            onSelectCategory={setSelectedCategoryId}
          />
        </div>

        {/* Main Content */}
        <div className={styles.content} style={{ flex: 1, minWidth: 0 }}>
          {/* Search and Filter Bar */}
          <div style={{ marginBottom: '2rem' }}>
            <div style={{ marginBottom: '1rem' }}>
              <input
                type="text"
                placeholder="Search products..."
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                style={{
                  width: '100%',
                  padding: '0.75rem',
                  fontSize: '1rem',
                  border: '1px solid #e0e0e0',
                  borderRadius: '0.5rem',
                  boxSizing: 'border-box',
                }}
              />
            </div>

            {/* Active Filters Display */}
            {hasActiveFilters && (
              <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
                {debouncedSearch && (
                  <div
                    style={{
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: '0.5rem',
                      padding: '0.25rem 0.75rem',
                      backgroundColor: '#e3f2fd',
                      border: '1px solid #1976d2',
                      borderRadius: '1rem',
                      fontSize: '0.875rem',
                      color: '#1976d2',
                    }}
                  >
                    Search: <strong>{debouncedSearch}</strong>
                  </div>
                )}
                {selectedCategoryId && (
                  <div
                    style={{
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: '0.5rem',
                      padding: '0.25rem 0.75rem',
                      backgroundColor: '#f3e5f5',
                      border: '1px solid #7b1fa2',
                      borderRadius: '1rem',
                      fontSize: '0.875rem',
                      color: '#7b1fa2',
                    }}
                  >
                    Category Selected
                  </div>
                )}
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={handleClearFilters}
                  style={{ padding: '0.25rem 0.75rem', fontSize: '0.875rem' }}
                >
                  Clear Filters
                </Button>
              </div>
            )}
          </div>

          {error ? (
            <ErrorAlert message="Failed to load products. Please try again later." />
          ) : isLoading ? (
            <div className={styles.grid}>
              <LoadingSkeleton count={12} type="card" />
            </div>
          ) : result && result.items.length > 0 ? (
            <>
              {/* Results Count */}
              <div style={{ marginBottom: '1.5rem', color: '#666', fontSize: '0.95rem' }}>
                Showing <strong>{result.items.length}</strong> of <strong>{result.totalCount}</strong> products
                {hasActiveFilters && <span style={{ marginLeft: '0.5rem', fontStyle: 'italic' }}>(filtered)</span>}
              </div>

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
                <span className={styles.pageNumber}>Page {page} of {Math.ceil(result.totalCount / 12)}</span>
                <Button
                  variant="secondary"
                  onClick={() => setPage(page + 1)}
                  disabled={!result || result.items.length < 12}
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
              title={hasActiveFilters ? "No products match your filters" : "No products available"}
              description={hasActiveFilters ? "Try adjusting your search or category filter" : undefined}
              action={hasActiveFilters ? <Button onClick={handleClearFilters}>Clear Filters</Button> : undefined}
            />
          )}
        </div>
      </div>
    </div>
  );
}

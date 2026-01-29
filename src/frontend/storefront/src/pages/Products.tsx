import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
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
  const [searchParams, setSearchParams] = useSearchParams();

  // Initialize state from URL params on mount (lazy initialization)
  const [page, setPage] = useState(() => {
    const pageParam = searchParams.get('page');
    return pageParam ? parseInt(pageParam, 10) : 1;
  });

  const [selectedCategoryId, setSelectedCategoryId] = useState<string | undefined>(
    () => searchParams.get('categoryId') || undefined
  );

  const [searchInput, setSearchInput] = useState(() => searchParams.get('search') || '');
  const [debouncedSearch, setDebouncedSearch] = useState(() => searchParams.get('search') || '');

  // New filter states
  const [minPrice, setMinPrice] = useState<number | undefined>(() => {
    const val = searchParams.get('minPrice');
    return val ? parseFloat(val) : undefined;
  });
  const [maxPrice, setMaxPrice] = useState<number | undefined>(() => {
    const val = searchParams.get('maxPrice');
    return val ? parseFloat(val) : undefined;
  });
  const [minRating, setMinRating] = useState<number | undefined>(() => {
    const val = searchParams.get('minRating');
    return val ? parseFloat(val) : undefined;
  });
  const [sortBy, setSortBy] = useState<string>(() => searchParams.get('sortBy') || 'newest');
  const [isFeatured, setIsFeatured] = useState<boolean | undefined>(() => {
    const val = searchParams.get('isFeatured');
    return val === 'true' ? true : undefined;
  });

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
  }, [selectedCategoryId, debouncedSearch, minPrice, maxPrice, minRating, sortBy, isFeatured]);

  // Sync URL when filters change (after debounce completes)
  useEffect(() => {
    const params = new URLSearchParams();

    if (debouncedSearch) params.set('search', debouncedSearch);
    if (selectedCategoryId) params.set('categoryId', selectedCategoryId);
    if (minPrice !== undefined) params.set('minPrice', minPrice.toString());
    if (maxPrice !== undefined) params.set('maxPrice', maxPrice.toString());
    if (minRating !== undefined) params.set('minRating', minRating.toString());
    if (sortBy !== 'newest') params.set('sortBy', sortBy);
    if (isFeatured) params.set('isFeatured', 'true');
    if (page > 1) params.set('page', page.toString());

    setSearchParams(params, { replace: true }); // Use replace to avoid history pollution
  }, [debouncedSearch, selectedCategoryId, minPrice, maxPrice, minRating, sortBy, isFeatured, page, setSearchParams]);

  const { data: result, isLoading, error } = useGetProductsQuery({
    page,
    pageSize: 12,
    categoryId: selectedCategoryId,
    search: debouncedSearch,
    minPrice,
    maxPrice,
    minRating,
    sortBy,
    isFeatured,
  });

  const hasActiveFilters = selectedCategoryId || debouncedSearch || minPrice !== undefined || maxPrice !== undefined || minRating !== undefined || isFeatured;

  const handleClearFilters = () => {
    setSelectedCategoryId(undefined);
    setSearchInput('');
    setMinPrice(undefined);
    setMaxPrice(undefined);
    setMinRating(undefined);
    setSortBy('newest');
    setIsFeatured(undefined);
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

          {/* Price Filter */}
          <div style={{ marginTop: '2rem', padding: '1rem', backgroundColor: '#f9f9f9', borderRadius: '8px' }}>
            <h3 style={{ fontSize: '1rem', fontWeight: '600', marginBottom: '1rem' }}>Price Range</h3>
            <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.75rem' }}>
              <input
                type="number"
                placeholder="Min"
                value={minPrice || ''}
                onChange={(e) => setMinPrice(e.target.value ? parseFloat(e.target.value) : undefined)}
                style={{
                  width: '50%',
                  padding: '0.5rem',
                  fontSize: '0.875rem',
                  border: '1px solid #e0e0e0',
                  borderRadius: '0.375rem',
                }}
              />
              <input
                type="number"
                placeholder="Max"
                value={maxPrice || ''}
                onChange={(e) => setMaxPrice(e.target.value ? parseFloat(e.target.value) : undefined)}
                style={{
                  width: '50%',
                  padding: '0.5rem',
                  fontSize: '0.875rem',
                  border: '1px solid #e0e0e0',
                  borderRadius: '0.375rem',
                }}
              />
            </div>
          </div>

          {/* Rating Filter */}
          <div style={{ marginTop: '1rem', padding: '1rem', backgroundColor: '#f9f9f9', borderRadius: '8px' }}>
            <h3 style={{ fontSize: '1rem', fontWeight: '600', marginBottom: '1rem' }}>Minimum Rating</h3>
            <select
              value={minRating || ''}
              onChange={(e) => setMinRating(e.target.value ? parseFloat(e.target.value) : undefined)}
              style={{
                width: '100%',
                padding: '0.5rem',
                fontSize: '0.875rem',
                border: '1px solid #e0e0e0',
                borderRadius: '0.375rem',
                backgroundColor: 'white',
              }}
            >
              <option value="">All Ratings</option>
              <option value="4">4+ Stars</option>
              <option value="4.5">4.5+ Stars</option>
              <option value="5">5 Stars</option>
            </select>
          </div>

          {/* Featured Filter */}
          <div style={{ marginTop: '1rem', padding: '1rem', backgroundColor: '#f9f9f9', borderRadius: '8px' }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer' }}>
              <input
                type="checkbox"
                checked={isFeatured || false}
                onChange={(e) => setIsFeatured(e.target.checked ? true : undefined)}
                style={{ cursor: 'pointer' }}
              />
              <span style={{ fontSize: '0.9rem' }}>Featured Products Only</span>
            </label>
          </div>
        </div>

        {/* Main Content */}
        <div className={styles.content} style={{ flex: 1, minWidth: 0 }}>
          {/* Search and Sort Bar */}
          <div style={{ marginBottom: '2rem' }}>
            <div style={{ display: 'flex', gap: '1rem', marginBottom: '1rem' }}>
              <input
                type="text"
                placeholder="Search products..."
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                style={{
                  flex: 1,
                  padding: '0.75rem',
                  fontSize: '1rem',
                  border: '1px solid #e0e0e0',
                  borderRadius: '0.5rem',
                }}
              />
              <select
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value)}
                style={{
                  padding: '0.75rem',
                  fontSize: '1rem',
                  border: '1px solid #e0e0e0',
                  borderRadius: '0.5rem',
                  backgroundColor: 'white',
                  minWidth: '180px',
                }}
              >
                <option value="newest">Newest First</option>
                <option value="name">Name (A-Z)</option>
                <option value="price-asc">Price: Low to High</option>
                <option value="price-desc">Price: High to Low</option>
                <option value="rating">Highest Rated</option>
              </select>
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
                {(minPrice !== undefined || maxPrice !== undefined) && (
                  <div
                    style={{
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: '0.5rem',
                      padding: '0.25rem 0.75rem',
                      backgroundColor: '#e8f5e9',
                      border: '1px solid #388e3c',
                      borderRadius: '1rem',
                      fontSize: '0.875rem',
                      color: '#388e3c',
                    }}
                  >
                    Price: ${minPrice || 0} - ${maxPrice || '∞'}
                  </div>
                )}
                {minRating !== undefined && (
                  <div
                    style={{
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: '0.5rem',
                      padding: '0.25rem 0.75rem',
                      backgroundColor: '#fff3e0',
                      border: '1px solid #f57c00',
                      borderRadius: '1rem',
                      fontSize: '0.875rem',
                      color: '#f57c00',
                    }}
                  >
                    {minRating}+ Stars
                  </div>
                )}
                {isFeatured && (
                  <div
                    style={{
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: '0.5rem',
                      padding: '0.25rem 0.75rem',
                      backgroundColor: '#fce4ec',
                      border: '1px solid #c2185b',
                      borderRadius: '1rem',
                      fontSize: '0.875rem',
                      color: '#c2185b',
                    }}
                  >
                    Featured Only
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

import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useGetProductsQuery } from '../store/api/productApi';
import Button from '../components/ui/Button';
import CategoryFilter from '../components/CategoryFilter';
import PageHeader from '../components/PageHeader';
import QueryRenderer from '../components/QueryRenderer';
import {
  ProductFilters,
  ProductSearchBar,
  ActiveFilters,
  ProductGrid,
} from './components/Products';
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

  const hasActiveFilters = !!(selectedCategoryId || debouncedSearch || minPrice !== undefined || maxPrice !== undefined || minRating !== undefined || isFeatured);

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

      <div className={styles.layout}>
        {/* Sidebar */}
        <div className={styles.sidebar}>
          <CategoryFilter
            selectedCategoryId={selectedCategoryId}
            onSelectCategory={setSelectedCategoryId}
          />

          <div className={styles.filtersSection}>
            <ProductFilters
              minPrice={minPrice}
              maxPrice={maxPrice}
              minRating={minRating}
              isFeatured={isFeatured}
              onMinPriceChange={setMinPrice}
              onMaxPriceChange={setMaxPrice}
              onMinRatingChange={setMinRating}
              onIsFeaturedChange={setIsFeatured}
            />
          </div>
        </div>

        {/* Main Content */}
        <div className={styles.content}>
          <div className={styles.searchSection}>
            <ProductSearchBar
              searchValue={searchInput}
              sortBy={sortBy}
              onSearchChange={setSearchInput}
              onSortChange={setSortBy}
            />

            <ActiveFilters
              search={debouncedSearch}
              categorySelected={!!selectedCategoryId}
              minPrice={minPrice}
              maxPrice={maxPrice}
              minRating={minRating}
              isFeatured={isFeatured}
              onClearAll={handleClearFilters}
            />
          </div>

          <QueryRenderer
            isLoading={isLoading}
            error={error}
            data={result}
            errorMessage="Failed to load products. Please try again later."
            isEmpty={(data) => !data || data.items.length === 0}
            emptyState={{
              icon: (
                <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
                </svg>
              ),
              title: hasActiveFilters ? "No products match your filters" : "No products available",
              description: hasActiveFilters ? "Try adjusting your search or category filter" : undefined,
              action: hasActiveFilters ? <Button onClick={handleClearFilters}>Clear Filters</Button> : undefined,
            }}
          >
            {(data) => (
              <ProductGrid
                products={data.items}
                totalCount={data.totalCount}
                currentPage={page}
                pageSize={12}
                onPageChange={setPage}
              />
            )}
          </QueryRenderer>
        </div>
      </div>
    </div>
  );
}

import { useGetProductsQuery } from '../store/api/productApi';
import { useProductFilters } from '../hooks';
import Button from '../components/ui/Button';
import CategoryFilter from '../components/CategoryFilter';
import PageHeader from '../components/PageHeader';
import QueryRenderer from '../components/QueryRenderer';
import { ProductsGridSkeleton } from '../components/Skeletons';
import {
  ProductFilters,
  ProductSearchBar,
  ActiveFilters,
  ProductGrid,
} from './components/Products';
import styles from './Products.module.css';

// Icons
const GridIcon = () => (
  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
  </svg>
);

export default function Products() {
  const {
    page,
    selectedCategoryId,
    searchInput,
    debouncedSearch,
    minPrice,
    maxPrice,
    minRating,
    sortBy,
    isFeatured,
    hasActiveFilters,
    setPage,
    setSelectedCategoryId,
    setSearchInput,
    setMinPrice,
    setMaxPrice,
    setMinRating,
    setSortBy,
    setIsFeatured,
    handleClearFilters,
  } = useProductFilters();

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

  return (
    <div className={styles.container}>
      <PageHeader 
        title="Discover Our Products" 
        subtitle="Explore our curated collection of premium products. Find exactly what you're looking for with our powerful filters and search."
        icon={<GridIcon />}
        badge="Premium Selection"
      />

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
            loadingSkeleton={{ custom: <ProductsGridSkeleton count={12} /> }}
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

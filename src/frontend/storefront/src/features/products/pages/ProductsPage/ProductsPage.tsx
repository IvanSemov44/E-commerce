import { useTranslation } from 'react-i18next';
import { usePerformanceMonitor } from '@/shared/hooks';
import { useGetProductsQuery } from '@/features/products/api/productApi';
import { useProductFilters } from '@/features/products/hooks/useProductFilters';
import { Button } from '@/shared/components/ui/Button';
import { CategoryFilter } from '@/features/products/components/CategoryFilter';
import { SearchBar } from '@/app/SearchBar';
import PageHeader from '@/shared/components/PageHeader';
import QueryRenderer from '@/shared/components/QueryRenderer';
import { GridIcon, RefreshIcon } from '@/shared/components/icons';
import {
  ProductFilters,
  ActiveFilters,
  ProductGrid,
  ProductsGridSkeleton,
} from '@/features/products/components';
import { PRODUCTS_PAGE_SIZE } from '@/features/products/constants';
import styles from './ProductsPage.module.css';

export function ProductsPage() {
  usePerformanceMonitor();
  const { t } = useTranslation();
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

  const {
    data: result,
    isLoading,
    isFetching,
    error,
  } = useGetProductsQuery({
    page,
    pageSize: PRODUCTS_PAGE_SIZE,
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
        title={t('products.discoverProducts')}
        subtitle={t('products.exploreCollection')}
        icon={<GridIcon />}
        badge={t('products.allProducts')}
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
            <div className={styles.searchBar}>
              <div className={styles.searchInput}>
                <SearchBar
                  key={searchInput ? 1 : 0}
                  size="md"
                  placeholder={t('products.searchProducts')}
                  onQueryChange={setSearchInput}
                />
              </div>
              <select
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value)}
                className={styles.sortSelect}
              >
                <option value="newest">{t('products.sortNewest')}</option>
                <option value="name">{t('products.sortNameAZ')}</option>
                <option value="price-asc">{t('products.sortPriceLowHigh')}</option>
                <option value="price-desc">{t('products.sortPriceHighLow')}</option>
                <option value="rating">{t('products.sortRating')}</option>
              </select>
            </div>

            <ActiveFilters
              search={debouncedSearch}
              categorySelected={!!selectedCategoryId}
              minPrice={minPrice}
              maxPrice={maxPrice}
              minRating={minRating}
              isFeatured={isFeatured}
              onClearAll={handleClearFilters}
            />

            {isFetching && !isLoading && (
              <div className={styles.refetchBadge} aria-live="polite" aria-atomic="true">
                <RefreshIcon className={styles.refetchIcon} aria-hidden="true" />
                <span>{t('common.updating')}</span>
              </div>
            )}
          </div>

          <QueryRenderer
            isLoading={isLoading}
            error={error}
            data={result}
            errorMessage={t('products.failedToLoadProducts')}
            isEmpty={(data) => !data || data.items.length === 0}
            loadingSkeleton={{ custom: <ProductsGridSkeleton count={PRODUCTS_PAGE_SIZE} /> }}
            emptyState={{
              icon: <GridIcon />,
              title: hasActiveFilters
                ? t('products.noProductsMatchFilters')
                : t('products.noProducts'),
              description: hasActiveFilters ? t('products.tryAdjustingSearch') : undefined,
              action: hasActiveFilters ? (
                <Button onClick={handleClearFilters}>{t('common.clear')}</Button>
              ) : undefined,
            }}
          >
            {(data) => (
              <ProductGrid
                products={data.items}
                totalCount={data.totalCount}
                currentPage={page}
                pageSize={PRODUCTS_PAGE_SIZE}
                onPageChange={setPage}
              />
            )}
          </QueryRenderer>
        </div>
      </div>
    </div>
  );
}

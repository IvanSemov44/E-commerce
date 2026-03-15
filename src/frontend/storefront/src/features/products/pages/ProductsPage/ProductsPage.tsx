import { useTranslation } from 'react-i18next';
import { usePerformanceMonitor } from '@/shared/hooks';
import { useGetProductsQuery } from '@/features/products/api/productApi';
import { useProductFilters } from '@/features/products/hooks/useProductFilters';
import { Button } from '@/shared/components/ui/Button';
import { CategoryFilter } from '@/features/products/components/CategoryFilter';
import { ProductsToolbar } from '@/features/products/components/ProductsToolbar';
import PageHeader from '@/shared/components/PageHeader';
import QueryRenderer from '@/shared/components/QueryRenderer';
import { GridIcon } from '@/shared/components/icons';
import { ProductFilters, ProductGrid, ProductsGridSkeleton } from '@/features/products/components';
import { PRODUCTS_PAGE_SIZE } from '@/features/products/constants';
import styles from './ProductsPage.module.css';

export function ProductsPage() {
  usePerformanceMonitor();
  const { t } = useTranslation();
  const filters = useProductFilters();

  const {
    data: result,
    isLoading,
    isFetching,
    error,
  } = useGetProductsQuery({
    page: filters.page,
    pageSize: PRODUCTS_PAGE_SIZE,
    categoryId: filters.selectedCategoryId,
    search: filters.debouncedSearch,
    minPrice: filters.minPrice,
    maxPrice: filters.maxPrice,
    minRating: filters.minRating,
    sortBy: filters.sortBy,
    isFeatured: filters.isFeatured,
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
            selectedCategoryId={filters.selectedCategoryId}
            onSelectCategory={filters.setSelectedCategoryId}
          />

          <div className={styles.filtersSection}>
            <ProductFilters
              minPrice={filters.minPrice}
              maxPrice={filters.maxPrice}
              minRating={filters.minRating}
              isFeatured={filters.isFeatured}
              onMinPriceChange={filters.setMinPrice}
              onMaxPriceChange={filters.setMaxPrice}
              onMinRatingChange={filters.setMinRating}
              onIsFeaturedChange={filters.setIsFeatured}
            />
          </div>
        </div>

        {/* Main Content */}
        <div className={styles.content}>
          <ProductsToolbar
            searchInput={filters.searchInput}
            sortBy={filters.sortBy}
            debouncedSearch={filters.debouncedSearch}
            selectedCategoryId={filters.selectedCategoryId}
            minPrice={filters.minPrice}
            maxPrice={filters.maxPrice}
            minRating={filters.minRating}
            isFeatured={filters.isFeatured}
            onSearchChange={filters.setSearchInput}
            onSortChange={filters.setSortBy}
            onClearFilters={filters.handleClearFilters}
            isFetching={isFetching}
            isLoading={isLoading}
          />

          <QueryRenderer
            isLoading={isLoading}
            error={error}
            data={result}
            errorMessage={t('products.failedToLoadProducts')}
            isEmpty={(data) => !data || data.items.length === 0}
            loadingSkeleton={{ custom: <ProductsGridSkeleton count={PRODUCTS_PAGE_SIZE} /> }}
            emptyState={{
              icon: <GridIcon />,
              title: filters.hasActiveFilters
                ? t('products.noProductsMatchFilters')
                : t('products.noProducts'),
              description: filters.hasActiveFilters ? t('products.tryAdjustingSearch') : undefined,
              action: filters.hasActiveFilters ? (
                <Button onClick={filters.handleClearFilters}>{t('common.clear')}</Button>
              ) : undefined,
            }}
          >
            {(data) => (
              <ProductGrid
                products={data.items}
                totalCount={data.totalCount}
                currentPage={filters.page}
                pageSize={PRODUCTS_PAGE_SIZE}
                onPageChange={filters.setPage}
              />
            )}
          </QueryRenderer>
        </div>
      </div>
    </div>
  );
}

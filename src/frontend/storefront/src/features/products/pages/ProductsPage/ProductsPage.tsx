import { useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePerformanceMonitor } from '@/shared/hooks';
import { useGetProductsQuery } from '@/features/products/api';
import { useProductFilters } from '@/features/products/hooks';
import { Button } from '@/shared/components/ui/Button';
import { CategoryFilter, ProductsToolbar } from '@/features/products/components';
import PageHeader from '@/shared/components/PageHeader';
import QueryRenderer from '@/shared/components/QueryRenderer';
import { GridIcon } from '@/shared/components/icons';
import { ProductFilters, ProductGrid, ProductsGridSkeleton } from '@/features/products/components';
import { PRODUCTS_PAGE_SIZE } from '@/features/products/constants';
import styles from './ProductsPage.module.css';

export function ProductsPage() {
  usePerformanceMonitor();
  const { t } = useTranslation();
  const [, setSearchParams] = useSearchParams();
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
    search: filters.search,
    minPrice: filters.minPrice,
    maxPrice: filters.maxPrice,
    minRating: filters.minRating,
    sortBy: filters.sortBy,
    isFeatured: filters.isFeatured,
  });

  const handlePageChange = (page: number) => {
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        if (page > 1) next.set('page', page.toString());
        else next.delete('page');
        return next;
      },
      { replace: true }
    );
  };

  const handleClearFilters = () => setSearchParams({}, { replace: true });

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
          <CategoryFilter />

          <div className={styles.filtersSection}>
            <ProductFilters />
          </div>
        </div>

        {/* Main Content */}
        <div className={styles.content}>
          <ProductsToolbar isRefetching={isFetching && !isLoading} filters={filters} />

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
                <Button onClick={handleClearFilters}>{t('common.clear')}</Button>
              ) : undefined,
            }}
          >
            {(data) => (
              <ProductGrid
                products={data.items}
                totalCount={data.totalCount}
                currentPage={filters.page}
                pageSize={PRODUCTS_PAGE_SIZE}
                onPageChange={handlePageChange}
              />
            )}
          </QueryRenderer>
        </div>
      </div>
    </div>
  );
}

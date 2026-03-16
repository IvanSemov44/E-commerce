import { useTranslation } from 'react-i18next';
import { ProductCard } from '../ProductCard/ProductCard';
import PaginatedView from '@/shared/components/Pagination';
import type { ProductGridProps } from './ProductGrid.types';
import styles from './ProductGrid.module.css';

export function ProductGrid({
  products,
  totalCount,
  currentPage,
  pageSize,
  onPageChange,
}: ProductGridProps) {
  const { t } = useTranslation();

  return (
    <>
      {/* Results Count */}
      <div className={styles.resultsCount}>
        {t('common.showing')} <strong>{products.length}</strong> {t('common.of')}{' '}
        <strong>{totalCount}</strong> {t('common.products')}
      </div>

      <PaginatedView
        items={products}
        totalCount={totalCount}
        currentPage={currentPage}
        pageSize={pageSize}
        onPageChange={onPageChange}
        gridClassName={styles.grid}
        renderItem={(product) => (
          <ProductCard
            key={product.id}
            id={product.id}
            name={product.name}
            slug={product.slug}
            price={product.price}
            compareAtPrice={product.compareAtPrice}
            imageUrl={product.images[0]?.url}
            rating={product.averageRating}
            reviewCount={product.reviewCount}
            stockQuantity={product.stockQuantity}
          />
        )}
      />
    </>
  );
}

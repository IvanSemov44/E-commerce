import { useTranslation } from 'react-i18next';
import ProductCard from '../ProductCard/ProductCard';
import PaginatedView from '@/shared/components/Pagination';
import type { Product } from '@/shared/types';
import styles from './ProductGrid.module.css';

interface ProductGridProps {
  products: Product[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  onPageChange: (page: number) => void;
}

export default function ProductGrid({
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
            rating={Math.round(product.averageRating)}
            reviewCount={product.reviewCount}
          />
        )}
      />
    </>
  );
}

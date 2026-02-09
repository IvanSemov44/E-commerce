import ProductCard from '../../../components/ProductCard';
import PaginatedView from '../../../components/PaginatedView';
import type { Product } from '../../../types';
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
  return (
    <>
      {/* Results Count */}
      <div className={styles.resultsCount}>
        Showing <strong>{products.length}</strong> of <strong>{totalCount}</strong> products
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

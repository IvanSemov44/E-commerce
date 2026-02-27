/**
 * Products Grid Skeleton - Skeleton for products grid loading state
 */

import ProductSkeleton from './ProductSkeleton';
import styles from './Skeleton.module.css';

interface ProductsGridSkeletonProps {
  count?: number;
}

export default function ProductsGridSkeleton({ count = 12 }: ProductsGridSkeletonProps) {
  return (
    <div className={styles.productsGrid}>
      {[...Array(count)].map((_, i) => (
        <ProductSkeleton key={i} />
      ))}
    </div>
  );
}

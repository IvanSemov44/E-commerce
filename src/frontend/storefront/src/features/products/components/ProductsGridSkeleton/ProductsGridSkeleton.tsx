import ProductSkeleton from '../ProductSkeleton/ProductSkeleton';
import styles from './ProductsGridSkeleton.module.css';

interface ProductsGridSkeletonProps {
  count?: number;
}

export default function ProductsGridSkeleton({ count = 12 }: ProductsGridSkeletonProps) {
  return (
    <div className={styles.productsGrid}>
      {Array.from({ length: count }).map((_, i) => (
        <ProductSkeleton key={i} />
      ))}
    </div>
  );
}

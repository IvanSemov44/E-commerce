/**
 * Products Grid Skeleton - Skeleton for products grid loading state
 */

import ProductSkeleton from './ProductSkeleton';

interface ProductsGridSkeletonProps {
  count?: number;
}

export default function ProductsGridSkeleton({ count = 12 }: ProductsGridSkeletonProps) {
  return (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: '1.5rem' }}>
      {[...Array(count)].map((_, i) => (
        <ProductSkeleton key={i} />
      ))}
    </div>
  );
}

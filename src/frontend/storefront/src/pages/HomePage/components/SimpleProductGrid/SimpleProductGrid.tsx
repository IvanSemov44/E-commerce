import type { Product } from '@/shared/types';
import { ProductCard } from '@/features/products/components/ProductCard/ProductCard';
import styles from './SimpleProductGrid.module.css';

export interface SimpleProductGridProps {
  products: Product[];
}

/**
 * SimpleProductGrid - A grid component for displaying products without pagination.
 * Used on pages like HomePage where all products are shown at once.
 */
export function SimpleProductGrid({ products }: SimpleProductGridProps) {
  return (
    <div className={styles.grid}>
      {products.map((product) => (
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
      ))}
    </div>
  );
}

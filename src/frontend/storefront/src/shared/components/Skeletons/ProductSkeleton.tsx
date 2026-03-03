/**
 * Product Skeleton - Skeleton for product card loading state
 */

import Skeleton from './Skeleton';
import styles from './Skeleton.module.css';

export default function ProductSkeleton() {
  return (
    <div className={styles.productCard}>
      {/* Product Image */}
      <Skeleton height={250} className={styles.productImage} />

      {/* Product Info */}
      <div className={styles.productInfo}>
        {/* Product Name */}
        <Skeleton height={20} className={styles.productName} />

        {/* Price */}
        <div className={styles.priceRow}>
          <Skeleton width="40%" height={24} />
        </div>

        {/* Rating */}
        <div className={styles.ratingRow}>
          <Skeleton width="60%" height={16} />
        </div>

        {/* Add to Cart Button */}
        <Skeleton height={40} className={styles.button} />
      </div>
    </div>
  );
}

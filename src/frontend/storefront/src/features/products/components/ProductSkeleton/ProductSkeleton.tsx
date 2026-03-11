import { Skeleton } from '@/shared/components/Skeletons';
import styles from './ProductSkeleton.module.css';

export default function ProductSkeleton() {
  return (
    <div className={styles.productCard}>
      <Skeleton height={250} className={styles.productImage} />
      <div className={styles.productInfo}>
        <Skeleton height={20} className={styles.productName} />
        <div className={styles.priceRow}>
          <Skeleton width="40%" height={24} />
        </div>
        <div className={styles.ratingRow}>
          <Skeleton width="60%" height={16} />
        </div>
        <Skeleton height={40} className={styles.button} />
      </div>
    </div>
  );
}

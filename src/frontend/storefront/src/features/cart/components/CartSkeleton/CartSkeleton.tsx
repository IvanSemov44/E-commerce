import { Skeleton, SkeletonLabelRow } from '@/shared/components/Skeletons';
import styles from './CartSkeleton.module.css';

export default function CartSkeleton() {
  return (
    <div className={styles.cartContainer}>
      <div className={styles.cartItems}>
        {Array.from({ length: 3 }).map((_, i) => (
          <div key={i} className={styles.cartItem}>
            <Skeleton width={80} height={80} />
            <div className={styles.cartItemInfo}>
              <Skeleton height={20} width="60%" />
              <Skeleton height={18} width="40%" />
              <Skeleton height={18} width="30%" />
            </div>
            <div className={styles.cartItemActions}>
              <Skeleton height={40} width={100} />
              <Skeleton height={24} width="20%" />
            </div>
          </div>
        ))}
      </div>

      <div className={styles.cartSummary}>
        <Skeleton height={24} width="40%" />
        <SkeletonLabelRow
          items={[
            { width: '50%', height: 20 },
            { width: '30%', height: 20 },
          ]}
        />
        <SkeletonLabelRow
          items={[
            { width: '50%', height: 20 },
            { width: '30%', height: 20 },
          ]}
        />
        <Skeleton height={44} width="100%" className={styles.marginTop} />
      </div>
    </div>
  );
}

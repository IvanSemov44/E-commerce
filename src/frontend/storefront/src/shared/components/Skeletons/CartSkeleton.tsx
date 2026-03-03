/**
 * Cart Skeleton - Skeleton for cart page loading state
 */

import Skeleton from './Skeleton';
import styles from './Skeleton.module.css';

export default function CartSkeleton() {
  return (
    <div className={styles.cartContainer}>
      {/* Cart Items */}
      <div className={styles.cartItems}>
        {[...Array(3)].map((_, i) => (
          <div key={i} className={styles.cartItem}>
            {/* Product Image */}
            <Skeleton width={80} height={80} />

            {/* Product Info */}
            <div className={styles.cartItemInfo}>
              <Skeleton height={20} width="60%" />
              <Skeleton height={18} width="40%" />
              <Skeleton height={18} width="30%" />
            </div>

            {/* Quantity & Price */}
            <div className={styles.cartItemActions}>
              <Skeleton height={40} width={100} />
              <Skeleton height={24} width="20%" />
            </div>
          </div>
        ))}
      </div>

      {/* Cart Summary */}
      <div className={styles.cartSummary}>
        <Skeleton height={24} width="40%" />
        <div className={styles.summaryRow}>
          <Skeleton height={20} width="50%" />
          <Skeleton height={20} width="30%" />
        </div>
        <div className={styles.summaryRow}>
          <Skeleton height={20} width="50%" />
          <Skeleton height={20} width="30%" />
        </div>
        <Skeleton height={44} width="100%" className={styles.marginTop} />
      </div>
    </div>
  );
}

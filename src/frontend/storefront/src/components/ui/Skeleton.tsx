import styles from './Skeleton.module.css';

interface SkeletonProps {
  variant?: 'text' | 'circular' | 'rectangular' | 'rounded';
  width?: string | number;
  height?: string | number;
  className?: string;
  animation?: 'pulse' | 'wave' | 'none';
}

export default function Skeleton({
  variant = 'text',
  width,
  height,
  className = '',
  animation = 'pulse',
}: SkeletonProps) {
  const style: React.CSSProperties = {
    width: typeof width === 'number' ? `${width}px` : width,
    height: typeof height === 'number' ? `${height}px` : height,
  };

  return (
    <span
      className={`${styles.skeleton} ${styles[variant]} ${styles[animation]} ${className}`}
      style={style}
    />
  );
}

// Product Card Skeleton
export function ProductCardSkeleton() {
  return (
    <div className={styles.productCard}>
      <div className={styles.productImage}>
        <Skeleton variant="rectangular" height={200} />
      </div>
      <div className={styles.productContent}>
        <Skeleton variant="text" width="60%" height={20} />
        <Skeleton variant="text" width="80%" height={16} />
        <div className={styles.productFooter}>
          <Skeleton variant="text" width={60} height={24} />
          <Skeleton variant="rounded" width={100} height={36} />
        </div>
      </div>
    </div>
  );
}

// Cart Item Skeleton
export function CartItemSkeleton() {
  return (
    <div className={styles.cartItem}>
      <Skeleton variant="rounded" width={80} height={80} />
      <div className={styles.cartItemContent}>
        <Skeleton variant="text" width="70%" height={18} />
        <Skeleton variant="text" width="40%" height={14} />
        <div className={styles.cartItemFooter}>
          <Skeleton variant="rounded" width={100} height={32} />
          <Skeleton variant="text" width={60} height={20} />
        </div>
      </div>
    </div>
  );
}

// Order Card Skeleton
export function OrderCardSkeleton() {
  return (
    <div className={styles.orderCard}>
      <div className={styles.orderHeader}>
        <Skeleton variant="text" width={150} height={16} />
        <Skeleton variant="rounded" width={80} height={24} />
      </div>
      <div className={styles.orderItems}>
        <Skeleton variant="text" width="60%" height={14} />
        <Skeleton variant="text" width="40%" height={14} />
      </div>
      <div className={styles.orderFooter}>
        <Skeleton variant="text" width={80} height={20} />
        <Skeleton variant="rounded" width={100} height={36} />
      </div>
    </div>
  );
}

// Product Grid Skeleton
export function ProductGridSkeleton({ count = 8 }: { count?: number }) {
  return (
    <div className={styles.productGrid}>
      {Array.from({ length: count }).map((_, index) => (
        <ProductCardSkeleton key={index} />
      ))}
    </div>
  );
}

// Cart Skeleton
export function CartSkeleton() {
  return (
    <div className={styles.cart}>
      <div className={styles.cartItems}>
        {Array.from({ length: 3 }).map((_, index) => (
          <CartItemSkeleton key={index} />
        ))}
      </div>
      <div className={styles.cartSummary}>
        <Skeleton variant="text" width="60%" height={24} />
        <Skeleton variant="text" width="40%" height={16} />
        <Skeleton variant="text" width="50%" height={16} />
        <Skeleton variant="rounded" width="100%" height={44} />
      </div>
    </div>
  );
}

// Order History Skeleton
export function OrderHistorySkeleton({ count = 3 }: { count?: number }) {
  return (
    <div className={styles.orderHistory}>
      {Array.from({ length: count }).map((_, index) => (
        <OrderCardSkeleton key={index} />
      ))}
    </div>
  );
}

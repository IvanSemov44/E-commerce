import { Skeleton, SkeletonCard, SkeletonLabelRow } from '@/shared/components/Skeletons';
import styles from './WishlistSkeleton.module.css';

interface WishlistSkeletonProps {
  count?: number;
}

export default function WishlistSkeleton({ count = 8 }: WishlistSkeletonProps) {
  return (
    <div className={styles.grid}>
      {Array.from({ length: count }).map((_, i) => (
        <SkeletonCard key={i} imageHeight={220} lines={[{ width: '80%', height: 20 }]}>
          <SkeletonLabelRow
            items={[
              { width: '35%', height: 18 },
              { width: '25%', height: 14 },
            ]}
          />
          <div className={styles.actions}>
            <Skeleton width="100%" height={40} variant="rounded" animation="wave" />
            <Skeleton width={40} height={40} variant="circle" animation="wave" />
          </div>
        </SkeletonCard>
      ))}
    </div>
  );
}

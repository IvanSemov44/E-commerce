import { SkeletonLabelRow } from '@/shared/components/Skeletons';
import styles from './OrdersListSkeleton.module.css';

interface OrdersListSkeletonProps {
  count?: number;
}

export default function OrdersListSkeleton({ count = 6 }: OrdersListSkeletonProps) {
  return (
    <div className={styles.list}>
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className={styles.card}>
          <SkeletonLabelRow
            items={[
              { width: '35%', height: 18 },
              { width: 110, height: 28 },
            ]}
          />
          <SkeletonLabelRow
            items={[
              { width: '22%', height: 14 },
              { width: '18%', height: 14 },
              { width: '28%', height: 14 },
            ]}
            between={false}
            wrap
          />
        </div>
      ))}
    </div>
  );
}

import { Skeleton, SkeletonLabelRow } from '@/shared/components/Skeletons';
import styles from './OrderDetailSkeleton.module.css';

export function OrderDetailSkeleton() {
  return (
    <div className={styles.container}>
      <div className={styles.backButtonRow}>
        <Skeleton width={150} height={36} variant="rounded" animation="wave" />
      </div>

      <div className={styles.section}>
        <SkeletonLabelRow
          items={[
            { width: '40%', height: 24 },
            { width: 120, height: 28 },
          ]}
        />
        <Skeleton width="30%" height={16} variant="rounded" animation="wave" />
      </div>

      <div className={styles.section}>
        <Skeleton width="25%" height={20} variant="rounded" animation="wave" />
        {Array.from({ length: 2 }).map((_, i) => (
          <div key={i} className={styles.itemRow}>
            <Skeleton width={80} height={80} variant="rounded" animation="wave" />
            <div className={styles.itemContent}>
              <Skeleton width="55%" height={16} variant="rounded" animation="wave" />
              <Skeleton width="35%" height={14} variant="rounded" animation="wave" />
              <Skeleton width="25%" height={14} variant="rounded" animation="wave" />
            </div>
          </div>
        ))}
      </div>

      <div className={styles.section}>
        <Skeleton width="30%" height={20} variant="rounded" animation="wave" />
        {Array.from({ length: 4 }).map((_, i) => (
          <SkeletonLabelRow
            key={i}
            items={[
              { width: '30%', height: 14 },
              { width: '20%', height: 14 },
            ]}
          />
        ))}
      </div>
    </div>
  );
}

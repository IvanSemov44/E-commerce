import { Skeleton, SkeletonLabelRow } from '@/shared/components/Skeletons';
import { Card } from '@/shared/components/ui/Card';
import { REVIEW_SKELETON_COUNT } from '@/features/products/constants';
import styles from './ReviewSkeleton.module.css';

export function ReviewSkeleton() {
  return (
    <div className={styles.grid}>
      {Array.from({ length: REVIEW_SKELETON_COUNT }).map((_, i) => (
        <Card key={i} variant="bordered" padding="lg">
          <SkeletonLabelRow
            items={[
              { width: '55%', height: 16 },
              { width: 90, height: 14 },
            ]}
          />
          <Skeleton width="100%" height={12} variant="rounded" animation="wave" />
          <Skeleton width="90%" height={12} variant="rounded" animation="wave" />
          <Skeleton width="70%" height={12} variant="rounded" animation="wave" />
          <SkeletonLabelRow
            items={[
              { width: '28%', height: 12 },
              { width: '18%', height: 12 },
            ]}
            between={false}
          />
        </Card>
      ))}
    </div>
  );
}

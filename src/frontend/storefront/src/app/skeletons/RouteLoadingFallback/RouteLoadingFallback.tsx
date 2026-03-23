import { Skeleton, SkeletonCard, SkeletonLabelRow } from '@/shared/components/Skeletons';
import styles from './RouteLoadingFallback.module.css';

export function RouteLoadingFallback() {
  return (
    <div className={styles.container} role="status" aria-live="polite" aria-label="Loading route">
      <SkeletonLabelRow
        items={[
          { width: '35%', height: 28 },
          { width: 110, height: 36 },
        ]}
      />

      <div className={styles.filtersRow}>
        <Skeleton width="100%" height={44} variant="rounded" animation="wave" />
      </div>

      <div className={styles.grid}>
        {Array.from({ length: 6 }).map((_, i) => (
          <SkeletonCard key={i} imageHeight={180} />
        ))}
      </div>
    </div>
  );
}

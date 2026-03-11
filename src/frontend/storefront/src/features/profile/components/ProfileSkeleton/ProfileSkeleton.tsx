import { Skeleton } from '@/shared/components/Skeletons';
import styles from './ProfileSkeleton.module.css';

export default function ProfileSkeleton() {
  return (
    <div className={styles.profileContainer}>
      <div className={styles.profileHeader}>
        <Skeleton width={120} height={120} variant="circle" />
        <div className={styles.headerInfo}>
          <Skeleton height={28} width="40%" />
          <Skeleton height={20} width="30%" />
        </div>
      </div>

      <div className={styles.profileForm}>
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className={styles.formGroup}>
            <Skeleton height={20} width="30%" />
            <Skeleton height={40} width="100%" />
          </div>
        ))}
        <Skeleton height={44} width="100%" className={styles.marginTop} />
      </div>
    </div>
  );
}

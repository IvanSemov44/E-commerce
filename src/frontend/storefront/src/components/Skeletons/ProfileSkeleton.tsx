/**
 * Profile Skeleton - Skeleton for profile page loading state
 */

import Skeleton from './Skeleton';
import styles from './Skeleton.module.css';

export default function ProfileSkeleton() {
  return (
    <div className={styles.profileContainer}>
      {/* Header */}
      <div className={styles.profileHeader}>
        <Skeleton width={120} height={120} variant="circle" />
        <div className={styles.headerInfo}>
          <Skeleton height={28} width="40%" />
          <Skeleton height={20} width="30%" />
        </div>
      </div>

      {/* Form Fields */}
      <div className={styles.profileForm}>
        {/* First Name */}
        <div className={styles.formGroup}>
          <Skeleton height={20} width="30%" />
          <Skeleton height={40} width="100%" />
        </div>

        {/* Last Name */}
        <div className={styles.formGroup}>
          <Skeleton height={20} width="30%" />
          <Skeleton height={40} width="100%" />
        </div>

        {/* Email */}
        <div className={styles.formGroup}>
          <Skeleton height={20} width="30%" />
          <Skeleton height={40} width="100%" />
        </div>

        {/* Phone */}
        <div className={styles.formGroup}>
          <Skeleton height={20} width="30%" />
          <Skeleton height={40} width="100%" />
        </div>

        {/* Submit Button */}
        <Skeleton height={44} width="100%" className={styles.marginTop} />
      </div>
    </div>
  );
}

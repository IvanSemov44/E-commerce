import { Skeleton, SkeletonCard } from '@/shared/components/Skeletons';
import styles from './AppShellSkeleton.module.css';

/**
 * AppShellSkeleton — Phase 2 bootstrap loading placeholder.
 * Mirrors the real AppShell header layout so the transition
 * to actual content causes zero layout shift.
 */
export function AppShellSkeleton() {
  return (
    <div className={styles.shell} aria-busy="true" aria-label="Loading application">
      {/* AnnouncementBar placeholder */}
      <div className={styles.announcementBar} />

      {/* Header skeleton */}
      <header className={styles.header}>
        <div className={styles.nav}>
          {/* Logo */}
          <div className={styles.logoGroup}>
            <Skeleton variant="circle" width={32} height={32} animation="wave" />
            <Skeleton variant="rounded" width={70} height={20} animation="wave" />
          </div>

          {/* Nav links */}
          <div className={styles.navLinks}>
            {[80, 64, 72, 56].map((w) => (
              <Skeleton key={w} variant="rounded" width={w} height={16} animation="wave" />
            ))}
          </div>

          {/* Search bar */}
          <Skeleton
            variant="rounded"
            width={220}
            height={36}
            animation="wave"
            className={styles.search}
          />

          {/* Icon group: wishlist, cart, user, theme, lang */}
          <div className={styles.iconGroup}>
            {[32, 32, 32, 32, 32].map((_, i) => (
              <Skeleton key={i} variant="circle" width={32} height={32} animation="wave" />
            ))}
          </div>
        </div>
      </header>

      {/* Content area skeleton */}
      <main className={styles.content}>
        {/* Hero-like wide block */}
        <Skeleton
          variant="rounded"
          width="100%"
          height={200}
          animation="wave"
          className={styles.hero}
        />

        {/* Grid of cards */}
        <div className={styles.grid}>
          {Array.from({ length: 6 }).map((_, i) => (
            <SkeletonCard key={i} imageHeight={180} />
          ))}
        </div>
      </main>
    </div>
  );
}

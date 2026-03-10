import type { CSSProperties } from 'react';
import TopLoadingBar from '../TopLoadingBar/TopLoadingBar';
import AppShellSkeleton from '../AppShellSkeleton/AppShellSkeleton';
import {
  BOOTSTRAP_TOP_BAR_DELAY_MS,
  BOOTSTRAP_FULL_FALLBACK_DELAY_MS,
} from '@/shared/lib/utils/constants';
import styles from './AppBootstrapLoading.module.css';

export default function AppBootstrapLoading() {
  const delayVars = {
    '--bootstrap-top-bar-delay': `${BOOTSTRAP_TOP_BAR_DELAY_MS}ms`,
    '--bootstrap-full-fallback-delay': `${BOOTSTRAP_FULL_FALLBACK_DELAY_MS}ms`,
  } as CSSProperties;

  return (
    <>
      <div className={styles.topBarDelay} style={delayVars}>
        <TopLoadingBar />
      </div>
      <div className={styles.fullFallbackDelay} style={delayVars}>
        <AppShellSkeleton />
      </div>
    </>
  );
}

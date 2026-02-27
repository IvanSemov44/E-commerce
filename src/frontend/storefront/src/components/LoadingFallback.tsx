import styles from './LoadingFallback.module.css';

interface LoadingFallbackProps {
  message?: string;
}

/**
 * Reusable loading fallback component for lazy-loaded routes
 * Replaces inline styles previously used in App.tsx
 */
export default function LoadingFallback({ message = 'Loading page...' }: LoadingFallbackProps) {
  return (
    <div className={styles.container}>
      <div className={styles.spinner} aria-hidden="true">
        <div className={styles.spinnerRing}></div>
      </div>
      <p className={styles.message}>{message}</p>
    </div>
  );
}

import styles from './TopLoadingBar.module.css';

export default function TopLoadingBar() {
  return (
    <div className={styles.container} role="status" aria-live="polite" aria-label="Loading">
      <div className={styles.indicator} />
    </div>
  );
}

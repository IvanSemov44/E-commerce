/**
 * Error Page - Fallback UI displayed when error boundary catches an error
 */

import { useNavigate } from 'react-router-dom';
import Button from '../ui/Button';
import styles from './ErrorPage.module.css';

interface ErrorPageProps {
  error: Error | null;
  isDevelopment: boolean;
  onReset: () => void;
}

export default function ErrorPage({ error, isDevelopment, onReset }: ErrorPageProps) {
  const navigate = useNavigate();

  const handleGoHome = () => {
    onReset();
    navigate('/');
  };

  return (
    <div className={styles.container}>
      <div className={styles.content}>
        <div className={styles.icon}>⚠️</div>

        <h1 className={styles.title}>Oops! Something went wrong</h1>

        <p className={styles.description}>
          We encountered an unexpected error. Please try refreshing the page or going back home.
        </p>

        {isDevelopment && error && (
          <div className={styles.errorDetails}>
            <p className={styles.errorTitle}>Error Details (Development Only)</p>
            <pre className={styles.errorStack}>{error.message}</pre>
          </div>
        )}

        <div className={styles.actions}>
          <Button onClick={handleGoHome} size="lg" className={styles.primaryButton}>
            Go to Home
          </Button>
          <Button
            onClick={() => window.location.reload()}
            variant="outline"
            size="lg"
            className={styles.secondaryButton}
          >
            Refresh Page
          </Button>
        </div>

        <p className={styles.support}>
          If the problem persists, please{' '}
          <a href="mailto:support@example.com" className={styles.link}>
            contact support
          </a>
        </p>
      </div>
    </div>
  );
}

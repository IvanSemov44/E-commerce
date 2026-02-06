/**
 * Error Page Component - Admin Panel
 * Fallback UI displayed when error boundary catches error
 */

import { useNavigate } from 'react-router-dom';
import Button from './ui/Button';
import styles from './ErrorPage.module.css';

interface ErrorPageProps {
  error: Error | null;
  isDevelopment: boolean;
  onReset: () => void;
}

export default function ErrorPage({
  error,
  isDevelopment,
  onReset,
}: ErrorPageProps) {
  const navigate = useNavigate();

  return (
    <div className={styles.container}>
      <div className={styles.content}>
        <div className={styles.icon}>⚠️</div>

        <h1 className={styles.title}>Oops! Something went wrong</h1>

        <p className={styles.description}>
          An unexpected error occurred while rendering the page. Our team has been notified.
        </p>

        {isDevelopment && error && (
          <details className={styles.errorDetails}>
            <summary>Error Details (Development Only)</summary>
            <pre className={styles.errorStack}>
              {error.message}
              {'\n\n'}
              {error.stack}
            </pre>
          </details>
        )}

        <div className={styles.actions}>
          <Button
            onClick={() => {
              onReset();
              window.location.href = '/';
            }}
            size="md"
          >
            Go to Dashboard
          </Button>
          <Button
            onClick={() => window.location.reload()}
            variant="secondary"
            size="md"
          >
            Refresh Page
          </Button>
        </div>

        <p className={styles.footer}>
          If the problem persists, contact{' '}
          <a href="mailto:support@example.com">support@example.com</a>
        </p>
      </div>
    </div>
  );
}

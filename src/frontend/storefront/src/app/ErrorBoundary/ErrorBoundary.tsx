import React from 'react';
import type { ReactNode } from 'react';
import { logger } from '@/shared/lib/utils/logger';
import { telemetry } from '@/shared/lib/utils/telemetry';
import styles from './ErrorBoundary.module.css';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export default class ErrorBoundary extends React.Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error) {
    logger.error('ErrorBoundary', 'Unhandled render error', error);
    telemetry.track('error.boundary', {
      message: error.message,
      name: error.name,
    });
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className={styles.errorContainer}>
          <div className={styles.errorCard}>
            <h1 className={styles.errorTitle}>Something went wrong</h1>
            <p className={styles.errorMessage}>
              An error occurred while loading the application. Please try refreshing the page.
            </p>
            <details className={styles.detailsSection}>
              <summary className={styles.detailsSummary}>Error details</summary>
              <pre className={styles.errorDetails}>{this.state.error?.message}</pre>
            </details>
            <button onClick={() => window.location.reload()} className={styles.refreshButton}>
              Refresh Page
            </button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

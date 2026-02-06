import type { ReactNode } from 'react';
import { Card, CardContent } from './ui/Card';

import styles from './QueryRenderer.module.css';

// Default isEmpty function (defined outside component to prevent recreation on every render)
const defaultIsEmpty = <T,>(data: T): boolean =>
  !data || (Array.isArray(data) && data.length === 0);

interface QueryRendererProps<T> {
  isLoading: boolean;
  error: unknown;
  data: T | undefined;
  isEmpty?: (data: T) => boolean;
  emptyMessage?: string;
  emptyTitle?: string;
  children: (data: T) => ReactNode;
}

export default function QueryRenderer<T>({
  isLoading,
  error,
  data,
  isEmpty = defaultIsEmpty,
  emptyMessage = 'No data available',
  emptyTitle = 'No Results',
  children,
}: QueryRendererProps<T>) {
  if (isLoading) {
    return (
      <Card variant="elevated">
        <CardContent>
          <div className={`${styles.state} ${styles.loading}`}>
            Loading...
          </div>
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card variant="elevated">
        <CardContent>
          <div className={`${styles.state} ${styles.error}`}>
            Failed to load data. Please try again.
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!data || (isEmpty && isEmpty(data))) {
    return (
      <Card variant="elevated">
        <CardContent>
          <div className={`${styles.state} ${styles.empty}`}>
            <div className={styles.emptyTitle}>{emptyTitle}</div>
            <div>{emptyMessage}</div>
          </div>
        </CardContent>
      </Card>
    );
  }

  return <>{children(data)}</>;
}

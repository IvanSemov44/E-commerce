import type { ReactNode } from 'react';
import { Card, CardContent } from './ui/Card';

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
  isEmpty,
  emptyMessage = 'No data available',
  emptyTitle = 'No Results',
  children,
}: QueryRendererProps<T>) {
  if (isLoading) {
    return (
      <Card variant="elevated">
        <CardContent>
          <div style={{ textAlign: 'center', padding: '2rem', color: '#64748b' }}>
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
          <div style={{ textAlign: 'center', padding: '2rem', color: '#ef4444' }}>
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
          <div style={{ textAlign: 'center', padding: '2rem', color: '#94a3b8' }}>
            <div style={{ fontWeight: 600, marginBottom: '0.5rem' }}>{emptyTitle}</div>
            <div>{emptyMessage}</div>
          </div>
        </CardContent>
      </Card>
    );
  }

  return <>{children(data)}</>;
}

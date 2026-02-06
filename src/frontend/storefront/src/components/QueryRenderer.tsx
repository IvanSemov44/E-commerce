import ErrorAlert from './ErrorAlert';
import LoadingSkeleton from './LoadingSkeleton';
import EmptyState from './EmptyState';

interface QueryRendererProps<T> {
  isLoading: boolean;
  error: any;
  data: T | undefined;
  isEmpty?: (data: T) => boolean;
  loadingSkeleton?: {
    count?: number;
    type?: 'card' | 'text' | 'image';
  };
  emptyState?: {
    icon?: React.ReactNode;
    title: string;
    description?: string;
    action?: React.ReactNode;
  };
  errorMessage?: string;
  children: (data: T) => React.ReactNode;
}

export default function QueryRenderer<T>({
  isLoading,
  error,
  data,
  isEmpty = (data) => !data || (Array.isArray(data) && data.length === 0),
  loadingSkeleton = { count: 4, type: 'card' },
  emptyState,
  errorMessage = 'Failed to load data. Please try again.',
  children,
}: QueryRendererProps<T>) {
  if (error) {
    return <ErrorAlert message={errorMessage} />;
  }

  if (isLoading) {
    // Use custom loading component if provided
    if (loadingSkeleton.custom) {
      return <>{loadingSkeleton.custom}</>;
    }
    
    return (
      <LoadingSkeleton
        count={loadingSkeleton.count}
        type={loadingSkeleton.type}
      />
    );
  }

  if (!data || isEmpty(data)) {
    if (!emptyState) return null;

    return (
      <EmptyState
        icon={emptyState.icon}
        title={emptyState.title}
        description={emptyState.description}
        action={emptyState.action}
      />
    );
  }

  return <>{children(data)}</>;
}

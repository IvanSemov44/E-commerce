import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import QueryRenderer from './QueryRenderer';

vi.mock('../ErrorAlert', () => ({
  default: ({ message }: { message: string }) => <div data-testid="error-alert">{message}</div>,
}));

vi.mock('../LoadingSkeleton', () => ({
  default: ({ count, type }: { count?: number; type?: string }) => (
    <div data-testid="loading-skeleton">{`${count ?? 'none'}-${type ?? 'none'}`}</div>
  ),
}));

vi.mock('../EmptyState', () => ({
  default: ({ title, description }: { title: string; description?: string }) => (
    <div data-testid="empty-state">{`${title}${description ? `:${description}` : ''}`}</div>
  ),
}));

describe('QueryRenderer', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders error state when error exists', () => {
    render(
      <QueryRenderer
        isLoading={false}
        error={new Error('boom')}
        data={undefined}
      >
        {() => <div>Content</div>}
      </QueryRenderer>
    );

    expect(screen.getByTestId('error-alert')).toHaveTextContent('Failed to load data. Please try again.');
  });

  it('renders custom error message', () => {
    render(
      <QueryRenderer
        isLoading={false}
        error={new Error('boom')}
        data={undefined}
        errorMessage="Custom error"
      >
        {() => <div>Content</div>}
      </QueryRenderer>
    );

    expect(screen.getByTestId('error-alert')).toHaveTextContent('Custom error');
  });

  it('renders loading skeleton with defaults when loading', () => {
    render(
      <QueryRenderer
        isLoading
        error={null}
        data={undefined}
      >
        {() => <div>Content</div>}
      </QueryRenderer>
    );

    expect(screen.getByTestId('loading-skeleton')).toHaveTextContent('4-card');
  });

  it('renders custom loading skeleton when provided', () => {
    render(
      <QueryRenderer
        isLoading
        error={null}
        data={undefined}
        loadingSkeleton={{ custom: <div data-testid="custom-loader">Custom Loader</div> }}
      >
        {() => <div>Content</div>}
      </QueryRenderer>
    );

    expect(screen.getByTestId('custom-loader')).toBeInTheDocument();
    expect(screen.queryByTestId('loading-skeleton')).not.toBeInTheDocument();
  });

  it('renders null on empty data without emptyState config', () => {
    const { container } = render(
      <QueryRenderer
        isLoading={false}
        error={null}
        data={[] as string[]}
      >
        {() => <div>Content</div>}
      </QueryRenderer>
    );

    expect(container).toBeEmptyDOMElement();
  });

  it('renders empty state when data is empty and config is provided', () => {
    render(
      <QueryRenderer
        isLoading={false}
        error={null}
        data={[] as string[]}
        emptyState={{ title: 'No items', description: 'Try again later' }}
      >
        {() => <div>Content</div>}
      </QueryRenderer>
    );

    expect(screen.getByTestId('empty-state')).toHaveTextContent('No items:Try again later');
  });

  it('uses custom isEmpty function', () => {
    render(
      <QueryRenderer
        isLoading={false}
        error={null}
        data={{ count: 0 }}
        isEmpty={(data) => data.count === 0}
        emptyState={{ title: 'No results' }}
      >
        {() => <div>Content</div>}
      </QueryRenderer>
    );

    expect(screen.getByTestId('empty-state')).toHaveTextContent('No results');
  });

  it('renders children when data is available', () => {
    render(
      <QueryRenderer
        isLoading={false}
        error={null}
        data={{ name: 'Phone' }}
      >
        {(data) => <div>{data.name}</div>}
      </QueryRenderer>
    );

    expect(screen.getByText('Phone')).toBeInTheDocument();
  });
});

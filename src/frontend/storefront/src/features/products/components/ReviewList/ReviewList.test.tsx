import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ReviewList } from './ReviewList';

vi.mock('@/shared/components/ui/Card', () => ({
  Card: ({ children }: { children: React.ReactNode }) => <div data-testid="card">{children}</div>,
}));

vi.mock('@/shared/components/ErrorAlert', () => ({
  default: ({ message }: { message: string }) => <div data-testid="error-alert">{message}</div>,
}));

vi.mock('@/shared/components/ui/EmptyState', () => ({
  EmptyState: ({ title }: { title: string }) => <div data-testid="empty-state">{title}</div>,
}));

vi.mock('../StarRating', () => ({
  StarRating: ({ rating }: { rating: number }) => <div data-testid="star-rating">{rating}</div>,
}));

vi.mock('@/features/products/components', () => ({
  ReviewSkeleton: () => <div data-testid="review-skeleton">Loading...</div>,
}));

vi.mock('@/shared/components/Skeletons', () => ({
  Skeleton: ({
    height,
    width,
    className,
  }: {
    height?: number | string;
    width?: number | string;
    className?: string;
  }) => <span data-testid="skeleton" style={{ height, width }} className={className} />,
  SkeletonLabelRow: () => <div data-testid="skeleton-label-row" />,
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

const reviews = [
  {
    id: '1',
    title: 'Great',
    comment: 'Loved it',
    rating: 5,
    userName: 'Ivan',
    createdAt: '2026-03-01T00:00:00.000Z',
  },
];

describe('ReviewList', () => {
  it('shows loading state', () => {
    render(<ReviewList reviews={[]} isLoading />);
    expect(screen.getByTestId('review-skeleton')).toBeInTheDocument();
  });

  it('shows error state', () => {
    render(<ReviewList reviews={[]} error={new Error('fail')} />);
    expect(screen.getByTestId('error-alert')).toHaveTextContent('products.failedToLoadReviews');
  });

  it('shows empty state when no reviews', () => {
    render(<ReviewList reviews={[]} />);
    expect(screen.getByText('products.noReviewsYet')).toBeInTheDocument();
  });

  it('renders review cards with rating and metadata', () => {
    render(<ReviewList reviews={reviews} />);

    expect(screen.getByText('Great')).toBeInTheDocument();
    expect(screen.getByText('Loved it')).toBeInTheDocument();
    expect(screen.getByText('5/5')).toBeInTheDocument();
    expect(screen.getByText('products.by Ivan')).toBeInTheDocument();
    expect(screen.getByTestId('star-rating')).toHaveTextContent('5');
  });

  it('falls back to anonymous when userName missing', () => {
    render(<ReviewList reviews={[{ ...reviews[0], id: '2', userName: undefined }]} />);

    expect(screen.getByText('products.by products.anonymous')).toBeInTheDocument();
  });
});

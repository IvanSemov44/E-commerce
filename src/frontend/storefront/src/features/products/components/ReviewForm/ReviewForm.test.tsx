import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import ReviewForm from './ReviewForm';

const unwrapMock = vi.fn();
const createReviewMock = vi.fn(() => ({ unwrap: unwrapMock }));
const handleErrorMock = vi.fn();

vi.mock('@/features/products/api/reviewsApi', () => ({
  useCreateReviewMutation: () => [createReviewMock, { isLoading: false }],
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock('@/shared/hooks', () => ({
  useApiErrorHandler: () => ({
    handleError: handleErrorMock,
  }),
}));

vi.mock('../ui/Button', () => ({
  default: ({ children, ...props }: React.ButtonHTMLAttributes<HTMLButtonElement>) => (
    <button {...props}>{children}</button>
  ),
}));

vi.mock('../ui/Card', () => ({
  default: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}));

vi.mock('../StarRating', () => ({
  default: ({ onRatingChange }: { onRatingChange?: (value: number) => void }) => (
    <button type="button" onClick={() => onRatingChange?.(4)}>
      Set Rating
    </button>
  ),
}));

describe('ReviewForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders form fields and submit button', () => {
    render(<ReviewForm productId="p1" />);

    expect(screen.getByLabelText('products.reviewTitle')).toBeInTheDocument();
    expect(screen.getByLabelText('products.reviewComment')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'products.submitReview' })).toBeInTheDocument();
  });

  it('disables submit button when comment is empty', () => {
    render(<ReviewForm productId="p1" />);
    expect(screen.getByRole('button', { name: 'products.submitReview' })).toBeDisabled();
  });

  it('submits trimmed data and calls onSuccess', async () => {
    unwrapMock.mockResolvedValueOnce({});
    const onSuccess = vi.fn();
    render(<ReviewForm productId="p1" onSuccess={onSuccess} />);

    fireEvent.change(screen.getByLabelText('products.reviewTitle'), {
      target: { value: '  Nice product  ' },
    });
    fireEvent.change(screen.getByLabelText('products.reviewComment'), {
      target: { value: '  Great quality  ' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Set Rating' }));
    fireEvent.click(screen.getByRole('button', { name: 'products.submitReview' }));

    await waitFor(() => {
      expect(createReviewMock).toHaveBeenCalledWith({
        productId: 'p1',
        title: 'Nice product',
        comment: 'Great quality',
        rating: 4,
      });
    });

    expect(onSuccess).toHaveBeenCalled();
  });

  it('handles mutation errors through api error handler', async () => {
    const err = new Error('request failed');
    unwrapMock.mockRejectedValueOnce(err);

    render(<ReviewForm productId="p1" />);

    fireEvent.change(screen.getByLabelText('products.reviewComment'), {
      target: { value: 'Helpful review' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'products.submitReview' }));

    await waitFor(() => {
      expect(handleErrorMock).toHaveBeenCalledWith(err, 'common.errorOccurred');
    });
  });
});

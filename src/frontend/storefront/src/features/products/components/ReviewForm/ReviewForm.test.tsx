import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { ReviewForm } from './ReviewForm';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

const handleErrorMock = vi.fn();

// Return human-readable text so button queries like /submit review/i work
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const map: Record<string, string> = {
        'products.submitReview': 'Submit Review',
        'products.submitting': 'Submitting...',
        'products.writeReview': 'Write a Review',
        'products.reviewTitle': 'Review Title (Optional)',
        'products.reviewComment': 'Comment',
        'products.reviewTitlePlaceholder': 'Enter review title',
        'products.reviewCommentPlaceholder': 'Write your review...',
        'common.errorOccurred': 'An error occurred',
      };
      return map[key] ?? key;
    },
  }),
}));

vi.mock('@/shared/hooks', () => ({
  useApiErrorHandler: () => ({
    handleError: handleErrorMock,
  }),
}));

vi.mock('@/features/products/api', () => ({
  useCreateReviewMutation: () => [
    vi.fn().mockReturnValue({ unwrap: vi.fn().mockResolvedValue({ id: 'r1' }) }),
    { isLoading: false },
  ],
}));

// StarRating is in the same folder — relative path is correct
vi.mock('../StarRating', () => ({
  StarRating: ({ onRatingChange }: { onRatingChange?: (value: number) => void }) => (
    <button type="button" onClick={() => onRatingChange?.(4)}>
      Set Rating
    </button>
  ),
}));

const setupHandlers = () => {
  server.use(
    http.post('/api/products/p1/reviews', async () => {
      return HttpResponse.json({
        success: true,
        data: {
          id: 'r1',
          rating: 4,
          comment: 'Great!',
          userName: 'Test',
          createdAt: new Date().toISOString(),
        },
      });
    })
  );
};

describe('ReviewForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    setupHandlers();
    handleErrorMock.mockReset();
  });

  it('renders form fields and submit button', () => {
    render(<ReviewForm productId="p1" />);

    expect(screen.getByRole('textbox', { name: /comment/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /submit review/i })).toBeInTheDocument();
  });

  it('submit button is disabled when comment is empty', () => {
    render(<ReviewForm productId="p1" />);

    // Button is disabled until comment is filled in
    expect(screen.getByRole('button', { name: /submit review/i })).toBeDisabled();
  });

  it('submits review successfully', async () => {
    render(<ReviewForm productId="p1" />);

    fireEvent.click(screen.getByRole('button', { name: 'Set Rating' }));
    fireEvent.change(screen.getByRole('textbox', { name: /comment/i }), {
      target: { value: 'Great product!' },
    });

    const submitBtn = screen.getByRole('button', { name: /submit review/i });
    expect(submitBtn).not.toBeDisabled();

    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(handleErrorMock).not.toHaveBeenCalled();
    });
  });
});

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ReviewForm from '../ReviewForm';

// Mock the mutation hook
const mockUseCreateReviewMutation = vi.fn();
const mockCreateReview = vi.fn();
vi.mock('../../store/api/reviewsApi', () => ({
  useCreateReviewMutation: () => mockUseCreateReviewMutation(),
}));

describe('ReviewForm', () => {
  const productId = 'product-123';
  const onSuccess = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    mockCreateReview.mockReturnValue({ unwrap: () => Promise.resolve() });
    mockUseCreateReviewMutation.mockReturnValue([mockCreateReview, { isLoading: false, error: null }]);
  });

  it('renders form elements', () => {
    render(<ReviewForm productId={productId} />);

    expect(screen.getByText('Write a Review')).toBeInTheDocument();
    // Rating is a custom StarRating component, not a form control with label
    expect(screen.getByText('Rating')).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: /stars/i })).toHaveLength(5);
    expect(screen.getByLabelText(/title/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/comment/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /submit review/i })).toBeInTheDocument();
  });

  it('disables submit button when comment is empty', () => {
    render(<ReviewForm productId={productId} />);
    
    const submitBtn = screen.getByRole('button', { name: /submit review/i });
    expect(submitBtn).toBeDisabled();
  });

  it('enables submit button when comment is entered', async () => {
    const user = userEvent.setup();
    render(<ReviewForm productId={productId} />);

    const commentInput = screen.getByLabelText(/comment/i);
    await user.type(commentInput, 'Great product!');

    const submitBtn = screen.getByRole('button', { name: /submit review/i });
    expect(submitBtn).toBeEnabled();
  });

  it('submits the form with correct data', async () => {
    const user = userEvent.setup();
    render(<ReviewForm productId={productId} onSuccess={onSuccess} />);

    // Set rating (click 4th star)
    const stars = screen.getAllByRole('button', { name: /stars/i });
    await user.click(stars[3]); // 4 stars

    // Fill text fields
    await user.type(screen.getByLabelText(/title/i), 'My Review');
    await user.type(screen.getByLabelText(/comment/i), 'This is a comment');

    // Submit
    await user.click(screen.getByRole('button', { name: /submit review/i }));

    expect(mockCreateReview).toHaveBeenCalledWith({
      productId,
      rating: 4,
      title: 'My Review',
      comment: 'This is a comment',
    });

    await waitFor(() => {
      expect(onSuccess).toHaveBeenCalled();
    });
  });

  it('displays error message on submission failure', async () => {
    // Simulate error state returned by the hook
    mockUseCreateReviewMutation.mockReturnValue([
      mockCreateReview,
      { isLoading: false, error: { status: 500, data: { message: 'Error' } } }
    ]);
    
    render(<ReviewForm productId={productId} />);
    expect(screen.getByText(/Failed to submit review/i)).toBeInTheDocument();
  });
});
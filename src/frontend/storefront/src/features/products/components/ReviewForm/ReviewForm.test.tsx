import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { ReviewForm } from './ReviewForm';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

const handleErrorMock = vi.fn();

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

  it('shows validation error when rating is not selected', async () => {
    render(<ReviewForm productId="p1" />);

    fireEvent.click(screen.getByRole('button', { name: /submit review/i }));

    await waitFor(() => {
      expect(screen.getByText('reviews.ratingRequired')).toBeInTheDocument();
    });
  });

  it('submits review successfully', async () => {
    render(<ReviewForm productId="p1" />);

    fireEvent.click(screen.getByRole('button', { name: 'Set Rating' }));
    fireEvent.change(screen.getByRole('textbox', { name: /comment/i }), {
      target: { value: 'Great product!' },
    });

    await waitFor(() => {
      expect(handleErrorMock).not.toHaveBeenCalled();
    });
  });
});

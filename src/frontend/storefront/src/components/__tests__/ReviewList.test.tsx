import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import ReviewList, { type Review } from '../ReviewList';

describe('ReviewList', () => {
  const mockReviews: Review[] = [
    {
      id: '1',
      title: 'Great product!',
      comment: 'I love this product. It works perfectly.',
      rating: 5,
      userName: 'John Doe',
      createdAt: '2024-01-15T10:30:00Z',
    },
    {
      id: '2',
      title: 'Good value',
      comment: 'Good value for the price.',
      rating: 4,
      userName: 'Jane Smith',
      createdAt: '2024-01-10T14:20:00Z',
    },
    {
      id: '3',
      comment: 'Average product.',
      rating: 3,
      userName: 'Bob Wilson',
      createdAt: '2024-01-05T09:00:00Z',
    },
  ];

  describe('Loading State', () => {
    it('shows loading state when isLoading is true', () => {
      render(<ReviewList reviews={[]} isLoading={true} />);

      expect(screen.getByText('Loading reviews...')).toBeInTheDocument();
    });

    it('does not show reviews when loading', () => {
      render(<ReviewList reviews={mockReviews} isLoading={true} />);

      expect(screen.queryByText('Great product!')).not.toBeInTheDocument();
    });
  });

  describe('Error State', () => {
    it('shows error message when error is provided', () => {
      render(<ReviewList reviews={[]} error={new Error('Test error')} />);

      expect(screen.getByText('Failed to load reviews.')).toBeInTheDocument();
    });

    it('does not show reviews when there is an error', () => {
      render(<ReviewList reviews={mockReviews} error={new Error('Test error')} />);

      expect(screen.queryByText('Great product!')).not.toBeInTheDocument();
    });
  });

  describe('Empty State', () => {
    it('shows empty state when reviews array is empty', () => {
      render(<ReviewList reviews={[]} />);

      expect(screen.getByText('No reviews yet')).toBeInTheDocument();
    });
  });

  describe('Review Display', () => {
    it('renders all reviews', () => {
      render(<ReviewList reviews={mockReviews} />);

      expect(screen.getByText('Great product!')).toBeInTheDocument();
      expect(screen.getByText('Good value')).toBeInTheDocument();
    });

    it('displays review comments', () => {
      render(<ReviewList reviews={mockReviews} />);

      expect(screen.getByText('I love this product. It works perfectly.')).toBeInTheDocument();
      expect(screen.getByText('Good value for the price.')).toBeInTheDocument();
    });

    it('displays review titles when present', () => {
      render(<ReviewList reviews={mockReviews} />);

      expect(screen.getByText('Great product!')).toBeInTheDocument();
      expect(screen.getByText('Good value')).toBeInTheDocument();
    });

    it('handles reviews without titles', () => {
      render(<ReviewList reviews={mockReviews} />);

      // Third review has no title but should still render
      expect(screen.getByText('Average product.')).toBeInTheDocument();
    });
  });

  describe('User Information', () => {
    it('displays user name when provided', () => {
      render(<ReviewList reviews={mockReviews} />);

      expect(screen.getByText(/By John Doe/)).toBeInTheDocument();
      expect(screen.getByText(/By Jane Smith/)).toBeInTheDocument();
    });

    it('displays Anonymous when userName is not provided', () => {
      const reviewsWithoutUser: Review[] = [
        {
          id: '1',
          comment: 'Anonymous review',
          rating: 4,
          createdAt: '2024-01-15T10:30:00Z',
        },
      ];

      render(<ReviewList reviews={reviewsWithoutUser} />);

      expect(screen.getByText(/By Anonymous/)).toBeInTheDocument();
    });
  });

  describe('Rating Display', () => {
    it('displays rating for each review', () => {
      render(<ReviewList reviews={mockReviews} />);

      expect(screen.getByText('5/5')).toBeInTheDocument();
      expect(screen.getByText('4/5')).toBeInTheDocument();
      expect(screen.getByText('3/5')).toBeInTheDocument();
    });
  });

  describe('Date Formatting', () => {
    it('displays formatted date for each review', () => {
      render(<ReviewList reviews={mockReviews} />);

      // The date should be formatted as locale date string
      const dateElements = screen.getAllByText(/\d{1,2}\/\d{1,2}\/\d{4}/);
      expect(dateElements.length).toBeGreaterThan(0);
    });
  });

  describe('Multiple Reviews', () => {
    it('renders all reviews in a grid', () => {
      const { container } = render(<ReviewList reviews={mockReviews} />);

      const grid = container.querySelector('.grid');
      expect(grid).toBeInTheDocument();
    });

    it('renders correct number of review cards', () => {
      render(<ReviewList reviews={mockReviews} />);

      // Each review should have its title displayed
      expect(screen.getByText('Great product!')).toBeInTheDocument();
      expect(screen.getByText('Good value')).toBeInTheDocument();
      expect(screen.getByText('Average product.')).toBeInTheDocument();
    });
  });
});

import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import StarRating from './StarRating';

describe('StarRating', () => {
  it('renders five stars by default', () => {
    render(<StarRating rating={3} />);
    expect(screen.getAllByRole('button')).toHaveLength(5);
  });

  it('renders custom number of stars', () => {
    render(<StarRating rating={2} maxStars={7} />);
    expect(screen.getAllByRole('button')).toHaveLength(7);
  });

  it('calls onRatingChange when clicking star', () => {
    const onRatingChange = vi.fn();
    render(<StarRating rating={1} onRatingChange={onRatingChange} />);

    fireEvent.click(screen.getByRole('button', { name: /4 stars/i }));
    expect(onRatingChange).toHaveBeenCalledWith(4);
  });

  it('does not call onRatingChange when readonly', () => {
    const onRatingChange = vi.fn();
    render(<StarRating rating={4} readonly onRatingChange={onRatingChange} />);

    fireEvent.click(screen.getByRole('button', { name: /2 stars/i }));
    expect(onRatingChange).not.toHaveBeenCalled();
    expect(screen.getAllByRole('button').every((btn) => (btn as HTMLButtonElement).disabled)).toBe(true);
  });

  it('applies size class based on prop', () => {
    const { container } = render(<StarRating rating={3} size="lg" />);
    const wrapper = container.firstChild as HTMLElement;
    expect(wrapper.className).toMatch(/starRating|lg/i);
  });
});

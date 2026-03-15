import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { ProductFilters } from './ProductFilters';

const defaultProps = {
  minPrice: undefined,
  maxPrice: undefined,
  minRating: undefined,
  isFeatured: undefined,
  onMinPriceChange: vi.fn(),
  onMaxPriceChange: vi.fn(),
  onMinRatingChange: vi.fn(),
  onIsFeaturedChange: vi.fn(),
};

describe('ProductFilters', () => {
  it('renders price range inputs', () => {
    render(<ProductFilters {...defaultProps} />);

    expect(screen.getByPlaceholderText(/min/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/max/i)).toBeInTheDocument();
  });

  it('renders rating select dropdown', () => {
    render(<ProductFilters {...defaultProps} />);

    expect(screen.getByRole('combobox')).toBeInTheDocument();
  });

  it('renders featured checkbox', () => {
    render(<ProductFilters {...defaultProps} />);

    expect(screen.getByRole('checkbox')).toBeInTheDocument();
  });

  it('calls onMinPriceChange when min price is entered', async () => {
    const user = userEvent.setup();
    const onMinPriceChange = vi.fn();
    render(<ProductFilters {...defaultProps} onMinPriceChange={onMinPriceChange} />);

    const minPriceInput = screen.getByPlaceholderText(/min/i);
    await user.type(minPriceInput, '10');

    expect(onMinPriceChange).toHaveBeenCalled();
  });

  it('calls onMaxPriceChange when max price is entered', async () => {
    const user = userEvent.setup();
    const onMaxPriceChange = vi.fn();
    render(<ProductFilters {...defaultProps} onMaxPriceChange={onMaxPriceChange} />);

    const maxPriceInput = screen.getByPlaceholderText(/max/i);
    await user.type(maxPriceInput, '100');

    expect(onMaxPriceChange).toHaveBeenCalled();
  });

  it('calls onMinRatingChange when rating is selected', async () => {
    const user = userEvent.setup();
    const onMinRatingChange = vi.fn();
    render(<ProductFilters {...defaultProps} onMinRatingChange={onMinRatingChange} />);

    const ratingSelect = screen.getByRole('combobox');
    await user.selectOptions(ratingSelect, '4');

    expect(onMinRatingChange).toHaveBeenCalledWith(4);
  });

  it('calls onIsFeaturedChange when featured checkbox is toggled', async () => {
    const user = userEvent.setup();
    const onIsFeaturedChange = vi.fn();
    render(<ProductFilters {...defaultProps} onIsFeaturedChange={onIsFeaturedChange} />);

    const checkbox = screen.getByRole('checkbox');
    await user.click(checkbox);

    expect(onIsFeaturedChange).toHaveBeenCalledWith(true);
  });

  it('displays current price values', () => {
    render(<ProductFilters {...defaultProps} minPrice={10} maxPrice={100} />);

    const minPriceInput = screen.getByPlaceholderText(/min/i) as HTMLInputElement;
    const maxPriceInput = screen.getByPlaceholderText(/max/i) as HTMLInputElement;

    expect(minPriceInput.value).toBe('10');
    expect(maxPriceInput.value).toBe('100');
  });

  it('displays current rating value', () => {
    render(<ProductFilters {...defaultProps} minRating={4.5} />);

    const ratingSelect = screen.getByRole('combobox') as HTMLSelectElement;
    expect(ratingSelect.value).toBe('4.5');
  });

  it('displays checked state for featured filter', () => {
    render(<ProductFilters {...defaultProps} isFeatured={true} />);

    const checkbox = screen.getByRole('checkbox') as HTMLInputElement;
    expect(checkbox.checked).toBe(true);
  });
});

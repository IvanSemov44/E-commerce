import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import ProductFilters from '../../pages/components/Products/ProductFilters';

describe('ProductFilters', () => {
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

  it('renders Price Range section', () => {
    render(<ProductFilters {...defaultProps} />);
    expect(screen.getByText('Price Range')).toBeInTheDocument();
  });

  it('renders Minimum Rating section', () => {
    render(<ProductFilters {...defaultProps} />);
    expect(screen.getByText('Minimum Rating')).toBeInTheDocument();
  });

  it('renders Featured Products checkbox', () => {
    render(<ProductFilters {...defaultProps} />);
    expect(screen.getByText('Featured Products Only')).toBeInTheDocument();
  });

  it('renders min price input', () => {
    render(<ProductFilters {...defaultProps} />);
    expect(screen.getByPlaceholderText('Min')).toBeInTheDocument();
  });

  it('renders max price input', () => {
    render(<ProductFilters {...defaultProps} />);
    expect(screen.getByPlaceholderText('Max')).toBeInTheDocument();
  });

  it('calls onMinPriceChange when min price changes', () => {
    const onMinPriceChange = vi.fn();
    render(<ProductFilters {...defaultProps} onMinPriceChange={onMinPriceChange} />);
    
    const minInput = screen.getByPlaceholderText('Min');
    fireEvent.change(minInput, { target: { value: '10' } });
    
    expect(onMinPriceChange).toHaveBeenCalledWith(10);
  });

  it('calls onMaxPriceChange when max price changes', () => {
    const onMaxPriceChange = vi.fn();
    render(<ProductFilters {...defaultProps} onMaxPriceChange={onMaxPriceChange} />);
    
    const maxInput = screen.getByPlaceholderText('Max');
    fireEvent.change(maxInput, { target: { value: '100' } });
    
    expect(onMaxPriceChange).toHaveBeenCalledWith(100);
  });

  it('clears min price when input is empty', () => {
    const onMinPriceChange = vi.fn();
    render(<ProductFilters {...defaultProps} minPrice={50} onMinPriceChange={onMinPriceChange} />);
    
    const minInput = screen.getByPlaceholderText('Min');
    fireEvent.change(minInput, { target: { value: '' } });
    
    expect(onMinPriceChange).toHaveBeenCalledWith(undefined);
  });

  it('renders rating select with all options', () => {
    render(<ProductFilters {...defaultProps} />);
    expect(screen.getByText('All Ratings')).toBeInTheDocument();
    expect(screen.getByText('4+ Stars')).toBeInTheDocument();
    expect(screen.getByText('4.5+ Stars')).toBeInTheDocument();
    expect(screen.getByText('5 Stars')).toBeInTheDocument();
  });

  it('calls onMinRatingChange when rating selection changes', () => {
    const onMinRatingChange = vi.fn();
    render(<ProductFilters {...defaultProps} onMinRatingChange={onMinRatingChange} />);
    
    const select = screen.getByRole('combobox');
    fireEvent.change(select, { target: { value: '4' } });
    
    expect(onMinRatingChange).toHaveBeenCalledWith(4);
  });

  it('checkbox is unchecked by default', () => {
    render(<ProductFilters {...defaultProps} />);
    const checkbox = screen.getByRole('checkbox') as HTMLInputElement;
    expect(checkbox.checked).toBe(false);
  });

  it('checkbox is checked when isFeatured is true', () => {
    render(<ProductFilters {...defaultProps} isFeatured={true} />);
    const checkbox = screen.getByRole('checkbox') as HTMLInputElement;
    expect(checkbox.checked).toBe(true);
  });

  it('calls onIsFeaturedChange when checkbox is clicked', () => {
    const onIsFeaturedChange = vi.fn();
    render(<ProductFilters {...defaultProps} onIsFeaturedChange={onIsFeaturedChange} />);
    
    const checkbox = screen.getByRole('checkbox');
    fireEvent.click(checkbox);
    
    expect(onIsFeaturedChange).toHaveBeenCalledWith(true);
  });
});

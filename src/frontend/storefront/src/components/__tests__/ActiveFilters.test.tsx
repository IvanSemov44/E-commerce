import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import ActiveFilters from '../../pages/components/Products/ActiveFilters';

describe('ActiveFilters', () => {
  const defaultProps = {
    search: '',
    categorySelected: false,
    minPrice: undefined,
    maxPrice: undefined,
    minRating: undefined,
    isFeatured: undefined,
    onClearAll: vi.fn(),
  };

  it('renders nothing when no filters are active', () => {
    const { container } = render(<ActiveFilters {...defaultProps} />);
    expect(container.firstChild).toBeNull();
  });

  it('renders search filter badge when search is provided', () => {
    render(<ActiveFilters {...defaultProps} search="laptop" />);
    expect(screen.getByText(/Search:/)).toBeInTheDocument();
    expect(screen.getByText('laptop')).toBeInTheDocument();
  });

  it('renders category filter badge when category is selected', () => {
    render(<ActiveFilters {...defaultProps} categorySelected={true} />);
    expect(screen.getByText('Category Selected')).toBeInTheDocument();
  });

  it('renders price filter badge when minPrice is provided', () => {
    render(<ActiveFilters {...defaultProps} minPrice={10} />);
    expect(screen.getByText(/Price:/)).toBeInTheDocument();
  });

  it('renders price filter badge when maxPrice is provided', () => {
    render(<ActiveFilters {...defaultProps} maxPrice={100} />);
    expect(screen.getByText(/Price:/)).toBeInTheDocument();
  });

  it('renders price filter with correct values', () => {
    render(<ActiveFilters {...defaultProps} minPrice={10} maxPrice={100} />);
    expect(screen.getByText('Price: $10 - $100')).toBeInTheDocument();
  });

  it('renders rating filter badge when minRating is provided', () => {
    render(<ActiveFilters {...defaultProps} minRating={4} />);
    expect(screen.getByText('4+ Stars')).toBeInTheDocument();
  });

  it('renders featured filter badge when isFeatured is true', () => {
    render(<ActiveFilters {...defaultProps} isFeatured={true} />);
    expect(screen.getByText('Featured Only')).toBeInTheDocument();
  });

  it('renders Clear Filters button', () => {
    render(<ActiveFilters {...defaultProps} search="test" />);
    expect(screen.getByText('Clear Filters')).toBeInTheDocument();
  });

  it('calls onClearAll when Clear Filters button is clicked', () => {
    const onClearAll = vi.fn();
    render(<ActiveFilters {...defaultProps} search="test" onClearAll={onClearAll} />);
    
    fireEvent.click(screen.getByText('Clear Filters'));
    expect(onClearAll).toHaveBeenCalledTimes(1);
  });

  it('renders multiple filter badges', () => {
    render(
      <ActiveFilters
        {...defaultProps}
        search="laptop"
        categorySelected={true}
        minPrice={10}
        maxPrice={100}
      />
    );
    
    expect(screen.getByText('laptop')).toBeInTheDocument();
    expect(screen.getByText('Category Selected')).toBeInTheDocument();
    expect(screen.getByText('Price: $10 - $100')).toBeInTheDocument();
  });

  it('handles infinite price display', () => {
    render(<ActiveFilters {...defaultProps} minPrice={10} maxPrice={undefined} />);
    expect(screen.getByText('Price: $10 - ∞')).toBeInTheDocument();
  });
});

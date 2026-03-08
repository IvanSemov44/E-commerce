import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import ActiveFilters from './ActiveFilters';
import type { ActiveFiltersProps } from './ActiveFilters.types';

const defaultProps: ActiveFiltersProps = {
  search: '',
  categorySelected: false,
  minPrice: undefined,
  maxPrice: undefined,
  minRating: undefined,
  isFeatured: undefined,
  onClearAll: vi.fn(),
};

describe('ActiveFilters', () => {
  it('renders nothing when no filters are active', () => {
    const { container } = render(<ActiveFilters {...defaultProps} />);

    expect(container.firstChild).toBeNull();
  });

  it('shows search filter badge', () => {
    render(<ActiveFilters {...defaultProps} search="laptop" />);

    expect(screen.getByText(/laptop/)).toBeInTheDocument();
  });

  it('shows category filter badge', () => {
    render(<ActiveFilters {...defaultProps} categorySelected={true} />);

    expect(screen.getByText(/category/i)).toBeInTheDocument();
  });

  it('shows price range filter badge', () => {
    render(<ActiveFilters {...defaultProps} minPrice={10} maxPrice={100} />);

    expect(screen.getByText(/\$10 - \$100/)).toBeInTheDocument();
  });

  it('shows only min price when max is undefined', () => {
    render(<ActiveFilters {...defaultProps} minPrice={50} maxPrice={undefined} />);

    expect(screen.getByText(/\$50 - \$∞/)).toBeInTheDocument();
  });

  it('shows rating filter badge', () => {
    render(<ActiveFilters {...defaultProps} minRating={4.5} />);

    expect(screen.getByText(/4\.5/)).toBeInTheDocument();
  });

  it('shows featured filter badge', () => {
    render(<ActiveFilters {...defaultProps} isFeatured={true} />);

    expect(screen.getByText(/featured/i)).toBeInTheDocument();
  });

  it('shows all active filters together', () => {
    render(
      <ActiveFilters
        {...defaultProps}
        search="laptop"
        categorySelected={true}
        minPrice={100}
        minRating={4}
        isFeatured={true}
      />
    );

    expect(screen.getByText(/laptop/)).toBeInTheDocument();
    expect(screen.getByText(/category/i)).toBeInTheDocument();
    expect(screen.getByText(/100/)).toBeInTheDocument();
    expect(screen.getByText(/4/)).toBeInTheDocument();
    expect(screen.getByText(/featured/i)).toBeInTheDocument();
  });

  it('renders clear button', () => {
    render(<ActiveFilters {...defaultProps} search="test" />);

    expect(screen.getByRole('button', { name: /clear/i })).toBeInTheDocument();
  });

  it('calls onClearAll when clear button is clicked', async () => {
    const user = userEvent.setup();
    const onClearAll = vi.fn();
    render(<ActiveFilters {...defaultProps} search="laptop" onClearAll={onClearAll} />);

    const clearButton = screen.getByRole('button', { name: /clear/i });
    await user.click(clearButton);

    expect(onClearAll).toHaveBeenCalled();
  });

  it('does not render when featured is false', () => {
    const { container } = render(<ActiveFilters {...defaultProps} isFeatured={false} />);

    expect(container.firstChild).toBeNull();
  });

  it('does not render clear button when no filters', () => {
    render(<ActiveFilters {...defaultProps} />);

    expect(screen.queryByRole('button', { name: /clear/i })).not.toBeInTheDocument();
  });
});

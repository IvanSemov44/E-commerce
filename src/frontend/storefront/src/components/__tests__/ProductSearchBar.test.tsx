import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import ProductSearchBar from '../../pages/components/Products/ProductSearchBar';

describe('ProductSearchBar', () => {
  const defaultProps = {
    searchValue: '',
    sortBy: 'newest',
    onSearchChange: vi.fn(),
    onSortChange: vi.fn(),
  };

  it('renders search input with placeholder', () => {
    render(<ProductSearchBar {...defaultProps} />);
    expect(screen.getByPlaceholderText('Search products...')).toBeInTheDocument();
  });

  it('renders search input with provided value', () => {
    render(<ProductSearchBar {...defaultProps} searchValue="test query" />);
    const input = screen.getByPlaceholderText('Search products...') as HTMLInputElement;
    expect(input.value).toBe('test query');
  });

  it('calls onSearchChange when input changes', () => {
    const onSearchChange = vi.fn();
    render(<ProductSearchBar {...defaultProps} onSearchChange={onSearchChange} />);
    
    const input = screen.getByPlaceholderText('Search products...');
    fireEvent.change(input, { target: { value: 'laptop' } });
    
    expect(onSearchChange).toHaveBeenCalledWith('laptop');
  });

  it('renders sort select', () => {
    render(<ProductSearchBar {...defaultProps} />);
    expect(screen.getByRole('combobox')).toBeInTheDocument();
  });

  it('renders all sort options', () => {
    render(<ProductSearchBar {...defaultProps} />);
    expect(screen.getByText('Newest First')).toBeInTheDocument();
    expect(screen.getByText('Name (A-Z)')).toBeInTheDocument();
    expect(screen.getByText('Price: Low to High')).toBeInTheDocument();
    expect(screen.getByText('Price: High to Low')).toBeInTheDocument();
    expect(screen.getByText('Highest Rated')).toBeInTheDocument();
  });

  it('has correct default sort value', () => {
    render(<ProductSearchBar {...defaultProps} sortBy="newest" />);
    const select = screen.getByRole('combobox') as HTMLSelectElement;
    expect(select.value).toBe('newest');
  });

  it('calls onSortChange when sort selection changes', () => {
    const onSortChange = vi.fn();
    render(<ProductSearchBar {...defaultProps} onSortChange={onSortChange} />);
    
    const select = screen.getByRole('combobox');
    fireEvent.change(select, { target: { value: 'price-asc' } });
    
    expect(onSortChange).toHaveBeenCalledWith('price-asc');
  });

  it('updates displayed sort value correctly', () => {
    render(<ProductSearchBar {...defaultProps} sortBy="price-desc" />);
    const select = screen.getByRole('combobox') as HTMLSelectElement;
    expect(select.value).toBe('price-desc');
  });
});

import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import CategoryFilter from '../CategoryFilter';

// Mock the API hook
const mockCategories = [
  { id: '1', name: 'Electronics', slug: 'electronics' },
  { id: '2', name: 'Clothing', slug: 'clothing' },
];

vi.mock('../../store/api/categoriesApi', () => ({
  useGetTopLevelCategoriesQuery: () => ({
    data: mockCategories,
    isLoading: false,
    error: null,
  }),
}));

describe('CategoryFilter', () => {
  const onSelectCategory = vi.fn();

  it('renders category list', () => {
    render(<CategoryFilter onSelectCategory={onSelectCategory} />);

    expect(screen.getByText('Categories')).toBeInTheDocument();
    expect(screen.getByText('All Products')).toBeInTheDocument();
    expect(screen.getByText('Electronics')).toBeInTheDocument();
    expect(screen.getByText('Clothing')).toBeInTheDocument();
  });

  it('highlights "All Products" when no category is selected', () => {
    render(<CategoryFilter onSelectCategory={onSelectCategory} />);

    const allBtn = screen.getByRole('button', { name: 'All Products' });
    expect(allBtn.className).toMatch(/active/);
  });

  it('highlights selected category', () => {
    render(
      <CategoryFilter
        selectedCategoryId="1"
        onSelectCategory={onSelectCategory}
      />
    );

    const electronicsBtn = screen.getByRole('button', { name: 'Electronics' });
    expect(electronicsBtn.className).toMatch(/active/);
    
    const allBtn = screen.getByRole('button', { name: 'All Products' });
    expect(allBtn.className).not.toMatch(/active/);
  });

  it('calls onSelectCategory with id when category clicked', () => {
    render(<CategoryFilter onSelectCategory={onSelectCategory} />);

    fireEvent.click(screen.getByText('Electronics'));
    expect(onSelectCategory).toHaveBeenCalledWith('1');
  });

  it('calls onSelectCategory with undefined when "All Products" clicked', () => {
    render(
      <CategoryFilter selectedCategoryId="1" onSelectCategory={onSelectCategory} />
    );

    fireEvent.click(screen.getByText('All Products'));
    expect(onSelectCategory).toHaveBeenCalledWith(undefined);
  });
});
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { CategoryFilter } from './CategoryFilter';

const getTopLevelCategoriesQueryMock = vi.fn();

vi.mock('@/features/products/api/categoriesApi', () => ({
  useGetTopLevelCategoriesQuery: () => getTopLevelCategoriesQueryMock(),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

describe('CategoryFilter', () => {
  it('shows loading state', () => {
    getTopLevelCategoriesQueryMock.mockReturnValue({ data: [], isLoading: true, error: null });
    render(<CategoryFilter onSelectCategory={vi.fn()} />);

    expect(screen.getByText('products.loadingCategories')).toBeInTheDocument();
  });

  it('shows error state', () => {
    getTopLevelCategoriesQueryMock.mockReturnValue({
      data: [],
      isLoading: false,
      error: new Error('fail'),
    });
    render(<CategoryFilter onSelectCategory={vi.fn()} />);

    expect(screen.getByText('products.failedToLoadCategories')).toBeInTheDocument();
  });

  it('renders categories and calls onSelectCategory', () => {
    const onSelectCategory = vi.fn();
    getTopLevelCategoriesQueryMock.mockReturnValue({
      data: [
        { id: 'c1', name: 'Phones' },
        { id: 'c2', name: 'Laptops' },
      ],
      isLoading: false,
      error: null,
    });

    render(<CategoryFilter selectedCategoryId="c1" onSelectCategory={onSelectCategory} />);

    fireEvent.click(screen.getByRole('button', { name: 'Laptops' }));
    expect(onSelectCategory).toHaveBeenCalledWith('c2');
  });

  it('supports selecting all products', () => {
    const onSelectCategory = vi.fn();
    getTopLevelCategoriesQueryMock.mockReturnValue({ data: [], isLoading: false, error: null });

    render(<CategoryFilter selectedCategoryId="c1" onSelectCategory={onSelectCategory} />);

    fireEvent.click(screen.getByRole('button', { name: 'products.allProducts' }));
    expect(onSelectCategory).toHaveBeenCalledWith(undefined);
  });

  it('marks "All Products" button as active when no category is selected', () => {
    getTopLevelCategoriesQueryMock.mockReturnValue({ data: [], isLoading: false, error: null });

    render(<CategoryFilter onSelectCategory={vi.fn()} />);

    expect(screen.getByRole('button', { name: 'products.allProducts' }).className).toContain(
      'active'
    );
  });

  it('marks the matching category button as active', () => {
    getTopLevelCategoriesQueryMock.mockReturnValue({
      data: [
        { id: 'c1', name: 'Phones' },
        { id: 'c2', name: 'Laptops' },
      ],
      isLoading: false,
      error: null,
    });

    render(<CategoryFilter selectedCategoryId="c2" onSelectCategory={vi.fn()} />);

    expect(screen.getByRole('button', { name: 'Laptops' }).className).toContain('active');
    expect(screen.getByRole('button', { name: 'Phones' }).className).not.toContain('active');
  });
});

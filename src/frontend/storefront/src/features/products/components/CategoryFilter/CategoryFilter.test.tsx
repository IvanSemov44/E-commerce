import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router';
import { CategoryFilter } from './CategoryFilter';

const getTopLevelCategoriesQueryMock = vi.fn();

vi.mock('@/features/products/api/categoriesApi', () => ({
  useGetTopLevelCategoriesQuery: () => getTopLevelCategoriesQueryMock(),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

function renderCategoryFilter(url = '/') {
  return render(
    <MemoryRouter initialEntries={[url]}>
      <Routes>
        <Route path="*" element={<CategoryFilter />} />
      </Routes>
    </MemoryRouter>
  );
}

describe('CategoryFilter', () => {
  it('shows loading state', () => {
    getTopLevelCategoriesQueryMock.mockReturnValue({ data: [], isLoading: true, error: null });
    renderCategoryFilter();

    expect(screen.getByText('products.loadingCategories')).toBeInTheDocument();
  });

  it('shows error state', () => {
    getTopLevelCategoriesQueryMock.mockReturnValue({
      data: [],
      isLoading: false,
      error: new Error('fail'),
    });
    renderCategoryFilter();

    expect(screen.getByText('products.failedToLoadCategories')).toBeInTheDocument();
  });

  it('renders category buttons from API data', () => {
    getTopLevelCategoriesQueryMock.mockReturnValue({
      data: [
        { id: 'c1', name: 'Phones' },
        { id: 'c2', name: 'Laptops' },
      ],
      isLoading: false,
      error: null,
    });
    renderCategoryFilter();

    expect(screen.getByRole('button', { name: 'Phones' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Laptops' })).toBeInTheDocument();
  });

  it('renders "All Products" button', () => {
    getTopLevelCategoriesQueryMock.mockReturnValue({ data: [], isLoading: false, error: null });
    renderCategoryFilter();

    expect(screen.getByRole('button', { name: 'products.allProducts' })).toBeInTheDocument();
  });

  it('marks "All Products" as active when no categoryId in URL', () => {
    getTopLevelCategoriesQueryMock.mockReturnValue({ data: [], isLoading: false, error: null });
    renderCategoryFilter('/');

    expect(screen.getByRole('button', { name: 'products.allProducts' }).className).toContain(
      'active'
    );
  });

  it('marks the matching category button as active when categoryId is in URL', () => {
    getTopLevelCategoriesQueryMock.mockReturnValue({
      data: [
        { id: 'c1', name: 'Phones' },
        { id: 'c2', name: 'Laptops' },
      ],
      isLoading: false,
      error: null,
    });
    renderCategoryFilter('/?categoryId=c2');

    expect(screen.getByRole('button', { name: 'Laptops' }).className).toContain('active');
    expect(screen.getByRole('button', { name: 'Phones' }).className).not.toContain('active');
    expect(screen.getByRole('button', { name: 'products.allProducts' }).className).not.toContain(
      'active'
    );
  });

  it('clicking a category makes it active', () => {
    getTopLevelCategoriesQueryMock.mockReturnValue({
      data: [
        { id: 'c1', name: 'Phones' },
        { id: 'c2', name: 'Laptops' },
      ],
      isLoading: false,
      error: null,
    });
    renderCategoryFilter('/');

    fireEvent.click(screen.getByRole('button', { name: 'Laptops' }));

    expect(screen.getByRole('button', { name: 'Laptops' }).className).toContain('active');
  });

  it('clicking "All Products" clears active category', () => {
    getTopLevelCategoriesQueryMock.mockReturnValue({
      data: [{ id: 'c1', name: 'Phones' }],
      isLoading: false,
      error: null,
    });
    renderCategoryFilter('/?categoryId=c1');

    fireEvent.click(screen.getByRole('button', { name: 'products.allProducts' }));

    expect(screen.getByRole('button', { name: 'products.allProducts' }).className).toContain(
      'active'
    );
    expect(screen.getByRole('button', { name: 'Phones' }).className).not.toContain('active');
  });
});

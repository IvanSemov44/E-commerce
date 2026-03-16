import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { MemoryRouter, Routes, Route } from 'react-router';
import { ProductsToolbar } from './ProductsToolbar';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

vi.mock('@/app/SearchBar', () => ({
  SearchBar: ({ onQueryChange }: { onQueryChange: (v: string) => void }) => (
    <input data-testid="search-bar" onChange={(e) => onQueryChange(e.target.value)} />
  ),
}));

vi.mock('@/features/products/components/ActiveFilters', () => ({
  ActiveFilters: () => <div data-testid="active-filters" />,
}));

function renderToolbar(isRefetching = false, url = '/') {
  return render(
    <MemoryRouter initialEntries={[url]}>
      <Routes>
        <Route path="*" element={<ProductsToolbar isRefetching={isRefetching} />} />
      </Routes>
    </MemoryRouter>
  );
}

describe('ProductsToolbar', () => {
  it('renders the search bar', () => {
    renderToolbar();

    expect(screen.getByTestId('search-bar')).toBeInTheDocument();
  });

  it('renders the sort select', () => {
    renderToolbar();

    expect(screen.getByRole('combobox')).toBeInTheDocument();
  });

  it('renders sort options', () => {
    renderToolbar();

    expect(screen.getByRole('option', { name: 'products.sortNewest' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'products.sortPriceLowHigh' })).toBeInTheDocument();
  });

  it('shows refetch badge when isRefetching is true', () => {
    renderToolbar(true);

    expect(screen.getByText('common.updating')).toBeInTheDocument();
  });

  it('hides refetch badge when isRefetching is false', () => {
    renderToolbar(false);

    expect(screen.queryByText('common.updating')).not.toBeInTheDocument();
  });

  it('reflects sortBy value from URL in select', () => {
    renderToolbar(false, '/?sortBy=price-asc');

    expect((screen.getByRole('combobox') as HTMLSelectElement).value).toBe('price-asc');
  });

  it('defaults sort to "newest" when no URL param', () => {
    renderToolbar(false, '/');

    expect((screen.getByRole('combobox') as HTMLSelectElement).value).toBe('newest');
  });

  it('renders active filters section', () => {
    renderToolbar();

    expect(screen.getByTestId('active-filters')).toBeInTheDocument();
  });
});

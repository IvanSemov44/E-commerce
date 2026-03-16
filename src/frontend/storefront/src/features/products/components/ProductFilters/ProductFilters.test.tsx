import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { MemoryRouter, Routes, Route } from 'react-router';
import { ProductFilters } from './ProductFilters';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

function renderProductFilters(url = '/') {
  return render(
    <MemoryRouter initialEntries={[url]}>
      <Routes>
        <Route path="*" element={<ProductFilters />} />
      </Routes>
    </MemoryRouter>
  );
}

describe('ProductFilters', () => {
  it('renders price range inputs', () => {
    renderProductFilters();

    expect(screen.getByPlaceholderText(/min/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/max/i)).toBeInTheDocument();
  });

  it('renders rating select dropdown', () => {
    renderProductFilters();

    expect(screen.getByRole('combobox')).toBeInTheDocument();
  });

  it('renders featured checkbox', () => {
    renderProductFilters();

    expect(screen.getByRole('checkbox')).toBeInTheDocument();
  });

  it('displays price values from URL params', () => {
    renderProductFilters('/?minPrice=10&maxPrice=100');

    expect((screen.getByPlaceholderText(/min/i) as HTMLInputElement).value).toBe('10');
    expect((screen.getByPlaceholderText(/max/i) as HTMLInputElement).value).toBe('100');
  });

  it('displays rating value from URL params', () => {
    renderProductFilters('/?minRating=4.5');

    expect((screen.getByRole('combobox') as HTMLSelectElement).value).toBe('4.5');
  });

  it('displays checked featured from URL params', () => {
    renderProductFilters('/?isFeatured=true');

    expect((screen.getByRole('checkbox') as HTMLInputElement).checked).toBe(true);
  });

  it('typing in min price input updates value', async () => {
    const user = userEvent.setup();
    renderProductFilters();

    const minInput = screen.getByPlaceholderText(/min/i);
    await user.type(minInput, '50');

    expect((minInput as HTMLInputElement).value).toBe('50');
  });

  it('typing in max price input updates value', async () => {
    const user = userEvent.setup();
    renderProductFilters();

    const maxInput = screen.getByPlaceholderText(/max/i);
    await user.type(maxInput, '200');

    expect((maxInput as HTMLInputElement).value).toBe('200');
  });

  it('selecting a rating option updates the select', async () => {
    const user = userEvent.setup();
    renderProductFilters();

    const select = screen.getByRole('combobox');
    await user.selectOptions(select, '4');

    expect((select as HTMLSelectElement).value).toBe('4');
  });

  it('checking featured checkbox reflects in UI', async () => {
    const user = userEvent.setup();
    renderProductFilters();

    const checkbox = screen.getByRole('checkbox');
    await user.click(checkbox);

    expect((checkbox as HTMLInputElement).checked).toBe(true);
  });
});

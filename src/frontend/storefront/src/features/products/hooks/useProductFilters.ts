import { useSearchParams } from 'react-router';
import type { SortBy } from '@/features/products/constants';
import { parseOptionalFloat, parseSortBy } from '@/features/products/utils/parsing';

export interface ProductFiltersState {
  page: number;
  selectedCategoryId: string | undefined;
  search: string;
  minPrice: number | undefined;
  maxPrice: number | undefined;
  minRating: number | undefined;
  sortBy: SortBy;
  isFeatured: boolean | undefined;
  hasActiveFilters: boolean;
}

export function useProductFilters(): ProductFiltersState {
  const [searchParams] = useSearchParams();

  const search = searchParams.get('search') ?? '';
  const selectedCategoryId = searchParams.get('categoryId') ?? undefined;
  const minPrice = parseOptionalFloat(searchParams.get('minPrice'));
  const maxPrice = parseOptionalFloat(searchParams.get('maxPrice'));
  const minRating = parseOptionalFloat(searchParams.get('minRating'));
  const sortBy = parseSortBy(searchParams.get('sortBy'));
  const isFeatured = searchParams.get('isFeatured') === 'true' ? true : undefined;
  const page = parseInt(searchParams.get('page') ?? '1', 10) || 1;

  const hasActiveFilters = !!(
    selectedCategoryId ||
    search ||
    minPrice !== undefined ||
    maxPrice !== undefined ||
    minRating !== undefined ||
    isFeatured
  );

  return {
    page,
    selectedCategoryId,
    search,
    minPrice,
    maxPrice,
    minRating,
    sortBy,
    isFeatured,
    hasActiveFilters,
  };
}

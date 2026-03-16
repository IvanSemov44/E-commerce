import { useSearchParams } from 'react-router';
import { VALID_SORT_BY, type SortBy } from '@/features/products/constants';

function parseSortBy(value: string | null): SortBy {
  return value && (VALID_SORT_BY as readonly string[]).includes(value)
    ? (value as SortBy)
    : 'newest';
}

function parseFloat_(value: string | null): number | undefined {
  return value ? parseFloat(value) : undefined;
}

export interface ProductFiltersState {
  page: number;
  selectedCategoryId: string | undefined;
  debouncedSearch: string;
  minPrice: number | undefined;
  maxPrice: number | undefined;
  minRating: number | undefined;
  sortBy: SortBy;
  isFeatured: boolean | undefined;
  hasActiveFilters: boolean;
}

export function useProductFilters(): ProductFiltersState {
  const [searchParams] = useSearchParams();

  const debouncedSearch = searchParams.get('search') ?? '';
  const selectedCategoryId = searchParams.get('categoryId') ?? undefined;
  const minPrice = parseFloat_(searchParams.get('minPrice'));
  const maxPrice = parseFloat_(searchParams.get('maxPrice'));
  const minRating = parseFloat_(searchParams.get('minRating'));
  const sortBy = parseSortBy(searchParams.get('sortBy'));
  const isFeatured = searchParams.get('isFeatured') === 'true' ? true : undefined;
  const page = parseInt(searchParams.get('page') ?? '1', 10) || 1;

  const hasActiveFilters = !!(
    selectedCategoryId ||
    debouncedSearch ||
    minPrice !== undefined ||
    maxPrice !== undefined ||
    minRating !== undefined ||
    isFeatured
  );

  return {
    page,
    selectedCategoryId,
    debouncedSearch,
    minPrice,
    maxPrice,
    minRating,
    sortBy,
    isFeatured,
    hasActiveFilters,
  };
}

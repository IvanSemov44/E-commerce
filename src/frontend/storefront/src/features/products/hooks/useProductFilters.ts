import { useEffect, useRef } from 'react';
import { useSearchParams } from 'react-router';
import { useDebounce } from '@/shared/hooks';
import { useFilterState } from './useFilterState';
import type { FilterStateReturn } from './useFilterState';

export type UseProductFiltersReturn = FilterStateReturn & { debouncedSearch: string };

// Valid sortBy values that match backend validation
const VALID_SORT_BY = ['newest', 'name', 'price-asc', 'price-desc', 'rating'];

function parseSortBy(value: string | null): string {
  return value && VALID_SORT_BY.includes(value) ? value : 'newest';
}

function parseFloat_(value: string | null): number | undefined {
  return value ? parseFloat(value) : undefined;
}

export function useProductFilters(): UseProductFiltersReturn {
  const [searchParams, setSearchParams] = useSearchParams();

  const filters = useFilterState({
    page: parseInt(searchParams.get('page') ?? '1', 10) || 1,
    selectedCategoryId: searchParams.get('categoryId') ?? undefined,
    searchInput: searchParams.get('search') ?? '',
    minPrice: parseFloat_(searchParams.get('minPrice')),
    maxPrice: parseFloat_(searchParams.get('maxPrice')),
    minRating: parseFloat_(searchParams.get('minRating')),
    sortBy: parseSortBy(searchParams.get('sortBy')),
    isFeatured: searchParams.get('isFeatured') === 'true' ? true : undefined,
  });

  const debouncedSearch = useDebounce(filters.searchInput, 500);
  const { setPage } = filters;

  // Reset page when the debounced search settles to a new value (skip initial mount)
  const isFirstRender = useRef(true);
  useEffect(() => {
    if (isFirstRender.current) {
      isFirstRender.current = false;
      return;
    }
    setPage(1);
  }, [debouncedSearch, setPage]);

  // Sync all filter state back to URL params
  useEffect(() => {
    const params = new URLSearchParams();

    if (debouncedSearch) params.set('search', debouncedSearch);
    if (filters.selectedCategoryId) params.set('categoryId', filters.selectedCategoryId);
    if (filters.minPrice !== undefined) params.set('minPrice', filters.minPrice.toString());
    if (filters.maxPrice !== undefined) params.set('maxPrice', filters.maxPrice.toString());
    if (filters.minRating !== undefined) params.set('minRating', filters.minRating.toString());
    if (filters.sortBy !== 'newest') params.set('sortBy', filters.sortBy);
    if (filters.isFeatured) params.set('isFeatured', 'true');
    if (filters.page > 1) params.set('page', filters.page.toString());

    setSearchParams(params, { replace: true });
  }, [
    debouncedSearch,
    filters.selectedCategoryId,
    filters.minPrice,
    filters.maxPrice,
    filters.minRating,
    filters.sortBy,
    filters.isFeatured,
    filters.page,
    setSearchParams,
  ]);

  return { ...filters, debouncedSearch };
}

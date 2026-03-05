/**
 * useProductFilters Hook
 * Manages product filter state, URL synchronization, and debouncing
 */

import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';

export interface ProductFiltersState {
  page: number;
  selectedCategoryId: string | undefined;
  searchInput: string;
  debouncedSearch: string;
  minPrice: number | undefined;
  maxPrice: number | undefined;
  minRating: number | undefined;
  sortBy: string;
  isFeatured: boolean | undefined;
  hasActiveFilters: boolean;
}

export interface ProductFiltersActions {
  setPage: (page: number) => void;
  setSelectedCategoryId: (id: string | undefined) => void;
  setSearchInput: (search: string) => void;
  setMinPrice: (price: number | undefined) => void;
  setMaxPrice: (price: number | undefined) => void;
  setMinRating: (rating: number | undefined) => void;
  setSortBy: (sort: string) => void;
  setIsFeatured: (featured: boolean | undefined) => void;
  handleClearFilters: () => void;
}

export type UseProductFiltersReturn = ProductFiltersState & ProductFiltersActions;

// Valid sortBy values that match backend validation
const VALID_SORT_BY = ['newest', 'name', 'price-asc', 'price-desc', 'rating'];

/**
 * Custom hook for managing product filters with URL synchronization
 * Handles:
 * - Lazy initialization from URL params
 * - Search input debouncing (500ms)
 * - Automatic page reset when filters change
 * - URL param synchronization
 * - Clear all filters
 */
export const useProductFilters = (): UseProductFiltersReturn => {
  const [searchParams, setSearchParams] = useSearchParams();

  // Initialize state from URL params
  const [page, setPage] = useState(() => {
    const pageParam = searchParams.get('page');
    return pageParam ? parseInt(pageParam, 10) : 1;
  });

  const [selectedCategoryId, setSelectedCategoryId] = useState<string | undefined>(
    () => searchParams.get('categoryId') || undefined
  );

  const [searchInput, setSearchInput] = useState(() => searchParams.get('search') || '');
  const [debouncedSearch, setDebouncedSearch] = useState(() => searchParams.get('search') || '');

  const [minPrice, setMinPrice] = useState<number | undefined>(() => {
    const val = searchParams.get('minPrice');
    return val ? parseFloat(val) : undefined;
  });

  const [maxPrice, setMaxPrice] = useState<number | undefined>(() => {
    const val = searchParams.get('maxPrice');
    return val ? parseFloat(val) : undefined;
  });

  const [minRating, setMinRating] = useState<number | undefined>(() => {
    const val = searchParams.get('minRating');
    return val ? parseFloat(val) : undefined;
  });

  const [sortBy, setSortBy] = useState<string>(() => {
    const urlSortBy = searchParams.get('sortBy');
    // Validate sortBy against allowed values, default to 'newest' if invalid
    return urlSortBy && VALID_SORT_BY.includes(urlSortBy) ? urlSortBy : 'newest';
  });

  const [isFeatured, setIsFeatured] = useState<boolean | undefined>(() => {
    const val = searchParams.get('isFeatured');
    return val === 'true' ? true : undefined;
  });

  // Debounce search input (500ms)
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchInput);
      // Reset to page 1 when search changes
      if (searchInput !== debouncedSearch) {
        setPage(1);
      }
    }, 500);

    return () => clearTimeout(timer);
  }, [searchInput, debouncedSearch]);

  // Wrapped setters that reset page to 1
  const handleSetSelectedCategoryId = (id: string | undefined) => {
    setSelectedCategoryId(id);
    setPage(1);
  };

  const handleSetMinPrice = (price: number | undefined) => {
    setMinPrice(price);
    setPage(1);
  };

  const handleSetMaxPrice = (price: number | undefined) => {
    setMaxPrice(price);
    setPage(1);
  };

  const handleSetMinRating = (rating: number | undefined) => {
    setMinRating(rating);
    setPage(1);
  };

  const handleSetSortBy = (sort: string) => {
    setSortBy(sort);
    setPage(1);
  };

  const handleSetIsFeatured = (featured: boolean | undefined) => {
    setIsFeatured(featured);
    setPage(1);
  };

  // Sync URL when filters change (page resets to 1 when filters change)
  useEffect(() => {
    const params = new URLSearchParams();

    if (debouncedSearch) params.set('search', debouncedSearch);
    if (selectedCategoryId) params.set('categoryId', selectedCategoryId);
    if (minPrice !== undefined) params.set('minPrice', minPrice.toString());
    if (maxPrice !== undefined) params.set('maxPrice', maxPrice.toString());
    if (minRating !== undefined) params.set('minRating', minRating.toString());
    if (sortBy !== 'newest') params.set('sortBy', sortBy);
    if (isFeatured) params.set('isFeatured', 'true');
    if (page > 1) params.set('page', page.toString());

    setSearchParams(params, { replace: true });
  }, [debouncedSearch, selectedCategoryId, minPrice, maxPrice, minRating, sortBy, isFeatured, page, setSearchParams]);

  // Compute derived state
  const hasActiveFilters = !!(
    selectedCategoryId ||
    debouncedSearch ||
    minPrice !== undefined ||
    maxPrice !== undefined ||
    minRating !== undefined ||
    isFeatured
  );

  // Clear all filters
  const handleClearFilters = () => {
    setSelectedCategoryId(undefined);
    setSearchInput('');
    setMinPrice(undefined);
    setMaxPrice(undefined);
    setMinRating(undefined);
    setSortBy('newest');
    setIsFeatured(undefined);
    setPage(1);
  };

  return {
    page,
    selectedCategoryId,
    searchInput,
    debouncedSearch,
    minPrice,
    maxPrice,
    minRating,
    sortBy,
    isFeatured,
    hasActiveFilters,
    setPage,
    setSelectedCategoryId: handleSetSelectedCategoryId,
    setSearchInput,
    setMinPrice: handleSetMinPrice,
    setMaxPrice: handleSetMaxPrice,
    setMinRating: handleSetMinRating,
    setSortBy: handleSetSortBy,
    setIsFeatured: handleSetIsFeatured,
    handleClearFilters,
  };
};

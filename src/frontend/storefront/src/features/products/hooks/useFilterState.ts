import { useState } from 'react';
import type { SortBy } from '@/features/products/constants';

export interface FilterValues {
  page: number;
  selectedCategoryId: string | undefined;
  searchInput: string;
  minPrice: number | undefined;
  maxPrice: number | undefined;
  minRating: number | undefined;
  sortBy: SortBy;
  isFeatured: boolean | undefined;
}

export interface FilterStateReturn extends FilterValues {
  hasActiveFilters: boolean;
  setPage: (page: number) => void;
  setSearchInput: (search: string) => void;
  setSelectedCategoryId: (id: string | undefined) => void;
  setMinPrice: (price: number | undefined) => void;
  setMaxPrice: (price: number | undefined) => void;
  setMinRating: (rating: number | undefined) => void;
  setSortBy: (sort: SortBy) => void;
  setIsFeatured: (featured: boolean | undefined) => void;
  handleClearFilters: () => void;
}

const DEFAULTS: FilterValues = {
  page: 1,
  selectedCategoryId: undefined,
  searchInput: '',
  minPrice: undefined,
  maxPrice: undefined,
  minRating: undefined,
  sortBy: 'newest',
  isFeatured: undefined,
};

export function useFilterState(initial: Partial<FilterValues> = {}): FilterStateReturn {
  const init = { ...DEFAULTS, ...initial };

  const [page, setPage] = useState(init.page);
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | undefined>(
    init.selectedCategoryId
  );
  const [searchInput, setSearchInput] = useState(init.searchInput);
  const [minPrice, setMinPrice] = useState<number | undefined>(init.minPrice);
  const [maxPrice, setMaxPrice] = useState<number | undefined>(init.maxPrice);
  const [minRating, setMinRating] = useState<number | undefined>(init.minRating);
  const [sortBy, setSortBy] = useState(init.sortBy);
  const [isFeatured, setIsFeatured] = useState<boolean | undefined>(init.isFeatured);

  // Wrapped setters that reset page to 1 when a filter changes
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

  const handleSetSortBy = (sort: SortBy) => {
    setSortBy(sort);
    setPage(1);
  };

  const handleSetIsFeatured = (featured: boolean | undefined) => {
    setIsFeatured(featured);
    setPage(1);
  };

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

  const hasActiveFilters = !!(
    selectedCategoryId ||
    searchInput ||
    minPrice !== undefined ||
    maxPrice !== undefined ||
    minRating !== undefined ||
    isFeatured
  );

  return {
    page,
    selectedCategoryId,
    searchInput,
    minPrice,
    maxPrice,
    minRating,
    sortBy,
    isFeatured,
    hasActiveFilters,
    setPage,
    setSearchInput,
    setSelectedCategoryId: handleSetSelectedCategoryId,
    setMinPrice: handleSetMinPrice,
    setMaxPrice: handleSetMaxPrice,
    setMinRating: handleSetMinRating,
    setSortBy: handleSetSortBy,
    setIsFeatured: handleSetIsFeatured,
    handleClearFilters,
  };
}

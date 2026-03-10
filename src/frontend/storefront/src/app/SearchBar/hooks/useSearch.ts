import { useState, useEffect, useCallback, useMemo } from 'react';
import { useGetProductsQuery } from '@/features/products/api/productApi';

interface UseSearchOptions {
  debounceMs?: number;
  minChars?: number;
  pageSize?: number;
}

/**
 * useSearch Hook
 *
 * Handles search input debouncing and product API queries
 * Provides debounced results for use in search components
 *
 * @param options - Configuration options
 * @returns Search state including query, results, loading, and error states
 */
export function useSearch(options: UseSearchOptions = {}) {
  const { debounceMs = 300, minChars = 2, pageSize = 5 } = options;
  const [query, setQuery] = useState('');
  const [debouncedQuery, setDebouncedQuery] = useState('');

  // Debounce search query
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedQuery(query.trim());
    }, debounceMs);
    return () => clearTimeout(timer);
  }, [query, debounceMs]);

  // Fetch products when debounced query is ready
  const {
    data: searchResults,
    isFetching,
    error,
  } = useGetProductsQuery(
    { search: debouncedQuery, pageSize },
    { skip: debouncedQuery.length < minChars }
  );

  // Memoize results
  const results = useMemo(() => searchResults?.items || [], [searchResults]);

  const handleClear = useCallback(() => {
    setQuery('');
    setDebouncedQuery('');
  }, []);

  return {
    query,
    setQuery,
    debouncedQuery,
    results,
    isFetching,
    error,
    isSearching: isFetching || debouncedQuery.length >= minChars,
    handleClear,
  };
}

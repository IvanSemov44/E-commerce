import { useState, useEffect } from 'react';
import { skipToken } from '@reduxjs/toolkit/query';
import { useGetProductsQuery } from '@/features/products/api/productApi';
import type { SearchResult } from '../SearchBar.types';

const MIN_LENGTH = 2;
const PAGE_SIZE = 6;
const DEBOUNCE_MS = 350;

export function useProductSearch(query: string) {
  const trimmed = query.trim();
  const [debounced, setDebounced] = useState('');

  useEffect(() => {
    const id = setTimeout(() => setDebounced(trimmed), DEBOUNCE_MS);
    return () => clearTimeout(id);
  }, [trimmed]);

  const { data, isFetching } = useGetProductsQuery(
    debounced.length >= MIN_LENGTH ? { search: debounced, pageSize: PAGE_SIZE } : skipToken
  );

  const results: SearchResult[] = debounced.length >= MIN_LENGTH ? (data?.items ?? []) : [];

  return {
    results,
    isLoading: isFetching,
    isStale: debounced !== trimmed,
  };
}

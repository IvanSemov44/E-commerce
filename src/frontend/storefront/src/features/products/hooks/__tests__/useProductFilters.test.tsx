import { describe, it, expect, beforeEach } from 'vitest';
import { renderHook } from '@testing-library/react';
import { BrowserRouter } from 'react-router';
import { useProductFilters } from '../useProductFilters';

const wrapper = ({ children }: { children: React.ReactNode }) => (
  <BrowserRouter>{children}</BrowserRouter>
);

describe('useProductFilters', () => {
  beforeEach(() => {
    window.history.pushState({}, '', '/products');
  });

  it('returns defaults when no URL params are present', () => {
    const { result } = renderHook(() => useProductFilters(), { wrapper });

    expect(result.current.page).toBe(1);
    expect(result.current.search).toBe('');
    expect(result.current.sortBy).toBe('newest');
    expect(result.current.selectedCategoryId).toBeUndefined();
    expect(result.current.minPrice).toBeUndefined();
    expect(result.current.maxPrice).toBeUndefined();
    expect(result.current.minRating).toBeUndefined();
    expect(result.current.isFeatured).toBeUndefined();
    expect(result.current.hasActiveFilters).toBe(false);
  });

  it('reads page from URL', () => {
    window.history.pushState({}, '', '/products?page=3');
    const { result } = renderHook(() => useProductFilters(), { wrapper });

    expect(result.current.page).toBe(3);
  });

  it('reads search from URL', () => {
    window.history.pushState({}, '', '/products?search=laptop');
    const { result } = renderHook(() => useProductFilters(), { wrapper });

    expect(result.current.search).toBe('laptop');
  });

  it('reads categoryId from URL', () => {
    window.history.pushState({}, '', '/products?categoryId=cat-1');
    const { result } = renderHook(() => useProductFilters(), { wrapper });

    expect(result.current.selectedCategoryId).toBe('cat-1');
  });

  it('reads price range from URL', () => {
    window.history.pushState({}, '', '/products?minPrice=10&maxPrice=200');
    const { result } = renderHook(() => useProductFilters(), { wrapper });

    expect(result.current.minPrice).toBe(10);
    expect(result.current.maxPrice).toBe(200);
  });

  it('reads minRating from URL', () => {
    window.history.pushState({}, '', '/products?minRating=4.5');
    const { result } = renderHook(() => useProductFilters(), { wrapper });

    expect(result.current.minRating).toBe(4.5);
  });

  it('reads sortBy from URL', () => {
    window.history.pushState({}, '', '/products?sortBy=price-asc');
    const { result } = renderHook(() => useProductFilters(), { wrapper });

    expect(result.current.sortBy).toBe('price-asc');
  });

  it('falls back to "newest" for invalid sortBy value', () => {
    window.history.pushState({}, '', '/products?sortBy=invalid-sort');
    const { result } = renderHook(() => useProductFilters(), { wrapper });

    expect(result.current.sortBy).toBe('newest');
  });

  it('reads isFeatured from URL', () => {
    window.history.pushState({}, '', '/products?isFeatured=true');
    const { result } = renderHook(() => useProductFilters(), { wrapper });

    expect(result.current.isFeatured).toBe(true);
  });

  it('hasActiveFilters is true when any filter is set', () => {
    window.history.pushState({}, '', '/products?minPrice=50');
    const { result } = renderHook(() => useProductFilters(), { wrapper });

    expect(result.current.hasActiveFilters).toBe(true);
  });

  it('hasActiveFilters is false when only page is set', () => {
    window.history.pushState({}, '', '/products?page=2');
    const { result } = renderHook(() => useProductFilters(), { wrapper });

    expect(result.current.hasActiveFilters).toBe(false);
  });
});

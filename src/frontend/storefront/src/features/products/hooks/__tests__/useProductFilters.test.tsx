import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import { useProductFilters } from '../useProductFilters'

// Mock useTranslation
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: {
      language: 'en',
    },
  }),
}))

describe('useProductFilters', () => {
  beforeEach(() => {
    // Clear URL params before each test
    window.history.pushState({}, '', '/products')
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  const wrapper = ({ children }: { children: React.ReactNode }) => (
    <BrowserRouter>{children}</BrowserRouter>
  )

  it('should initialize with default values', () => {
    const { result } = renderHook(() => useProductFilters(), { wrapper })

    expect(result.current.page).toBe(1)
    expect(result.current.searchInput).toBe('')
    expect(result.current.debouncedSearch).toBe('')
    expect(result.current.sortBy).toBe('newest')
    expect(result.current.hasActiveFilters).toBe(false)
  })

  it('should set page', () => {
    const { result } = renderHook(() => useProductFilters(), { wrapper })

    act(() => {
      result.current.setPage(2)
    })

    expect(result.current.page).toBe(2)
  })

  it('should set search input', () => {
    const { result } = renderHook(() => useProductFilters(), { wrapper })

    act(() => {
      result.current.setSearchInput('test search')
    })

    expect(result.current.searchInput).toBe('test search')
  })

  it('should set category filter', () => {
    const { result } = renderHook(() => useProductFilters(), { wrapper })

    act(() => {
      result.current.setSelectedCategoryId('category-1')
    })

    expect(result.current.selectedCategoryId).toBe('category-1')
  })

  it('should clear all filters', () => {
    const { result } = renderHook(() => useProductFilters(), { wrapper })

    // Set some filters
    act(() => {
      result.current.setPage(3)
      result.current.setSearchInput('test')
      result.current.setSelectedCategoryId('cat-1')
      result.current.setMinPrice(10)
      result.current.setMaxPrice(100)
    })

    // Clear filters
    act(() => {
      result.current.handleClearFilters()
    })

    expect(result.current.page).toBe(1)
    expect(result.current.searchInput).toBe('')
    expect(result.current.selectedCategoryId).toBeUndefined()
    expect(result.current.minPrice).toBeUndefined()
    expect(result.current.maxPrice).toBeUndefined()
  })

  it('should set sort by', () => {
    const { result } = renderHook(() => useProductFilters(), { wrapper })

    act(() => {
      result.current.setSortBy('price-asc')
    })

    expect(result.current.sortBy).toBe('price-asc')
  })

  it('should set price range', () => {
    const { result } = renderHook(() => useProductFilters(), { wrapper })

    act(() => {
      result.current.setMinPrice(50)
      result.current.setMaxPrice(200)
    })

    expect(result.current.minPrice).toBe(50)
    expect(result.current.maxPrice).toBe(200)
  })

  it('should detect active filters', () => {
    const { result } = renderHook(() => useProductFilters(), { wrapper })

    expect(result.current.hasActiveFilters).toBe(false)

    act(() => {
      result.current.setMinPrice(50)
    })

    expect(result.current.hasActiveFilters).toBe(true)
  })
})

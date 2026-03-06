import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { useSearch } from '../useSearch'

// Mock the API
const mockUseGetProductsQuery = vi.fn((): any => ({
  data: undefined,
  isFetching: false,
  error: null,
}))

vi.mock('@/features/products/api/productApi', () => ({
  useGetProductsQuery: () => mockUseGetProductsQuery(),
}))

describe('useSearch', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockUseGetProductsQuery.mockReturnValue({
      data: undefined,
      isFetching: false,
      error: null,
    })
  })

  it('should initialize with empty query', () => {
    const { result } = renderHook(() => useSearch())

    expect(result.current.query).toBe('')
    expect(result.current.debouncedQuery).toBe('')
    expect(result.current.results).toEqual([])
  })

  it('should update query immediately', () => {
    const { result } = renderHook(() => useSearch())

    act(() => {
      result.current.setQuery('wireless')
    })

    expect(result.current.query).toBe('wireless')
  })

  it('should debounce query update', async () => {
    const { result } = renderHook(() => useSearch({ debounceMs: 100 }))

    act(() => {
      result.current.setQuery('wireless')
    })

    // Should not have debounced yet
    expect(result.current.debouncedQuery).toBe('')

    // Wait for debounce
    await new Promise((resolve) => setTimeout(resolve, 150))

    expect(result.current.debouncedQuery).toBe('wireless')
  })

  it('should respect minChars option', () => {
    const { result } = renderHook(() => useSearch({ minChars: 3 }))

    act(() => {
      result.current.setQuery('wi')
    })

    expect(result.current.isSearching).toBe(false)
  })

  it('should clear query and debounced query', () => {
    const { result } = renderHook(() => useSearch())

    act(() => {
      result.current.setQuery('search term')
    })

    expect(result.current.query).toBe('search term')

    act(() => {
      result.current.handleClear()
    })

    expect(result.current.query).toBe('')
  })

  it('should memoize results', () => {
    const mockData = {
      items: [
        { id: '1', name: 'Product 1', slug: 'product-1', price: 10 },
      ],
    }
    mockUseGetProductsQuery.mockReturnValue({
      data: mockData,
      isFetching: false,
      error: null,
    })

    const { result, rerender } = renderHook(() => useSearch())

    const firstResults = result.current.results

    rerender()

    const secondResults = result.current.results
    expect(firstResults).toStrictEqual(secondResults)
  })

  it('should return isSearching when fetching', () => {
    const { result } = renderHook(() => useSearch())

    act(() => {
      result.current.setQuery('wi')
    })

    // With default minChars=2, should not be searching with just 'wi' before debounce
    expect(result.current.isSearching).toBe(false)
  })
})

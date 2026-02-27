import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { Provider } from 'react-redux'
import { configureStore } from '@reduxjs/toolkit'
import Home from '../Home'
import toastReducer from '../../store/slices/toastSlice'
import { baseApi } from '../../store/api/baseApi'

// Mock the RTK Query hook
vi.mock('../../store/api/productApi', () => ({
  useGetFeaturedProductsQuery: vi.fn(),
  useGetTopLevelCategoriesQuery: vi.fn(),
}))

// Mock ProductCard component
vi.mock('../../components/ProductCard', () => ({
  default: ({ name, price }: { name: string; price: number }) => (
    <div data-testid="product-card">
      <span>{name}</span>
      <span>${price}</span>
    </div>
  ),
}))

// Mock QueryRenderer to simplify testing
vi.mock('../../components/QueryRenderer', () => ({
  default: ({ isLoading, error, data, children }: any) => {
    if (isLoading) return <div data-testid="loading">Loading...</div>
    if (error) return <div data-testid="error">Error loading products</div>
    if (!data || data.length === 0) return <div data-testid="empty">No products</div>
    return children(data)
  },
}))

import { useGetFeaturedProductsQuery } from '../../store/api/productApi'

const createTestStore = () => {
  return configureStore({
    reducer: {
      toast: toastReducer,
      [baseApi.reducerPath]: baseApi.reducer,
    },
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware().concat(baseApi.middleware),
  })
}

const renderHome = () => {
  return render(
    <Provider store={createTestStore()}>
      <MemoryRouter>
        <Home />
      </MemoryRouter>
    </Provider>
  )
}

describe('Home Page', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    cleanup()
  })

  describe('Hero Section', () => {
    it('should render the hero title', () => {
      vi.mocked(useGetFeaturedProductsQuery).mockReturnValue({
        data: [],
        isLoading: false,
        error: null,
      } as any)

      renderHome()

      expect(screen.getByText('Discover Premium Products')).toBeInTheDocument()
    })

    it('should render the hero subtitle', () => {
      vi.mocked(useGetFeaturedProductsQuery).mockReturnValue({
        data: [],
        isLoading: false,
        error: null,
      } as any)

      renderHome()

      expect(screen.getByText('Curated selection of quality items at exceptional prices')).toBeInTheDocument()
    })

    it('should render the explore products button', () => {
      vi.mocked(useGetFeaturedProductsQuery).mockReturnValue({
        data: [],
        isLoading: false,
        error: null,
      } as any)

      renderHome()

      expect(screen.getByText('Explore Products')).toBeInTheDocument()
    })

    it('should have a link to products page', () => {
      vi.mocked(useGetFeaturedProductsQuery).mockReturnValue({
        data: [],
        isLoading: false,
        error: null,
      } as any)

      renderHome()

      const link = screen.getByRole('link', { name: /explore products/i })
      expect(link).toHaveAttribute('href', '/products')
    })
  })

  describe('Featured Products Section', () => {
    it('should render the section title', () => {
      vi.mocked(useGetFeaturedProductsQuery).mockReturnValue({
        data: [],
        isLoading: false,
        error: null,
      } as any)

      renderHome()

      expect(screen.getByText('Featured Products')).toBeInTheDocument()
    })

    it('should show loading state', () => {
      vi.mocked(useGetFeaturedProductsQuery).mockReturnValue({
        data: null,
        isLoading: true,
        error: null,
      } as any)

      renderHome()

      expect(screen.getByTestId('loading')).toBeInTheDocument()
    })

    it('should show error state', () => {
      vi.mocked(useGetFeaturedProductsQuery).mockReturnValue({
        data: null,
        isLoading: false,
        error: { message: 'Failed to load' },
      } as any)

      renderHome()

      expect(screen.getByTestId('error')).toBeInTheDocument()
    })

    it('should show empty state when no products', () => {
      vi.mocked(useGetFeaturedProductsQuery).mockReturnValue({
        data: [],
        isLoading: false,
        error: null,
      } as any)

      renderHome()

      expect(screen.getByTestId('empty')).toBeInTheDocument()
    })

    it('should render featured products', () => {
      const mockProducts = [
        {
          id: '1',
          name: 'Product 1',
          slug: 'product-1',
          price: 99.99,
          images: [{ url: 'image1.jpg' }],
          averageRating: 4.5,
          reviewCount: 10,
        },
        {
          id: '2',
          name: 'Product 2',
          slug: 'product-2',
          price: 149.99,
          images: [{ url: 'image2.jpg' }],
          averageRating: 4.0,
          reviewCount: 5,
        },
      ]

      vi.mocked(useGetFeaturedProductsQuery).mockReturnValue({
        data: mockProducts,
        isLoading: false,
        error: null,
      } as any)

      renderHome()

      expect(screen.getByText('Product 1')).toBeInTheDocument()
      expect(screen.getByText('Product 2')).toBeInTheDocument()
      expect(screen.getAllByTestId('product-card')).toHaveLength(2)
    })
  })

  describe('API Integration', () => {
    it('should call useGetFeaturedProductsQuery with correct limit', () => {
      vi.mocked(useGetFeaturedProductsQuery).mockReturnValue({
        data: [],
        isLoading: false,
        error: null,
      } as any)

      renderHome()

      expect(useGetFeaturedProductsQuery).toHaveBeenCalledWith(10)
    })
  })
})
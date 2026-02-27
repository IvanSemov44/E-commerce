/**
 * Test Utilities
 * 
 * Shared utilities for writing tests across the application.
 * Use these to reduce boilerplate and ensure consistent test patterns.
 */

import type { ReactElement, ReactNode } from 'react';
import { render } from '@testing-library/react';
import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';
import { configureStore } from '@reduxjs/toolkit';
import { I18nextProvider } from 'react-i18next';
import i18n from 'i18next';
import type { EnhancedStore } from '@reduxjs/toolkit';

// ============================================================================
// Store Creation
// ============================================================================

/**
 * Create a test store with optional preloaded state
 * Note: Add your actual API and slice imports as needed
 */
export function createTestStore(preloadedState = {}): EnhancedStore {
  return configureStore({
    reducer: {
      // Add your actual slices here
      // auth: authReducer,
      // cart: cartReducer,
    },
    preloadedState,
  });
}

/**
 * Create a minimal store for simple tests
 */
export function createMinimalStore() {
  return configureStore({
    reducer: {},
  });
}

// ============================================================================
// Mock Data Factories
// ============================================================================

export const mockProduct = (overrides = {}) => ({
  id: '1',
  name: 'Test Product',
  slug: 'test-product',
  price: 29.99,
  compareAtPrice: 39.99,
  description: 'A test product description',
  shortDescription: 'Test product',
  images: [{ id: '1', url: 'test.jpg', isPrimary: true }],
  stockQuantity: 10,
  averageRating: 4.5,
  reviewCount: 100,
  isFeatured: true,
  category: { id: '1', name: 'Test Category', slug: 'test-category' },
  ...overrides,
});

export const mockCartItem = (overrides = {}) => ({
  id: '1',
  productId: 'p1',
  name: 'Test Product',
  slug: 'test-product',
  price: 29.99,
  quantity: 2,
  maxStock: 10,
  image: 'test.jpg',
  ...overrides,
});

export const mockUser = (overrides = {}) => ({
  id: '1',
  email: 'test@example.com',
  firstName: 'John',
  lastName: 'Doe',
  role: 'customer',
  ...overrides,
});

export const mockOrder = (overrides = {}) => ({
  id: '1',
  orderNumber: 'ORD-001',
  createdAt: '2024-01-15T10:00:00Z',
  status: 'processing',
  totalAmount: 150.00,
  items: [
    { productName: 'Product 1', quantity: 2, unitPrice: 50.00, totalPrice: 100.00 },
    { productName: 'Product 2', quantity: 1, unitPrice: 50.00, totalPrice: 50.00 },
  ],
  shippingAddress: {
    firstName: 'John',
    lastName: 'Doe',
    streetLine1: '123 Main St',
    city: 'New York',
    state: 'NY',
    postalCode: '10001',
    country: 'USA',
  },
  ...overrides,
});

export const mockApiResponse = <T,>(data: T): Promise<T> => 
  Promise.resolve(data);

export const mockApiError = (message: string, status = 400) => {
  const error = new Error(message) as unknown as { status: number; data: { message: string } };
  error.status = status;
  error.data = { message };
  return error;
};

// ============================================================================
// Custom Render Functions
// ============================================================================

interface CustomRenderOptions {
  store?: EnhancedStore;
  router?: boolean;
  preloadedState?: object;
}

/**
 * Render with Redux Provider and optional Router and i18n
 */
export function renderWithProviders(
  ui: ReactElement,
  options: CustomRenderOptions = {}
) {
  const { 
    store = createTestStore(), 
    router = false,
    preloadedState,
  } = options;

  // If preloadedState provided, create new store with it
  const finalStore = preloadedState 
    ? createTestStore(preloadedState)
    : store;

  function Wrapper({ children }: { children: ReactNode }) {
    let content = (
      <I18nextProvider i18n={i18n}>
        <Provider store={finalStore}>
          {children}
        </Provider>
      </I18nextProvider>
    );
    
    if (router) {
      content = <BrowserRouter>{content}</BrowserRouter>;
    }
    
    return content;
  }

  return {
    ...render(ui, { wrapper: Wrapper }),
    store: finalStore,
  };
}

/**
 * Render with just Router (no Redux, but with i18n)
 */
export function renderWithRouter(ui: ReactElement) {
  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <I18nextProvider i18n={i18n}>
        <BrowserRouter>
          {children}
        </BrowserRouter>
      </I18nextProvider>
    );
  }
  
  return render(ui, { wrapper: Wrapper });
}

// ============================================================================
// Mock API Responses
// ============================================================================

/**
 * Setup RTK Query mock handlers
 */
export const mockApiHandlers = {
  getProducts: {
    items: [mockProduct()],
    totalCount: 1,
    pageNumber: 1,
    pageSize: 12,
  },
  
  getCart: {
    items: [mockCartItem()],
    subtotal: 59.98,
    itemCount: 2,
  },
  
  getOrders: [mockOrder()],
  
  login: {
    token: 'mock-token',
    refreshToken: 'mock-refresh-token',
    user: mockUser(),
  },
  
  register: mockUser(),
};

// ============================================================================
// Re-export commonly used testing utilities
// ============================================================================

export * from '@testing-library/react';

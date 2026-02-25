import { Page, Route } from '@playwright/test';
import { mockApiResponses, testProducts, testUsers } from '../data/test-data';

/**
 * API Mocking Utilities
 * Provides functions to mock API responses for reliable testing
 */

// Types for mock responses
type MockResponse = any;
type MockHandler = (route: Route) => Promise<void>;

// Base API URL (adjust based on environment)
const API_BASE_URL = process.env.API_URL || 'http://localhost:5000/api';

/**
 * Mock all product-related API calls
 */
export async function mockProductsApi(page: Page, options: {
  products?: typeof mockApiResponses.products.list;
  delay?: number;
  failRate?: number;
} = {}): Promise<void> {
  const response = options.products || mockApiResponses.products.list;
  
  // Mock products list
  await page.route('**/api/products*', async route => {
    if (options.failRate && Math.random() < options.failRate) {
      await route.fulfill({ status: 500, body: 'Internal Server Error' });
      return;
    }
    
    if (options.delay) {
      await new Promise(resolve => setTimeout(resolve, options.delay));
    }
    
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(response),
    });
  });

  // Mock single product
  await page.route('**/api/products/*', async route => {
    const url = route.request().url();
    const match = url.match(/\/api\/products\/(\d+)/);
    const productId = match ? parseInt(match[1]) : 1;
    
    const product = Object.values(testProducts).find(p => p.id === productId) || testProducts.laptop;
    
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(product),
    });
  });
}

/**
 * Mock authentication API calls
 */
export async function mockAuthApi(page: Page, options: {
  authenticated?: boolean;
  isAdmin?: boolean;
  failLogin?: boolean;
} = {}): Promise<void> {
  // Mock login
  await page.route('**/api/auth/login', async route => {
    if (options.failLogin) {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Invalid credentials' }),
      });
      return;
    }
    
    const user = options.isAdmin ? mockApiResponses.user.admin : mockApiResponses.user.authenticated;
    
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        user,
        token: 'mock-jwt-token',
        refreshToken: 'mock-refresh-token',
      }),
    });
  });

  // Mock register
  await page.route('**/api/auth/register', async route => {
    await route.fulfill({
      status: 201,
      contentType: 'application/json',
      body: JSON.stringify({
        user: mockApiResponses.user.authenticated,
        token: 'mock-jwt-token',
      }),
    });
  });

  // Mock current user
  await page.route('**/api/auth/me', async route => {
    if (!options.authenticated) {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Unauthorized' }),
      });
      return;
    }
    
    const user = options.isAdmin ? mockApiResponses.user.admin : mockApiResponses.user.authenticated;
    
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(user),
    });
  });

  // Mock logout
  await page.route('**/api/auth/logout', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ message: 'Logged out successfully' }),
    });
  });
}

/**
 * Mock cart API calls
 */
export async function mockCartApi(page: Page, options: {
  cart?: typeof mockApiResponses.cart.withItems;
  empty?: boolean;
} = {}): Promise<void> {
  const cart = options.empty ? mockApiResponses.cart.empty : (options.cart || mockApiResponses.cart.withItems);
  
  // Mock get cart
  await page.route('**/api/cart', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(cart),
    });
  });

  // Mock add to cart
  await page.route('**/api/cart/add', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ ...cart, itemCount: cart.itemCount + 1 }),
    });
  });

  // Mock update cart item
  await page.route('**/api/cart/items/*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(cart),
    });
  });

  // Mock remove from cart
  await page.route('**/api/cart/items/*', async route => {
    if (route.request().method() === 'DELETE') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(cart),
      });
    }
  });
}

/**
 * Mock checkout API calls
 */
export async function mockCheckoutApi(page: Page, options: {
  success?: boolean;
  orderId?: string;
} = {}): Promise<void> {
  // Mock create order
  await page.route('**/api/orders', async route => {
    if (route.request().method() === 'POST') {
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          orderId: options.orderId || 'ORD-TEST-001',
          status: 'pending',
          message: 'Order created successfully',
        }),
      });
    }
  });

  // Mock payment processing
  await page.route('**/api/payments/process', async route => {
    if (options.success === false) {
      await route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({
          message: 'Payment failed',
          error: 'CARD_DECLINED',
        }),
      });
      return;
    }
    
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        paymentId: 'PAY-TEST-001',
        status: 'completed',
        message: 'Payment processed successfully',
      }),
    });
  });
}

/**
 * Mock all APIs at once for full integration testing
 */
export async function mockAllApis(page: Page, options: {
  authenticated?: boolean;
  isAdmin?: boolean;
  emptyCart?: boolean;
  emptyProducts?: boolean;
} = {}): Promise<void> {
  await mockAuthApi(page, { 
    authenticated: options.authenticated, 
    isAdmin: options.isAdmin 
  });
  
  await mockProductsApi(page, { 
    products: options.emptyProducts ? mockApiResponses.products.empty : undefined 
  });
  
  await mockCartApi(page, { 
    empty: options.emptyCart 
  });
  
  await mockCheckoutApi(page);
}

/**
 * Clear all route handlers
 */
export async function clearMocks(page: Page): Promise<void> {
  await page.unrouteAll();
}

/**
 * Mock slow network conditions
 */
export async function mockSlowNetwork(page: Page, delayMs: number = 2000): Promise<void> {
  await page.route('**', async route => {
    await new Promise(resolve => setTimeout(resolve, delayMs));
    await route.continue();
  });
}

/**
 * Mock offline conditions
 */
export async function mockOffline(page: Page): Promise<void> {
  await page.route('**', route => route.abort('failed'));
}

/**
 * Mock specific HTTP status code
 */
export async function mockErrorStatus(page: Page, urlPattern: string, status: number): Promise<void> {
  await page.route(urlPattern, route => 
    route.fulfill({ status, body: `Error ${status}` })
  );
}

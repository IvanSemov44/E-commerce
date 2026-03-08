/**
 * Test Data Management
 * Centralized test data for consistent testing
 */

// Test Users
export const testUsers = {
  standard: {
    email: 'test@example.com',
    password: 'TestPassword123!',
    firstName: 'Test',
    lastName: 'User',
  },
  admin: {
    email: 'admin@example.com',
    password: 'AdminPassword123!',
    firstName: 'Admin',
    lastName: 'User',
  },
  guest: {
    email: 'guest@example.com',
  },
  invalid: {
    email: 'invalid@example.com',
    password: 'WrongPassword123!',
  },
};

// Test Products
export const testProducts = {
  laptop: {
    id: 1,
    name: 'Test Laptop',
    price: 999.99,
    category: 'Electronics',
    stock: 10,
    description: 'A high-quality test laptop',
  },
  phone: {
    id: 2,
    name: 'Test Phone',
    price: 699.99,
    category: 'Electronics',
    stock: 15,
    description: 'A modern test smartphone',
  },
  headphones: {
    id: 3,
    name: 'Test Headphones',
    price: 149.99,
    category: 'Accessories',
    stock: 25,
    description: 'Premium test headphones',
  },
  outOfStock: {
    id: 4,
    name: 'Out of Stock Item',
    price: 49.99,
    category: 'Test',
    stock: 0,
    description: 'This item is out of stock',
  },
};

// Test Addresses
export const testAddresses = {
  valid: {
    firstName: 'John',
    lastName: 'Doe',
    email: 'john.doe@example.com',
    phone: '555-123-4567',
    address: '123 Main Street',
    city: 'New York',
    state: 'NY',
    zip: '10001',
    country: 'US',
  },
  international: {
    firstName: 'Jane',
    lastName: 'Smith',
    email: 'jane.smith@example.com',
    phone: '+44 20 1234 5678',
    address: '456 High Street',
    city: 'London',
    state: '',
    zip: 'SW1A 1AA',
    country: 'GB',
  },
  invalid: {
    firstName: '',
    lastName: '',
    email: 'invalid-email',
    phone: '123',
    address: '',
    city: '',
    state: '',
    zip: 'abc',
    country: '',
  },
};

// Test Payment Cards
export const testCards = {
  validVisa: {
    cardNumber: '4111111111111111',
    number: '4111111111111111', // alias for compatibility
    expiry: '12/25',
    cvv: '123',
    nameOnCard: 'Test User',
  },
  validMastercard: {
    cardNumber: '5555555555554444',
    number: '5555555555554444', // alias for compatibility
    expiry: '06/26',
    cvv: '456',
    nameOnCard: 'Test User',
  },
  invalid: {
    cardNumber: '1234567890123456',
    number: '1234567890123456', // alias for compatibility
    expiry: '01/20',
    cvv: '1',
    nameOnCard: '',
  },
  expired: {
    cardNumber: '4111111111111111',
    number: '4111111111111111', // alias for compatibility
    expiry: '01/20',
    cvv: '123',
    nameOnCard: 'Test User',
  },
};

// Test Promo Codes
export const testPromoCodes = {
  valid: {
    code: 'SAVE10',
    discount: 10,
    type: 'percentage',
  },
  fixedDiscount: {
    code: 'FLAT20',
    discount: 20,
    type: 'fixed',
  },
  expired: {
    code: 'EXPIRED',
    discount: 0,
    type: 'invalid',
  },
  invalid: {
    code: 'INVALID',
    discount: 0,
    type: 'invalid',
  },
};

// Test Categories
export const testCategories = [
  { id: 1, name: 'Electronics', slug: 'electronics' },
  { id: 2, name: 'Clothing', slug: 'clothing' },
  { id: 3, name: 'Accessories', slug: 'accessories' },
  { id: 4, name: 'Home & Garden', slug: 'home-garden' },
];

// Test Orders
export const testOrders = {
  pending: {
    id: 'ORD-001',
    status: 'pending',
    total: 999.99,
  },
  processing: {
    id: 'ORD-002',
    status: 'processing',
    total: 699.99,
  },
  shipped: {
    id: 'ORD-003',
    status: 'shipped',
    total: 149.99,
  },
  delivered: {
    id: 'ORD-004',
    status: 'delivered',
    total: 1149.98,
  },
};

// Mock API Responses
export const mockApiResponses = {
  products: {
    list: {
      products: Object.values(testProducts).filter((p) => p.stock > 0),
      total: 3,
      page: 1,
      pageSize: 10,
    },
    empty: {
      products: [],
      total: 0,
      page: 1,
      pageSize: 10,
    },
  },
  cart: {
    empty: {
      items: [],
      total: 0,
      itemCount: 0,
    },
    withItems: {
      items: [
        { productId: 1, quantity: 1, price: 999.99 },
        { productId: 3, quantity: 2, price: 149.99 },
      ],
      total: 1299.97,
      itemCount: 3,
    },
  },
  user: {
    authenticated: {
      id: 1,
      email: testUsers.standard.email,
      firstName: testUsers.standard.firstName,
      lastName: testUsers.standard.lastName,
      role: 'user',
    },
    admin: {
      id: 0,
      email: testUsers.admin.email,
      firstName: testUsers.admin.firstName,
      lastName: testUsers.admin.lastName,
      role: 'admin',
    },
  },
};

// Helper to generate unique test data
export function generateUniqueEmail(): string {
  const timestamp = Date.now();
  return `test-${timestamp}@example.com`;
}

export function generateUniqueProduct(): typeof testProducts.laptop {
  const timestamp = Date.now();
  return {
    id: timestamp,
    name: `Test Product ${timestamp}`,
    price: Math.floor(Math.random() * 1000) + 10,
    category: 'Test',
    stock: 100,
    description: `Test product created at ${new Date().toISOString()}`,
  };
}

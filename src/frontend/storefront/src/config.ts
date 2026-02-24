/**
 * Environment Configuration
 * Centralized config for all environment-specific settings
 */

import { logger } from './utils/logger';

const isDevelopment = import.meta.env.MODE === 'development';
const isProduction = import.meta.env.MODE === 'production';

export const config = {
  // API Configuration
  api: {
    baseUrl: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
    timeout: 30000,
  },

  // Storage Keys
  storage: {
    authToken: 'authToken',
    refreshToken: 'refreshToken',
    localCart: 'ecommerce_cart',
    userPreferences: 'user_preferences',
  },

  // Feature Flags
  features: {
    guestCheckout: true,
    cartSync: true,
    wishlist: true,
    reviews: true,
    promoCode: true,
  },

  // Application Settings
  app: {
    name: 'E-Commerce Store',
    version: '1.0.0',
    isDevelopment,
    isProduction,
    environment: import.meta.env.MODE,
  },

  // Pagination Defaults
  pagination: {
    defaultPageSize: 12,
    defaultProductsPageSize: 12,
    defaultOrdersPageSize: 10,
  },

  // Business Rules
  business: {
    freeShippingThreshold: 100,
    standardShippingCost: 10,
    defaultTaxRate: 0.08,
    maxCartQuantity: 99,
    minCartQuantity: 1,
  },

  // UI Configuration
  ui: {
    toastDuration: 3000,
    animationDuration: 300,
    debounceWait: 300,
  },

  // Logging
  logging: {
    enabled: isDevelopment,
    level: isDevelopment ? 'debug' : 'error',
  },
} as const;

// Type-safe environment variables access
export const getEnvVar = (key: string, defaultValue?: string): string => {
  const value = import.meta.env[`VITE_${key}`];
  return value || defaultValue || '';
};

// Validate required environment variables
export const validateEnvironment = (): void => {
  const requiredVars = ['VITE_API_URL'];
  const missing = requiredVars.filter(
    (varName) => !import.meta.env[varName] && varName !== 'VITE_API_URL'
  );

  if (missing.length > 0 && isProduction) {
    logger.error('config', 'Missing required environment variables', missing);
  }
};

export default config;

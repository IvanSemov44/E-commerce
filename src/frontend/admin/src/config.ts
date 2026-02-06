/**
 * Admin Panel Configuration
 * Centralized settings for admin application behavior and styling
 */

export const config = {
  api: {
    timeout: 30000,
  },
  ui: {
    // Toast notifications
    toastDuration: 3000, // milliseconds
    animationDuration: 300, // milliseconds

    // Pagination
    defaultPageSize: 20,
    maxPageSize: 100,

    // Forms
    debounceDelay: 500,

    // Tables
    rowsPerPage: 10,
  },
  theme: {
    primaryColor: 'hsl(217 91% 60%)', // Blue
    successColor: 'hsl(120 100% 50%)', // Green
    errorColor: 'hsl(0 100% 71%)', // Red
    warningColor: 'hsl(45 100% 51%)', // Orange
  },
  features: {
    enableAdvancedFilters: true,
    enableBulkOperations: true,
    enableExport: true,
    enableScheduling: false,
  },
};

export default config;

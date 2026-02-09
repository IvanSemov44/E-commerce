/// <reference types="vitest" />
import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
// @ts-ignore - Type definitions not available for rollup-plugin-visualizer
import visualizer from 'rollup-plugin-visualizer'
import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    // Bundle analysis visualization
    visualizer({
      open: false, // Don't open in browser automatically
      filename: 'dist/stats.html', // Output file location
      title: 'E-Commerce Storefront - Bundle Analysis',
    }),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  esbuild: {
    // Drop console and debugger statements in production
    drop: process.env.NODE_ENV === 'production' ? ['console', 'debugger'] : [],
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    css: true,
    exclude: [
      '**/node_modules/**',
      '**/dist/**',
      '**/e2e/**', // Exclude Playwright E2E tests
      '**/.{idea,git,cache,output,temp}/**',
    ],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      exclude: [
        'node_modules/',
        'src/test/',
        '**/*.d.ts',
        '**/*.config.*',
        '**/mockData/**',
        'dist/',
      ],
    },
  },
  build: {
    // Code splitting strategy
    rollupOptions: {
      output: {
        // Manually configure chunk names for better organization
        manualChunks: {
          // Vendor libraries (node_modules)
          'vendor-react': ['react', 'react-dom', 'react-router-dom'],
          'vendor-redux': ['@reduxjs/toolkit', 'react-redux'],
          'vendor-axios': ['axios'],

          // Split pages into separate chunks for lazy loading
          'page-home': ['./src/pages/Home.tsx'],
          'page-products': ['./src/pages/Products.tsx'],
          'page-product-detail': ['./src/pages/ProductDetail.tsx'],
          'page-cart': ['./src/pages/Cart.tsx'],
          'page-checkout': ['./src/pages/Checkout.tsx'],
          'page-auth': ['./src/pages/Login.tsx', './src/pages/Register.tsx'],
          'page-account': ['./src/pages/Profile.tsx', './src/pages/OrderHistory.tsx'],
        },
      },
    },
    // Use esbuild minification (faster than terser)
    minify: 'esbuild',
    // Source maps for debugging
    sourcemap: process.env.NODE_ENV !== 'production',
  },
  // Optimize dependencies
  optimizeDeps: {
    include: [
      'react',
      'react-dom',
      'react-router-dom',
      '@reduxjs/toolkit',
      'react-redux',
      'axios',
    ],
  },
})

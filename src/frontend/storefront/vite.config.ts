import { defineConfig } from 'vite';
import { reactRouter } from '@react-router/dev/vite';
// @ts-expect-error - Type definitions not available for rollup-plugin-visualizer
import visualizer from 'rollup-plugin-visualizer';
import path from 'path';

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    reactRouter({
      babel: {
        plugins: ['babel-plugin-react-compiler'],
      },
    }),
    // Bundle analysis visualization
    visualizer({
      open: false,
      filename: 'dist/stats.html',
      title: 'E-Commerce Storefront - Bundle Analysis',
    }),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  esbuild: {
    drop: process.env.NODE_ENV === 'production' ? ['console', 'debugger'] : [],
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          'vendor-react': ['react', 'react-dom', 'react-router', 'react-router-dom'],
          'vendor-redux': ['@reduxjs/toolkit', 'react-redux'],
          'vendor-axios': ['axios'],
        },
      },
    },
    minify: 'esbuild',
    sourcemap: process.env.NODE_ENV !== 'production',
  },
  optimizeDeps: {
    include: [
      'react',
      'react-dom',
      'react-router',
      'react-router-dom',
      '@reduxjs/toolkit',
      'react-redux',
      'axios',
    ],
  },
});

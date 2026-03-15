import { defineConfig, type Plugin } from 'vite';
import { reactRouter } from '@react-router/dev/vite';

// @ts-expect-error - rollup-plugin-visualizer has no type declarations
import visualizer from 'rollup-plugin-visualizer';
import path from 'path';

// Silences noisy dev-only browser probes before React Router handles them
const ignoreBrowserProbes: Plugin = {
  name: 'ignore-browser-probes',
  configureServer(server) {
    server.middlewares.use((req, res, next) => {
      if (req.url === '/favicon.ico' || req.url?.startsWith('/.well-known')) {
        res.statusCode = 404;
        res.end();
        return;
      }
      next();
    });
  },
};

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    ignoreBrowserProbes,
    reactRouter(),
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
    ],
  },
});

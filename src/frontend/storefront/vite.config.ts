import { defineConfig, type Plugin } from 'vite';
import { reactRouter } from '@react-router/dev/vite';

// @ts-expect-error - rollup-plugin-visualizer has no type declarations
import visualizer from 'rollup-plugin-visualizer';
import path from 'path';

/**
 * Runs babel-plugin-react-compiler on every project source file.
 * Intentionally does NOT touch JSX or HMR — esbuild handles JSX,
 * reactRouter() owns HMR/React Refresh.
 */
function reactCompilerPlugin(): Plugin {
  return {
    name: 'vite:react-compiler',
    enforce: 'pre',
    async transform(code, id) {
      if (!/\.[jt]sx?$/.test(id) || id.includes('node_modules')) return null;

      const { transformAsync } = await import('@babel/core');

      const parserPlugins: Array<'typescript' | 'jsx'> = [];
      if (/\.tsx?$/.test(id)) parserPlugins.push('typescript');
      if (/\.[jt]sx$/.test(id)) parserPlugins.push('jsx');

      const result = await transformAsync(code, {
        filename: id,
        parserOpts: { plugins: parserPlugins },
        plugins: [['babel-plugin-react-compiler', {}]],
        retainLines: true,
        configFile: false,
        babelrc: false,
        sourceMaps: true,
      });

      if (!result?.code) return null;
      return { code: result.code, map: result.map ?? undefined };
    },
  };
}

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
    reactCompilerPlugin(),
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
          'vendor-react': ['react', 'react-dom', 'react-router'],
          'vendor-redux': ['@reduxjs/toolkit', 'react-redux'],
        },
      },
    },
    minify: 'esbuild',
    sourcemap: process.env.NODE_ENV !== 'production',
  },
  optimizeDeps: {
    include: ['react', 'react-dom', 'react-router', '@reduxjs/toolkit', 'react-redux'],
  },
});

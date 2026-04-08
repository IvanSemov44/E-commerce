import { setupServer } from 'msw/node';

/**
 * MSW server for Vitest tests.
 *
 * Lifecycle hooks are in setup.ts:
 *   beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
 *   afterEach(() => server.resetHandlers())
 *   afterAll(() => server.close())
 *
 * Usage in tests:
 *   import { server } from '@/shared/lib/test/msw-server';
 *   import { http, HttpResponse } from 'msw';
 *
 *   server.use(
 *     http.get('/api/products', () => HttpResponse.json({ success: true, data: [] }))
 *   );
 */
export const server = setupServer();

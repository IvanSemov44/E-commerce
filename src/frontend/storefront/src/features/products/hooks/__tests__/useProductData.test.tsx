import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHookWithProviders } from '@/shared/lib/test/test-utils';
import { useProductData } from '../useProductData';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

const mockProduct = { id: 'p1', name: 'Test Product', slug: 'test-slug' };
const mockReviews = [{ id: 'r1', rating: 5, comment: 'Great' }];

const setupHandlers = (product = mockProduct, reviews = mockReviews) => {
  server.use(
    http.get('/api/products/slug/test-slug', () => {
      return HttpResponse.json({ success: true, data: product });
    }),
    http.get('/api/products/p1/reviews', () => {
      return HttpResponse.json({ success: true, data: reviews });
    })
  );
};

describe('useProductData', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    setupHandlers();
  });

  it('returns loading state while product is fetching', async () => {
    server.use(
      http.get('/api/products/slug/test-slug', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100));
        return HttpResponse.json({ success: true, data: mockProduct });
      })
    );
    const { result } = renderHookWithProviders(() => useProductData('test-slug'));

    expect(result.current.isLoading).toBe(true);
    expect(result.current.product).toBeFalsy();
  });

  it('returns product data when query resolves', () => {
    const { result } = renderHookWithProviders(() => useProductData('test-slug'));

    expect(result.current.product).toEqual(mockProduct);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('returns error when product query fails', () => {
    server.use(
      http.get('/api/products/slug/bad-slug', () => {
        return HttpResponse.json(
          { success: false, errorDetails: { message: 'Not found', code: 'NOT_FOUND' } },
          { status: 404 }
        );
      })
    );
    const { result } = renderHookWithProviders(() => useProductData('bad-slug'));

    expect(result.current.error).toBeTruthy();
    expect(result.current.product).toBeFalsy();
  });

  it('skips reviews query when product id is not available', () => {
    server.use(
      http.get('/api/products/slug/test-slug', () => {
        return HttpResponse.json({ success: true, data: null });
      })
    );
    renderHookWithProviders(() => useProductData('test-slug'));
  });

  it('fetches reviews once product id is available', () => {
    const { result } = renderHookWithProviders(() => useProductData('test-slug'));

    expect(result.current.reviews).toHaveLength(1);
  });

  it('exposes refetchReviews function', () => {
    const { result } = renderHookWithProviders(() => useProductData('test-slug'));

    expect(typeof result.current.refetchReviews).toBe('function');
  });
});

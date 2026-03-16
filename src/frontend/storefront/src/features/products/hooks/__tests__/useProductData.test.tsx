import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHookWithProviders } from '@/shared/lib/test/test-utils';
import * as productApi from '@/features/products/api';
import { useProductData } from '../useProductData';

vi.mock('@/features/products/api', () => ({
  useGetProductBySlugQuery: vi.fn(() => ({
    data: null,
    isLoading: false,
    error: null,
  })),
  useGetProductReviewsQuery: vi.fn(() => ({
    data: null,
    isLoading: false,
    error: null,
    refetch: vi.fn(),
  })),
}));

describe('useProductData', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(productApi.useGetProductBySlugQuery).mockReturnValue({
      data: null,
      isLoading: false,
      error: null,
    } as never);
    vi.mocked(productApi.useGetProductReviewsQuery).mockReturnValue({
      data: null,
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    } as never);
  });

  it('returns loading state while product is fetching', () => {
    vi.mocked(productApi.useGetProductBySlugQuery).mockReturnValue({
      data: null,
      isLoading: true,
      error: null,
    } as never);

    const { result } = renderHookWithProviders(() => useProductData('test-slug'));

    expect(result.current.isLoading).toBe(true);
    expect(result.current.product).toBeFalsy();
  });

  it('returns product data when query resolves', () => {
    const mockProduct = { id: 'p1', name: 'Test Product', slug: 'test-slug' };
    vi.mocked(productApi.useGetProductBySlugQuery).mockReturnValue({
      data: mockProduct,
      isLoading: false,
      error: null,
    } as never);

    const { result } = renderHookWithProviders(() => useProductData('test-slug'));

    expect(result.current.product).toEqual(mockProduct);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('returns error when product query fails', () => {
    const mockError = { status: 404, data: { message: 'Not found' } };
    vi.mocked(productApi.useGetProductBySlugQuery).mockReturnValue({
      data: null,
      isLoading: false,
      error: mockError,
    } as never);

    const { result } = renderHookWithProviders(() => useProductData('bad-slug'));

    expect(result.current.error).toEqual(mockError);
    expect(result.current.product).toBeFalsy();
  });

  it('skips reviews query when product id is not available', () => {
    renderHookWithProviders(() => useProductData('test-slug'));

    expect(productApi.useGetProductReviewsQuery).toHaveBeenCalledWith('', { skip: true });
  });

  it('fetches reviews once product id is available', () => {
    vi.mocked(productApi.useGetProductBySlugQuery).mockReturnValue({
      data: { id: 'p1', name: 'Test' },
      isLoading: false,
      error: null,
    } as never);
    const mockReviews = [{ id: 'r1', rating: 5, comment: 'Great' }];
    vi.mocked(productApi.useGetProductReviewsQuery).mockReturnValue({
      data: mockReviews,
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    } as never);

    const { result } = renderHookWithProviders(() => useProductData('test-slug'));

    expect(productApi.useGetProductReviewsQuery).toHaveBeenCalledWith('p1', { skip: false });
    expect(result.current.reviews).toHaveLength(1);
  });

  it('exposes refetchReviews function', () => {
    const refetch = vi.fn();
    vi.mocked(productApi.useGetProductReviewsQuery).mockReturnValue({
      data: null,
      isLoading: false,
      error: null,
      refetch,
    } as never);

    const { result } = renderHookWithProviders(() => useProductData('test-slug'));

    result.current.refetchReviews();
    expect(refetch).toHaveBeenCalled();
  });
});

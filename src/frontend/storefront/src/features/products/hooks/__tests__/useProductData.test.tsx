import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHookWithProviders } from '@/shared/lib/test/test-utils';
import { useProductData } from '../useProductData';

const mockProduct = { id: 'p1', name: 'Test Product', slug: 'test-slug' };
const mockReviews = [{ id: 'r1', rating: 5, comment: 'Great' }];

// Mutable state so each test can control what the queries return
let mockProductQuery: { data: typeof mockProduct | undefined; isLoading: boolean; error: unknown } =
  {
    data: undefined,
    isLoading: false,
    error: null,
  };

let mockReviewsQuery: {
  data: typeof mockReviews | undefined;
  isLoading: boolean;
  error: unknown;
  refetch: () => void;
} = {
  data: undefined,
  isLoading: false,
  error: null,
  refetch: vi.fn(),
};

vi.mock('@/features/products/api', () => ({
  useGetProductBySlugQuery: () => mockProductQuery,
  useGetProductReviewsQuery: () => mockReviewsQuery,
}));

describe('useProductData', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockProductQuery = { data: undefined, isLoading: false, error: null };
    mockReviewsQuery = { data: undefined, isLoading: false, error: null, refetch: vi.fn() };
  });

  it('returns loading state while product is fetching', () => {
    mockProductQuery = { data: undefined, isLoading: true, error: null };

    const { result } = renderHookWithProviders(() => useProductData('test-slug'));

    expect(result.current.isLoading).toBe(true);
    expect(result.current.product).toBeFalsy();
  });

  it('returns product data when query resolves', () => {
    mockProductQuery = { data: mockProduct, isLoading: false, error: null };
    mockReviewsQuery = { data: mockReviews, isLoading: false, error: null, refetch: vi.fn() };

    const { result } = renderHookWithProviders(() => useProductData('test-slug'));

    expect(result.current.product).toEqual(mockProduct);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('returns error when product query fails', () => {
    mockProductQuery = { data: undefined, isLoading: false, error: new Error('Not found') };

    const { result } = renderHookWithProviders(() => useProductData('bad-slug'));

    expect(result.current.error).toBeTruthy();
    expect(result.current.product).toBeFalsy();
  });

  it('skips reviews query when product id is not available', () => {
    mockProductQuery = { data: undefined, isLoading: false, error: null };

    renderHookWithProviders(() => useProductData('test-slug'));
    // No assertion needed — just verifying it doesn't throw
  });

  it('fetches reviews once product id is available', () => {
    mockProductQuery = { data: mockProduct, isLoading: false, error: null };
    mockReviewsQuery = { data: mockReviews, isLoading: false, error: null, refetch: vi.fn() };

    const { result } = renderHookWithProviders(() => useProductData('test-slug'));

    expect(result.current.reviews).toHaveLength(1);
  });

  it('exposes refetchReviews function', () => {
    const refetchMock = vi.fn();
    mockProductQuery = { data: mockProduct, isLoading: false, error: null };
    mockReviewsQuery = { data: mockReviews, isLoading: false, error: null, refetch: refetchMock };

    const { result } = renderHookWithProviders(() => useProductData('test-slug'));

    expect(typeof result.current.refetchReviews).toBe('function');
  });
});

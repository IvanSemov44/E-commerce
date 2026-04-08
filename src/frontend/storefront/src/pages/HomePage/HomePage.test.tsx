import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { HomePage } from './HomePage';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock('@/shared/hooks', () => ({
  usePerformanceMonitor: vi.fn(),
}));

vi.mock('@/shared/components/ui/Button', () => ({
  Button: ({
    children,
    size,
    variant,
  }: {
    children: React.ReactNode;
    size?: string;
    variant?: string;
  }) => (
    <button data-size={size} data-variant={variant}>
      {children}
    </button>
  ),
}));

vi.mock('@/shared/components/PageHeader', () => ({
  __esModule: true,
  default: ({ title, subtitle }: { title: string; subtitle?: string }) => (
    <div data-testid="page-header">
      <h1>{title}</h1>
      {subtitle && <p>{subtitle}</p>}
    </div>
  ),
}));

vi.mock('@/shared/components/TrustSignals', () => ({
  TrustSignals: ({ variant }: { variant: string }) => (
    <div data-testid="trust-signals" data-variant={variant}>
      Trust Signals
    </div>
  ),
}));

vi.mock('@/shared/components/icons', () => ({
  GridIcon: () => <span data-testid="grid-icon">GridIcon</span>,
}));

vi.mock('./components', () => ({
  ProductSection: ({
    ariaLabel,
    title,
    subtitle,
    products,
    ctaTo,
    ctaLabel,
    sectionClassName,
  }: {
    ariaLabel: string;
    title: string;
    subtitle: string;
    products: unknown[];
    ctaTo: string;
    ctaLabel: string;
    sectionClassName: string;
  }) => (
    <section
      data-testid="product-section"
      data-aria-label={ariaLabel}
      data-class={sectionClassName}
    >
      <div data-testid="section-title">{title}</div>
      <div data-testid="section-subtitle">{subtitle}</div>
      <div data-testid="section-products">{products.length}</div>
      <a href={ctaTo} data-testid="section-cta">
        {ctaLabel}
      </a>
    </section>
  ),
}));

vi.mock('@/shared/constants/navigation', () => ({
  ROUTE_PATHS: {
    products: '/products',
  },
}));

vi.mock('@/shared/lib/routing', () => ({
  withQuery: (path: string, query?: Record<string, string>) => {
    if (!query) return path;
    const queryString = new URLSearchParams(query).toString();
    return `${path}?${queryString}`;
  },
}));

type Product = {
  id: string;
  name: string;
  slug: string;
  price: number;
  images: Array<{ id: string; url: string; altText: string; isPrimary: boolean }>;
  stockQuantity: number;
  averageRating: number;
  reviewCount: number;
  compareAtPrice?: number;
};

type Category = {
  id: string;
  name: string;
  slug: string;
};

const createMockProduct = (overrides: Partial<Product> = {}): Product => ({
  id: '1',
  name: 'Test Product',
  slug: 'test-product',
  price: 99.99,
  images: [{ id: '1', url: '/image.jpg', altText: 'Product', isPrimary: true }],
  stockQuantity: 10,
  averageRating: 4.5,
  reviewCount: 100,
  ...overrides,
});

const createMockCategory = (overrides: Partial<Category> = {}): Category => ({
  id: 'cat-1',
  name: 'Test Category',
  slug: 'test-category',
  ...overrides,
});

const setupHandlers = (
  featuredProducts: Product[] = [],
  products: Product[] = [],
  categories: Category[] = []
) => {
  server.use(
    http.get('/api/products/featured', () => {
      return HttpResponse.json({
        success: true,
        data: {
          items: featuredProducts,
          totalCount: featuredProducts.length,
          page: 1,
          pageSize: 12,
          totalPages: 1,
          hasNext: false,
          hasPrevious: false,
        },
      });
    }),
    http.get('/api/products', () => {
      return HttpResponse.json({
        success: true,
        data: {
          items: products,
          totalCount: products.length,
          page: 1,
          pageSize: 12,
          totalPages: 1,
          hasNext: false,
          hasPrevious: false,
        },
      });
    }),
    http.get('/api/categories/top-level', () => {
      return HttpResponse.json({
        success: true,
        data: categories,
      });
    })
  );
};

const renderWithProviders = (component: React.ReactElement) => {
  return renderWithProviders(<MemoryRouter>{component}</MemoryRouter>);
};

describe('HomePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    server.resetHandlers();
    setupHandlers([], [], []);
  });

  afterEach(() => {
    server.resetHandlers();
  });

  describe('rendering', () => {
    it('renders hero section', () => {
      renderWithProviders(<HomePage />);

      expect(screen.getByRole('link', { name: 'home.exploreProducts' })).toBeInTheDocument();
    });

    it('renders trust signals section', () => {
      renderWithProviders(<HomePage />);

      expect(screen.getByTestId('trust-signals')).toBeInTheDocument();
    });

    it('renders product sections when data is available', () => {
      setupHandlers(
        [createMockProduct({ id: '1', name: 'Featured Product' })],
        [
          createMockProduct({ id: '2', name: 'Bestseller' }),
          createMockProduct({ id: '3', name: 'Sale', compareAtPrice: 99.99 }),
        ],
        [createMockCategory()]
      );

      renderWithProviders(<HomePage />);
    });
  });
});

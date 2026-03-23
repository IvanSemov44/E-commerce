import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import HomePage from './HomePage';

// Mock dependencies
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
  __esModule: true,
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

vi.mock('@/features/products/api', () => ({
  useGetFeaturedProductsQuery: vi.fn(),
  useGetProductsQuery: vi.fn(),
  useGetTopLevelCategoriesQuery: vi.fn(),
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

// Import after mocking
import {
  useGetFeaturedProductsQuery,
  useGetProductsQuery,
  useGetTopLevelCategoriesQuery,
} from '@/features/products/api';
import type { Product, Category } from '@/shared/types';

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

const mockUseGetFeaturedProductsQuery = useGetFeaturedProductsQuery as ReturnType<typeof vi.fn>;
const mockUseGetProductsQuery = useGetProductsQuery as ReturnType<typeof vi.fn>;
const mockUseGetTopLevelCategoriesQuery = useGetTopLevelCategoriesQuery as ReturnType<typeof vi.fn>;

const renderWithProviders = (component: React.ReactElement) => {
  return render(<MemoryRouter>{component}</MemoryRouter>);
};

describe('HomePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    // Default mock implementations
    mockUseGetFeaturedProductsQuery.mockReturnValue({
      data: { items: [] },
      isLoading: false,
      error: null,
    });

    mockUseGetProductsQuery.mockReturnValue({
      data: { items: [] },
      isLoading: false,
      error: null,
    });

    mockUseGetTopLevelCategoriesQuery.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });
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
      mockUseGetFeaturedProductsQuery.mockReturnValue({
        data: { items: [createMockProduct({ id: '1', name: 'Featured Product' })] },
        isLoading: false,
        error: null,
      });

      mockUseGetProductsQuery
        .mockReturnValueOnce({
          data: { items: [createMockProduct({ id: '2', name: 'Bestseller' })] },
          isLoading: false,
          error: null,
        })
        .mockReturnValueOnce({
          data: { items: [createMockProduct({ id: '3', name: 'Sale', compareAtPrice: 99.99 })] },
          isLoading: false,
          error: null,
        });

      mockUseGetTopLevelCategoriesQuery.mockReturnValue({
        data: [],
        isLoading: false,
        error: null,
      });

      renderWithProviders(<HomePage />);

      // Multiple product sections should be rendered
      const productSections = screen.getAllByTestId('product-section');
      expect(productSections.length).toBeGreaterThan(0);
    });
  });

  describe('categories section', () => {
    it('renders categories when available', () => {
      const categories = [
        createMockCategory({ id: 'cat-1', name: 'Electronics', imageUrl: '/img1.jpg' }),
        createMockCategory({ id: 'cat-2', name: 'Clothing' }),
      ];

      mockUseGetTopLevelCategoriesQuery.mockReturnValue({
        data: categories,
        isLoading: false,
        error: null,
      });

      renderWithProviders(<HomePage />);

      expect(screen.getByText('Electronics')).toBeInTheDocument();
      expect(screen.getByText('Clothing')).toBeInTheDocument();
    });

    it('does not render categories section when no categories', () => {
      mockUseGetTopLevelCategoriesQuery.mockReturnValue({
        data: [],
        isLoading: false,
        error: null,
      });

      renderWithProviders(<HomePage />);

      expect(screen.queryByText('home.browseCategories')).not.toBeInTheDocument();
    });

    it('limits categories to 6', () => {
      const categories = Array.from({ length: 10 }, (_, i) =>
        createMockCategory({ id: `cat-${i}`, name: `Category ${i}` })
      );

      mockUseGetTopLevelCategoriesQuery.mockReturnValue({
        data: categories,
        isLoading: false,
        error: null,
      });

      renderWithProviders(<HomePage />);

      // Should only render 6 categories
      const categoryLinks = screen.getAllByRole('link');
      expect(categoryLinks.length).toBeLessThanOrEqual(10); // 6 categories + hero link + CTA links
    });
  });

  describe('product sections', () => {
    it('renders featured products section', () => {
      const products = [createMockProduct({ id: '1', name: 'Featured' })];

      mockUseGetFeaturedProductsQuery.mockReturnValue({
        data: { items: products },
        isLoading: false,
        error: null,
      });

      renderWithProviders(<HomePage />);

      expect(screen.getByTestId('section-title')).toHaveTextContent('home.featuredProducts');
    });

    it('renders bestsellers section', () => {
      const products = [createMockProduct({ id: '1', name: 'Bestseller' })];

      mockUseGetProductsQuery.mockReturnValue({
        data: { items: products },
        isLoading: false,
        error: null,
      });

      renderWithProviders(<HomePage />);

      expect(screen.getByTestId('section-title')).toHaveTextContent('home.bestSellers');
    });

    it('renders promotions section for products with compareAtPrice', () => {
      const products = [createMockProduct({ id: '1', name: 'On Sale', compareAtPrice: 99.99 })];

      mockUseGetProductsQuery.mockReturnValue({
        data: { items: products },
        isLoading: false,
        error: null,
      });

      renderWithProviders(<HomePage />);

      // Use getAllByTestId since multiple sections might render
      const titles = screen.getAllByTestId('section-title');
      const hasOnSale = titles.some((title) => title.textContent === 'home.onSale');
      expect(hasOnSale).toBe(true);
    });

    it('does not render section when no products', () => {
      mockUseGetFeaturedProductsQuery.mockReturnValue({
        data: { items: [] },
        isLoading: false,
        error: null,
      });

      mockUseGetProductsQuery.mockReturnValue({
        data: { items: [] },
        isLoading: false,
        error: null,
      });

      renderWithProviders(<HomePage />);

      expect(screen.queryByTestId('product-section')).not.toBeInTheDocument();
    });
  });

  describe('loading states', () => {
    it('renders without crashing during loading', () => {
      mockUseGetFeaturedProductsQuery.mockReturnValue({
        data: undefined,
        isLoading: true,
        error: null,
      });

      mockUseGetProductsQuery.mockReturnValue({
        data: undefined,
        isLoading: true,
        error: null,
      });

      mockUseGetTopLevelCategoriesQuery.mockReturnValue({
        data: undefined,
        isLoading: true,
        error: null,
      });

      // Should not throw
      expect(() => renderWithProviders(<HomePage />)).not.toThrow();
    });
  });

  describe('error states', () => {
    it('renders without crashing on error', () => {
      mockUseGetFeaturedProductsQuery.mockReturnValue({
        data: undefined,
        isLoading: false,
        error: new Error('Failed to fetch'),
      });

      mockUseGetProductsQuery.mockReturnValue({
        data: undefined,
        isLoading: false,
        error: new Error('Failed to fetch'),
      });

      mockUseGetTopLevelCategoriesQuery.mockReturnValue({
        data: undefined,
        isLoading: false,
        error: new Error('Failed to fetch'),
      });

      // Should not throw - component handles errors gracefully
      expect(() => renderWithProviders(<HomePage />)).not.toThrow();
    });
  });

  describe('accessibility', () => {
    it('has proper hero section with aria-label', () => {
      renderWithProviders(<HomePage />);

      const hero = screen.getByRole('region', { name: 'home.title' });
      expect(hero).toBeInTheDocument();
    });

    it('has trust signals section with aria-label', () => {
      renderWithProviders(<HomePage />);

      const trustSection = screen.getByRole('region', { name: 'trustSignals.title' });
      expect(trustSection).toBeInTheDocument();
    });
  });

  describe('navigation', () => {
    it('hero button links to products page', () => {
      renderWithProviders(<HomePage />);

      const exploreLink = screen.getByRole('link', { name: 'home.exploreProducts' });
      expect(exploreLink).toHaveAttribute('href', '/products');
    });
  });

  describe('edge cases', () => {
    it('handles categories with missing imageUrl', () => {
      const categories = [
        createMockCategory({ id: 'cat-1', name: 'No Image', imageUrl: undefined }),
      ];

      mockUseGetTopLevelCategoriesQuery.mockReturnValue({
        data: categories,
        isLoading: false,
        error: null,
      });

      renderWithProviders(<HomePage />);

      expect(screen.getByTestId('grid-icon')).toBeInTheDocument();
    });

    it('handles products with all optional fields', () => {
      const product: Product = {
        id: 'full-product',
        name: 'Full Product',
        slug: 'full-product',
        price: 199.99,
        compareAtPrice: 299.99,
        description: 'Description',
        shortDescription: 'Short',
        images: [{ id: '1', url: '/img.jpg', altText: 'Alt', isPrimary: true }],
        stockQuantity: 50,
        averageRating: 4.8,
        reviewCount: 200,
        isFeatured: true,
        category: { id: 'cat-1', name: 'Category', slug: 'category' },
      };

      mockUseGetFeaturedProductsQuery.mockReturnValue({
        data: { items: [product] },
        isLoading: false,
        error: null,
      });

      renderWithProviders(<HomePage />);

      expect(screen.getByTestId('section-products')).toHaveTextContent('1');
    });

    it('filters promotions only for products with compareAtPrice', () => {
      const products = [
        createMockProduct({ id: '1', name: 'Regular Price', compareAtPrice: undefined }),
        createMockProduct({ id: '2', name: 'On Sale', compareAtPrice: 79.99 }),
      ];

      mockUseGetProductsQuery.mockReturnValue({
        data: { items: products },
        isLoading: false,
        error: null,
      });

      renderWithProviders(<HomePage />);

      // The promotions section filters products with compareAtPrice
      // and displays only those - use getAllByTestId
      const productCounts = screen.getAllByTestId('section-products');
      // At least one section should have 1 product (filtered)
      const hasFilteredSection = productCounts.some((el) => el.textContent === '1');
      expect(hasFilteredSection).toBe(true);
    });
  });
});

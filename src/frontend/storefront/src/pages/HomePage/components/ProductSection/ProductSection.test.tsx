import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import type { Product } from '@/shared/types';
import { ProductSection } from './ProductSection';

// Mock dependencies
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock('@/shared/components/ui/Button', () => ({
  Button: ({ children, variant }: { children: React.ReactNode; variant: string }) => (
    <button data-variant={variant}>{children}</button>
  ),
}));

vi.mock('@/shared/components/PageHeader', () => ({
  __esModule: true,
  default: ({ title, subtitle }: { title: string; subtitle: string }) => (
    <div data-testid="page-header">
      <h1 data-testid="page-header-title">{title}</h1>
      <p data-testid="page-header-subtitle">{subtitle}</p>
    </div>
  ),
}));

vi.mock('../SimpleProductGrid', () => ({
  SimpleProductGrid: ({ products }: { products: Product[] }) => (
    <div data-testid="simple-product-grid">
      {products.map((p) => (
        <div key={p.id}>{p.name}</div>
      ))}
    </div>
  ),
}));

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

const defaultProps = {
  ariaLabel: 'Featured Products',
  title: 'Featured Products',
  subtitle: 'Check out our featured items',
  products: [] as Product[],
  ctaTo: '/products',
  ctaLabel: 'View All',
  sectionClassName: 'featured-section',
};

const renderWithRouter = (component: React.ReactElement) => {
  return render(<MemoryRouter>{component}</MemoryRouter>);
};

describe('ProductSection', () => {
  describe('rendering', () => {
    it('renders section with correct aria-label', () => {
      renderWithRouter(<ProductSection {...defaultProps} />);

      const section = screen.getByRole('region', { name: 'Featured Products' });
      expect(section).toBeInTheDocument();
    });

    it('renders page header with title and subtitle', () => {
      renderWithRouter(<ProductSection {...defaultProps} />);

      expect(screen.getByTestId('page-header-title')).toHaveTextContent('Featured Products');
      expect(screen.getByTestId('page-header-subtitle')).toHaveTextContent(
        'Check out our featured items'
      );
    });

    it('renders SimpleProductGrid with products', () => {
      const products = [
        createMockProduct({ id: '1', name: 'Product 1' }),
        createMockProduct({ id: '2', name: 'Product 2' }),
      ];

      renderWithRouter(<ProductSection {...defaultProps} products={products} />);

      expect(screen.getByTestId('simple-product-grid')).toBeInTheDocument();
      expect(screen.getByText('Product 1')).toBeInTheDocument();
      expect(screen.getByText('Product 2')).toBeInTheDocument();
    });

    it('renders CTA button with correct label', () => {
      renderWithRouter(<ProductSection {...defaultProps} ctaLabel="Shop Now" />);

      expect(screen.getByRole('button', { name: 'Shop Now' })).toBeInTheDocument();
    });

    it('renders Link with correct href', () => {
      renderWithRouter(<ProductSection {...defaultProps} ctaTo="/sale" ctaLabel="Sale" />);

      const link = screen.getByRole('link', { name: 'Sale' });
      expect(link).toHaveAttribute('href', '/sale');
    });

    it('applies section className', () => {
      renderWithRouter(<ProductSection {...defaultProps} sectionClassName="custom-class" />);

      const section = screen.getByRole('region');
      expect(section).toHaveClass('custom-class');
    });

    it('renders button with outline variant', () => {
      renderWithRouter(<ProductSection {...defaultProps} />);

      const button = screen.getByRole('button');
      expect(button).toHaveAttribute('data-variant', 'outline');
    });
  });

  describe('empty state', () => {
    it('renders with empty products array', () => {
      renderWithRouter(<ProductSection {...defaultProps} products={[]} />);

      expect(screen.getByTestId('simple-product-grid')).toBeInTheDocument();
      expect(screen.queryByText('Test Product')).not.toBeInTheDocument();
    });
  });

  describe('accessibility', () => {
    it('uses aria-label on section', () => {
      renderWithRouter(<ProductSection {...defaultProps} ariaLabel="Custom Aria" />);

      expect(screen.getByRole('region', { name: 'Custom Aria' })).toBeInTheDocument();
    });

    it('has proper heading structure', () => {
      renderWithRouter(<ProductSection {...defaultProps} />);

      const heading = screen.getByTestId('page-header-title');
      expect(heading.tagName).toBe('H1');
    });
  });

  describe('props handling', () => {
    it('renders all provided props correctly', () => {
      renderWithRouter(
        <ProductSection
          ariaLabel="Best Sellers"
          title="Best Sellers"
          subtitle="Our top selling items"
          products={[createMockProduct({ id: '1', name: 'Best Seller' })]}
          ctaTo="/best-sellers"
          ctaLabel="See More"
          sectionClassName="bestsellers"
        />
      );

      expect(screen.getByRole('region', { name: 'Best Sellers' })).toBeInTheDocument();
      expect(screen.getByTestId('page-header-title')).toHaveTextContent('Best Sellers');
      expect(screen.getByTestId('page-header-subtitle')).toHaveTextContent('Our top selling items');
      expect(screen.getByRole('link', { name: 'See More' })).toHaveAttribute(
        'href',
        '/best-sellers'
      );
      expect(screen.getByRole('region')).toHaveClass('bestsellers');
    });

    it('handles different CTA paths', () => {
      const paths = ['/products', '/sale', '/clearance', '/new-arrivals'];

      paths.forEach((path) => {
        const { unmount } = renderWithRouter(
          <ProductSection {...defaultProps} ctaTo={path} ctaLabel="View" />
        );

        const link = screen.getByRole('link', { name: 'View' });
        expect(link).toHaveAttribute('href', path);
        unmount();
      });
    });

    it('handles long text in title and subtitle', () => {
      const longTitle = 'A Very Long Title That Might Wrap On Smaller Screens';
      const longSubtitle =
        'This is a very long subtitle that provides additional context about the product section and might need special styling';

      renderWithRouter(
        <ProductSection {...defaultProps} title={longTitle} subtitle={longSubtitle} />
      );

      expect(screen.getByTestId('page-header-title')).toHaveTextContent(longTitle);
      expect(screen.getByTestId('page-header-subtitle')).toHaveTextContent(longSubtitle);
    });

    it('handles special characters in props', () => {
      renderWithRouter(
        <ProductSection
          {...defaultProps}
          title="Products < $50"
          subtitle={'Items with quotes and double quotes'}
          ctaLabel="Shop Now"
        />
      );

      expect(screen.getByTestId('page-header-title')).toHaveTextContent('Products < $50');
      expect(screen.getByTestId('page-header')).toHaveTextContent(
        'Items with quotes and double quotes'
      );
      expect(screen.getByRole('button', { name: 'Shop Now' })).toBeInTheDocument();
    });
  });

  describe('component composition', () => {
    it('contains CTA section in the DOM', () => {
      renderWithRouter(<ProductSection {...defaultProps} />);

      // Verify there's a button in the component
      const button = screen.getByRole('button', { name: 'View All' });
      expect(button).toBeInTheDocument();

      // Verify the button is wrapped in a link
      const link = screen.getByRole('link', { name: 'View All' });
      expect(link).toBeInTheDocument();
    });

    it('renders PageHeader, SimpleProductGrid, and CTA in correct order', () => {
      const products = [createMockProduct({ id: '1', name: 'Test' })];

      renderWithRouter(<ProductSection {...defaultProps} products={products} />);

      const section = screen.getByRole('region');
      const children = Array.from(section.children);

      // Check order: PageHeader -> SimpleProductGrid -> CTA div
      expect(children[0]).toHaveAttribute('data-testid', 'page-header');
      expect(children[1]).toHaveAttribute('data-testid', 'simple-product-grid');
      // CTA div contains the link, verify it's the third child
      expect(children[2].querySelector('a')).toBeInTheDocument();
    });
  });
});

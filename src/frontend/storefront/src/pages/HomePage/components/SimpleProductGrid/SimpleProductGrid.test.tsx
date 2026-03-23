import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import type { Product } from '@/shared/types';
import { SimpleProductGrid } from './SimpleProductGrid';

// Mock ProductCard to isolate SimpleProductGrid tests
vi.mock('@/features/products/components/ProductCard/ProductCard', () => ({
  ProductCard: ({ id, name }: { id: string; name: string }) => (
    <div data-testid="product-card" data-product-id={id}>
      {name}
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

describe('SimpleProductGrid', () => {
  describe('rendering', () => {
    it('renders product grid with products', () => {
      const products: Product[] = [
        createMockProduct({ id: '1', name: 'Product 1' }),
        createMockProduct({ id: '2', name: 'Product 2' }),
        createMockProduct({ id: '3', name: 'Product 3' }),
      ];

      render(<SimpleProductGrid products={products} />);

      const productCards = screen.getAllByTestId('product-card');
      expect(productCards).toHaveLength(3);
      expect(screen.getByText('Product 1')).toBeInTheDocument();
      expect(screen.getByText('Product 2')).toBeInTheDocument();
      expect(screen.getByText('Product 3')).toBeInTheDocument();
    });

    it('renders correct grid structure', () => {
      const products = [createMockProduct()];

      render(<SimpleProductGrid products={products} />);

      const grid = screen.getByTestId('product-card').parentElement;
      // CSS modules hash class names in format _classname_hash
      expect(grid?.className).toMatch(/_grid_/);
    });

    it('renders single product correctly', () => {
      const products = [createMockProduct({ id: 'single', name: 'Single Product' })];

      render(<SimpleProductGrid products={products} />);

      expect(screen.getByText('Single Product')).toBeInTheDocument();
      expect(screen.getAllByTestId('product-card')).toHaveLength(1);
    });
  });

  describe('empty state', () => {
    it('renders empty grid when products array is empty', () => {
      render(<SimpleProductGrid products={[]} />);

      // When products is empty, there's no grid rendered at all
      expect(screen.queryByTestId('product-card')).not.toBeInTheDocument();
    });

    it('handles empty products array gracefully', () => {
      const products: Product[] = [];
      render(<SimpleProductGrid products={products} />);

      expect(screen.queryByTestId('product-card')).not.toBeInTheDocument();
    });
  });

  describe('product data handling', () => {
    it('passes all required props to ProductCard', () => {
      const product = createMockProduct({
        id: 'test-id',
        name: 'Test Name',
        slug: 'test-slug',
        price: 149.99,
        compareAtPrice: 199.99,
        images: [{ id: 'img1', url: '/test-image.jpg', altText: 'Test', isPrimary: true }],
        averageRating: 4.2,
        reviewCount: 50,
        stockQuantity: 25,
      });

      render(<SimpleProductGrid products={[product]} />);

      const card = screen.getByTestId('product-card');
      expect(card).toHaveAttribute('data-product-id', 'test-id');
    });

    it('handles products with no images', () => {
      const product = createMockProduct({ images: [] });

      render(<SimpleProductGrid products={[product]} />);

      expect(screen.getByTestId('product-card')).toBeInTheDocument();
    });

    it('handles products with missing optional fields', () => {
      const product: Product = {
        id: 'minimal',
        name: 'Minimal Product',
        slug: 'minimal',
        price: 50,
        images: [],
        stockQuantity: 0,
        averageRating: 0,
        reviewCount: 0,
      };

      render(<SimpleProductGrid products={[product]} />);

      expect(screen.getByText('Minimal Product')).toBeInTheDocument();
    });

    it('handles products with compareAtPrice', () => {
      const product = createMockProduct({
        price: 79.99,
        compareAtPrice: 99.99,
      });

      render(<SimpleProductGrid products={[product]} />);

      expect(screen.getByTestId('product-card')).toBeInTheDocument();
    });

    it('handles out of stock products', () => {
      const product = createMockProduct({ stockQuantity: 0 });

      render(<SimpleProductGrid products={[product]} />);

      expect(screen.getByTestId('product-card')).toBeInTheDocument();
    });

    it('handles products with categories', () => {
      const product = createMockProduct({
        category: { id: 'cat1', name: 'Electronics', slug: 'electronics' },
      });

      render(<SimpleProductGrid products={[product]} />);

      expect(screen.getByTestId('product-card')).toBeInTheDocument();
    });
  });

  describe('large datasets', () => {
    it('handles large number of products', () => {
      const products = Array.from({ length: 100 }, (_, i) =>
        createMockProduct({ id: String(i), name: `Product ${i}` })
      );

      render(<SimpleProductGrid products={products} />);

      const productCards = screen.getAllByTestId('product-card');
      expect(productCards).toHaveLength(100);
    });
  });

  describe('key prop', () => {
    it('uses product id as key', () => {
      const products = [
        createMockProduct({ id: 'unique-1', name: 'Product A' }),
        createMockProduct({ id: 'unique-2', name: 'Product B' }),
      ];

      render(<SimpleProductGrid products={products} />);

      const cards = screen.getAllByTestId('product-card');
      expect(cards[0]).toHaveAttribute('data-product-id', 'unique-1');
      expect(cards[1]).toHaveAttribute('data-product-id', 'unique-2');
    });

    it('handles products with string numeric ids', () => {
      const products = [createMockProduct({ id: '123', name: 'Product 123' })];

      render(<SimpleProductGrid products={products} />);

      expect(screen.getByTestId('product-card')).toHaveAttribute('data-product-id', '123');
    });
  });
});

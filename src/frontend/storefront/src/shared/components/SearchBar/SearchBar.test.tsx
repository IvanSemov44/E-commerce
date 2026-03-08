import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { SearchBar } from './SearchBar';

interface TranslationParams {
  query?: string;
}

// Mock navigate function
const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

// Mock i18n
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, params?: TranslationParams) => {
      const translations: Record<string, string | ((p: TranslationParams) => string)> = {
        'products.searchProducts': 'Search products...',
        'common.searching': 'Searching...',
        'common.viewAllResults': (p: TranslationParams) =>
          `View all results for "${p.query ?? ''}"`,
      };
      const translation = translations[key];
      if (typeof translation === 'function') {
        return translation(params ?? {});
      }
      return translation || key;
    },
  }),
}));

// Mock icons
vi.mock('@/shared/components/icons', () => ({
  SearchIcon: () => <div data-testid="search-icon">🔍</div>,
  SpinnerIcon: () => <div data-testid="spinner-icon">⏳</div>,
  CloseIcon: () => <div data-testid="close-icon">✕</div>,
  PackageIcon: () => <div data-testid="package-icon">📦</div>,
}));

// Mock products API
const mockProducts = [
  {
    id: '1',
    name: 'Wireless Headphones',
    slug: 'wireless-headphones',
    price: 99.99,
    compareAtPrice: 129.99,
    images: [{ url: 'https://example.com/img1.jpg' }],
  },
  {
    id: '2',
    name: 'Wireless Mouse',
    slug: 'wireless-mouse',
    price: 49.99,
    compareAtPrice: null,
    images: [],
  },
];

vi.mock('@/features/products/api/productApi', () => ({
  useGetProductsQuery: vi.fn(() => ({
    data: { items: mockProducts },
    isFetching: false,
    error: null,
  })),
}));

const renderSearchBar = (props = {}) => {
  return render(
    <MemoryRouter>
      <SearchBar {...props} />
    </MemoryRouter>
  );
};

describe('SearchBar Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render search input', () => {
    renderSearchBar();
    const input = screen.getByPlaceholderText('Search products...');
    expect(input).toBeInTheDocument();
  });

  it('should show search icon', () => {
    renderSearchBar();
    expect(screen.getByTestId('search-icon')).toBeInTheDocument();
  });

  it('should show keyboard shortcut hint when not focused', () => {
    renderSearchBar();
    expect(screen.getAllByText('⌘')[0]).toBeInTheDocument();
    expect(screen.getAllByText('K')[0]).toBeInTheDocument();
  });

  it('should show clear button when query has text', async () => {
    const user = userEvent.setup();
    renderSearchBar();
    const input = screen.getByPlaceholderText('Search products...');

    await user.type(input, 'test');

    const clearButton = screen.getByLabelText('Clear search');
    expect(clearButton).toBeInTheDocument();
  });

  it('should clear input when clear button clicked', async () => {
    const user = userEvent.setup();
    renderSearchBar();
    const input = screen.getByPlaceholderText('Search products...') as HTMLInputElement;

    await user.type(input, 'test');
    expect(input.value).toBe('test');

    const clearButton = screen.getByLabelText('Clear search');
    await user.click(clearButton);

    expect(input.value).toBe('');
  });

  it('should call onSearch callback when query changes', async () => {
    const user = userEvent.setup();
    const onSearch = vi.fn();
    renderSearchBar({ onSearch });
    const input = screen.getByPlaceholderText('Search products...');

    await user.type(input, 'wireless');

    await waitFor(() => {
      expect(onSearch).toHaveBeenCalled();
    });
  });

  it('should show dropdown when query has 2+ characters and focused', async () => {
    const user = userEvent.setup();
    renderSearchBar();
    const input = screen.getByPlaceholderText('Search products...');

    await user.click(input);
    await user.type(input, 'wireless');

    await waitFor(() => {
      expect(screen.getByText('Wireless Headphones')).toBeInTheDocument();
    });
  });

  it('should display products in dropdown', async () => {
    const user = userEvent.setup();
    renderSearchBar();
    const input = screen.getByPlaceholderText('Search products...');

    await user.click(input);
    await user.type(input, 'wireless');

    await waitFor(() => {
      expect(screen.getByText('Wireless Headphones')).toBeInTheDocument();
      expect(screen.getByText('Wireless Mouse')).toBeInTheDocument();
    });
  });

  it('should display product prices', async () => {
    const user = userEvent.setup();
    renderSearchBar();
    const input = screen.getByPlaceholderText('Search products...');

    await user.click(input);
    await user.type(input, 'wireless');

    await waitFor(() => {
      expect(screen.getByText('$99.99')).toBeInTheDocument();
      expect(screen.getByText('$49.99')).toBeInTheDocument();
    });
  });

  it('should display compare-at price when available', async () => {
    const user = userEvent.setup();
    renderSearchBar();
    const input = screen.getByPlaceholderText('Search products...');

    await user.click(input);
    await user.type(input, 'wireless');

    await waitFor(() => {
      expect(screen.getByText('$129.99')).toBeInTheDocument();
    });
  });

  it('should call onSelectResult when product clicked', async () => {
    const user = userEvent.setup();
    const onSelectResult = vi.fn();
    renderSearchBar({ onSelectResult });
    const input = screen.getByPlaceholderText('Search products...');

    await user.click(input);
    await user.type(input, 'wireless');

    await waitFor(() => {
      const productLink = screen.getByText('Wireless Headphones').closest('a');
      expect(productLink).toBeInTheDocument();
    });

    const productLink = screen.getByText('Wireless Headphones').closest('a')!;
    fireEvent.click(productLink);

    expect(onSelectResult).toHaveBeenCalledWith(mockProducts[0]);
  });

  it('should show "View all results" link', async () => {
    const user = userEvent.setup();
    renderSearchBar();
    const input = screen.getByPlaceholderText('Search products...');

    await user.click(input);
    await user.type(input, 'wireless');

    await waitFor(() => {
      expect(screen.getByText(/View all results/)).toBeInTheDocument();
    });
  });

  it('should navigate on form submit', async () => {
    const user = userEvent.setup();
    renderSearchBar();
    const input = screen.getByPlaceholderText('Search products...');

    await user.type(input, 'wireless');

    const form = input.closest('form')!;
    fireEvent.submit(form);

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/products?search=wireless');
    });
  });

  it('should close dropdown when clicking outside', async () => {
    const user = userEvent.setup();
    renderSearchBar();
    const input = screen.getByPlaceholderText('Search products...');

    await user.click(input);
    await user.type(input, 'wireless');

    await waitFor(() => {
      expect(screen.getByText('Wireless Headphones')).toBeInTheDocument();
    });

    // Click outside
    fireEvent.mouseDown(document.body);

    await waitFor(() => {
      expect(screen.queryByText('Wireless Headphones')).not.toBeInTheDocument();
    });
  });

  it('should support keyboard shortcuts', async () => {
    renderSearchBar();
    const input = screen.getByPlaceholderText('Search products...');

    // Cmd+K should focus input
    fireEvent.keyDown(document, { key: 'k', metaKey: true });

    expect(document.activeElement).toBe(input);
  });

  it('should support Escape key to blur', async () => {
    const user = userEvent.setup();
    renderSearchBar();
    const input = screen.getByPlaceholderText('Search products...');

    await user.click(input);
    expect(document.activeElement).toBe(input);

    fireEvent.keyDown(document, { key: 'Escape' });
    expect(document.activeElement).not.toBe(input);
  });

  it('should respect custom placeholder', () => {
    renderSearchBar({ placeholder: 'Custom placeholder...' });
    expect(screen.getByPlaceholderText('Custom placeholder...')).toBeInTheDocument();
  });

  it('should respect size prop', () => {
    const { container } = renderSearchBar({ size: 'lg' });
    const searchContainer = container.querySelector('[class*="container"]');
    expect(searchContainer?.className).toContain('lg');
  });

  it('should apply custom className', () => {
    const { container } = renderSearchBar({ className: 'custom-class' });
    expect(container.querySelector('.custom-class')).toBeInTheDocument();
  });
});

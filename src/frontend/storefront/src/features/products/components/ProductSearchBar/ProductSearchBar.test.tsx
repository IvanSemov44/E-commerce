import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ProductSearchBar from './ProductSearchBar'

// Mock i18n
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const translations: Record<string, string> = {
        'products.searchProducts': 'Search products...',
        'products.sortNewest': 'Newest',
        'products.sortNameAZ': 'Name (A-Z)',
        'products.sortPriceLowHigh': 'Price (Low to High)',
        'products.sortPriceHighLow': 'Price (High to Low)',
        'products.sortRating': 'Rating',
      }
      return translations[key] || key
    },
  }),
}))

describe('ProductSearchBar', () => {
  const mockOnSearchChange = vi.fn()
  const mockOnSortChange = vi.fn()

  const defaultProps = {
    searchValue: '',
    sortBy: 'newest',
    onSearchChange: mockOnSearchChange,
    onSortChange: mockOnSortChange,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should render search input field', () => {
    render(<ProductSearchBar {...defaultProps} />)
    const input = screen.getByPlaceholderText('Search products...')
    expect(input).toBeInTheDocument()
    expect(input).toHaveAttribute('type', 'text')
  })

  it('should render search input with current value', () => {
    render(<ProductSearchBar {...defaultProps} searchValue="headphones" />)
    const input = screen.getByPlaceholderText('Search products...') as HTMLInputElement
    expect(input.value).toBe('headphones')
  })

  it('should render sort dropdown with all options', () => {
    render(<ProductSearchBar {...defaultProps} />)
    const selectElement = screen.getByRole('combobox')
    expect(selectElement).toBeInTheDocument()
    expect(screen.getByText('Newest')).toBeInTheDocument()
    expect(screen.getByText('Name (A-Z)')).toBeInTheDocument()
    expect(screen.getByText('Price (Low to High)')).toBeInTheDocument()
    expect(screen.getByText('Price (High to Low)')).toBeInTheDocument()
    expect(screen.getByText('Rating')).toBeInTheDocument()
  })

  it('should display selected sort option', () => {
    render(<ProductSearchBar {...defaultProps} sortBy="price-asc" />)
    const selectElement = screen.getByRole('combobox') as HTMLSelectElement
    expect(selectElement.value).toBe('price-asc')
  })

  it('should call onSearchChange when input text changes', async () => {
    const user = userEvent.setup()
    render(<ProductSearchBar {...defaultProps} />)
    const input = screen.getByPlaceholderText('Search products...')

    await user.type(input, 'test')

    // onChange fires for each character typed
    expect(mockOnSearchChange).toHaveBeenCalled()
    expect(mockOnSearchChange).toHaveBeenCalledWith('t')
  })

  it('should call onSearchChange with each character typed', async () => {
    const user = userEvent.setup()
    render(<ProductSearchBar {...defaultProps} searchValue="" />)
    const input = screen.getByPlaceholderText('Search products...')

    await user.type(input, 'test')
    
    // onChange fires for each keystroke
    expect(mockOnSearchChange).toHaveBeenCalledWith('t')
    expect(mockOnSearchChange).toHaveBeenCalledWith('e')
    expect(mockOnSearchChange).toHaveBeenCalledWith('s')
  })

  it('should call onSortChange when sort option changes', () => {
    render(<ProductSearchBar {...defaultProps} />)
    const selectElement = screen.getByRole('combobox')

    fireEvent.change(selectElement, { target: { value: 'price-desc' } })

    expect(mockOnSortChange).toHaveBeenCalledWith('price-desc')
  })

  it('should call onSortChange with correct values for each option', () => {
    render(<ProductSearchBar {...defaultProps} />)
    const selectElement = screen.getByRole('combobox')

    fireEvent.change(selectElement, { target: { value: 'name' } })
    expect(mockOnSortChange).toHaveBeenCalledWith('name')

    fireEvent.change(selectElement, { target: { value: 'rating' } })
    expect(mockOnSortChange).toHaveBeenCalledWith('rating')

    fireEvent.change(selectElement, { target: { value: 'price-asc' } })
    expect(mockOnSortChange).toHaveBeenCalledWith('price-asc')
  })

  it('should update input value when searchValue prop changes', () => {
    const { rerender } = render(
      <ProductSearchBar {...defaultProps} searchValue="initial" />
    )
    const input = screen.getByPlaceholderText('Search products...') as HTMLInputElement
    expect(input.value).toBe('initial')

    rerender(<ProductSearchBar {...defaultProps} searchValue="updated" />)
    expect(input.value).toBe('updated')
  })

  it('should update sort value when sortBy prop changes', () => {
    const { rerender } = render(<ProductSearchBar {...defaultProps} sortBy="newest" />)
    const selectElement = screen.getByRole('combobox') as HTMLSelectElement
    expect(selectElement.value).toBe('newest')

    rerender(<ProductSearchBar {...defaultProps} sortBy="price-desc" />)
    expect(selectElement.value).toBe('price-desc')
  })

  it('should handle rapid search input changes', async () => {
    const user = userEvent.setup()
    render(<ProductSearchBar {...defaultProps} />)
    const input = screen.getByPlaceholderText('Search products...')

    await user.type(input, 'test')

    // onChange fires multiple times for each character
    expect(mockOnSearchChange).toHaveBeenCalledTimes(4)
  })

  it('should handle clearing search input', async () => {
    const user = userEvent.setup()
    render(<ProductSearchBar {...defaultProps} searchValue="headphones" />)
    const input = screen.getByPlaceholderText('Search products...')

    await user.clear(input)

    expect(mockOnSearchChange).toHaveBeenCalledWith('')
  })

  it('should handle empty search and default sort', () => {
    render(<ProductSearchBar {...defaultProps} searchValue="" sortBy="newest" />)
    const input = screen.getByPlaceholderText('Search products...') as HTMLInputElement
    const selectElement = screen.getByRole('combobox') as HTMLSelectElement

    expect(input.value).toBe('')
    expect(selectElement.value).toBe('newest')
  })
})

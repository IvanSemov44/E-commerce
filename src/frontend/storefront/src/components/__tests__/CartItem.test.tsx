import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import CartItem from '../CartItem'
import type { CartItem as CartItemType } from '../../store/slices/cartSlice'

// Mock the constants
vi.mock('../../utils/constants', () => ({
  DEFAULT_PRODUCT_IMAGE: 'https://placehold.co/400x400/f1f5f9/64748b?text=Product',
}))

const defaultItem: CartItemType = {
  id: '1',
  name: 'Test Product',
  slug: 'test-product',
  price: 29.99,
  quantity: 2,
  maxStock: 10,
  image: 'https://example.com/image.jpg',
}

const renderCartItem = (
  item: CartItemType = defaultItem,
  props: { onUpdateQuantity?: (id: string, quantity: number) => void; onRemove?: (id: string) => void; readOnly?: boolean } = {}
) => {
  const onUpdateQuantity = props.onUpdateQuantity || vi.fn()
  const onRemove = props.onRemove || vi.fn()

  return render(
    <MemoryRouter>
      <CartItem
        item={item}
        onUpdateQuantity={onUpdateQuantity}
        onRemove={onRemove}
        readOnly={props.readOnly}
      />
    </MemoryRouter>
  )
}

describe('CartItem', () => {
  describe('rendering', () => {
    it('should render product name', () => {
      renderCartItem()

      expect(screen.getByText('Test Product')).toBeInTheDocument()
    })

    it('should render product price', () => {
      renderCartItem()

      expect(screen.getByText('$29.99')).toBeInTheDocument()
    })

    it('should render product image with correct alt text', () => {
      renderCartItem()

      const img = screen.getByRole('img', { name: 'Test Product' })
      expect(img).toBeInTheDocument()
      expect(img).toHaveAttribute('src', 'https://example.com/image.jpg')
    })

    it('should render quantity', () => {
      renderCartItem()

      expect(screen.getByText('2')).toBeInTheDocument()
    })

    it('should render subtotal', () => {
      renderCartItem()

      // 29.99 * 2 = 59.98
      expect(screen.getByText('$59.98')).toBeInTheDocument()
    })

    it('should render compare at price when provided', () => {
      const item: CartItemType = {
        ...defaultItem,
        compareAtPrice: 39.99,
      }

      renderCartItem(item)

      expect(screen.getByText('$39.99')).toBeInTheDocument()
    })
  })

  describe('links', () => {
    it('should have correct product link on image', () => {
      renderCartItem()

      const links = screen.getAllByRole('link')
      const imageLink = links.find((link) => link.querySelector('img'))
      expect(imageLink).toHaveAttribute('href', '/products/test-product')
    })

    it('should have correct product link on name', () => {
      renderCartItem()

      // There are multiple links with "Test Product" - one for image, one for name
      const links = screen.getAllByRole('link', { name: /Test Product/ })
      // Find the one that contains text (not the image link)
      const nameLink = links.find((link) => link.textContent === 'Test Product')
      expect(nameLink).toHaveAttribute('href', '/products/test-product')
    })
  })

  describe('quantity controls', () => {
    it('should call onUpdateQuantity with decreased quantity when minus is clicked', async () => {
      const user = userEvent.setup()
      const onUpdateQuantity = vi.fn()

      renderCartItem(defaultItem, { onUpdateQuantity })

      const minusButton = screen.getByRole('button', { name: '−' })
      await user.click(minusButton)

      expect(onUpdateQuantity).toHaveBeenCalledWith('1', 1)
    })

    it('should call onUpdateQuantity with increased quantity when plus is clicked', async () => {
      const user = userEvent.setup()
      const onUpdateQuantity = vi.fn()

      renderCartItem(defaultItem, { onUpdateQuantity })

      const plusButton = screen.getByRole('button', { name: '+' })
      await user.click(plusButton)

      expect(onUpdateQuantity).toHaveBeenCalledWith('1', 3)
    })

    it('should disable plus button when at max stock', () => {
      const item: CartItemType = {
        ...defaultItem,
        quantity: 10,
        maxStock: 10,
      }

      renderCartItem(item)

      const plusButton = screen.getByRole('button', { name: '+' })
      expect(plusButton).toBeDisabled()
    })

    it('should show max stock warning when at max stock', () => {
      const item: CartItemType = {
        ...defaultItem,
        quantity: 10,
        maxStock: 10,
      }

      renderCartItem(item)

      expect(screen.getByText('Max stock reached')).toBeInTheDocument()
    })

    it('should not show max stock warning when below max stock', () => {
      renderCartItem()

      expect(screen.queryByText('Max stock reached')).not.toBeInTheDocument()
    })
  })

  describe('remove button', () => {
    it('should call onRemove when remove button is clicked', async () => {
      const user = userEvent.setup()
      const onRemove = vi.fn()

      renderCartItem(defaultItem, { onRemove })

      const removeButton = screen.getByRole('button', { name: 'Remove' })
      await user.click(removeButton)

      expect(onRemove).toHaveBeenCalledWith('1')
    })
  })

  describe('readOnly mode', () => {
    it('should not show quantity controls in readOnly mode', () => {
      renderCartItem(defaultItem, { readOnly: true })

      expect(screen.queryByRole('button', { name: '−' })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: '+' })).not.toBeInTheDocument()
    })

    it('should not show remove button in readOnly mode', () => {
      renderCartItem(defaultItem, { readOnly: true })

      expect(screen.queryByRole('button', { name: 'Remove' })).not.toBeInTheDocument()
    })

    it('should show quantity text in readOnly mode', () => {
      renderCartItem(defaultItem, { readOnly: true })

      expect(screen.getByText('Quantity: 2')).toBeInTheDocument()
    })
  })

  describe('image error handling', () => {
    it('should fallback to default product image on error', async () => {
      renderCartItem()

      const img = screen.getByRole('img', { name: 'Test Product' })
      img.dispatchEvent(new Event('error'))

      expect(img).toHaveAttribute('src', 'https://placehold.co/400x400/f1f5f9/64748b?text=Product')
    })
  })
})
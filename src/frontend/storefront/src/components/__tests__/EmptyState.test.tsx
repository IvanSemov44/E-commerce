import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import EmptyState from '../EmptyState'

describe('EmptyState', () => {
  describe('rendering', () => {
    it('should render title', () => {
      render(<EmptyState title="No items found" />)

      expect(screen.getByText('No items found')).toBeInTheDocument()
    })

    it('should render description when provided', () => {
      render(
        <EmptyState
          title="No items found"
          description="Try adjusting your search filters"
        />
      )

      expect(screen.getByText('Try adjusting your search filters')).toBeInTheDocument()
    })

    it('should not render description when not provided', () => {
      render(<EmptyState title="No items found" />)

      expect(screen.queryByText('Try adjusting your search filters')).not.toBeInTheDocument()
    })
  })

  describe('icon', () => {
    it('should render icon when provided', () => {
      render(
        <EmptyState
          title="No items"
          icon={<span data-testid="custom-icon">Empty</span>}
        />
      )

      expect(screen.getByTestId('custom-icon')).toBeInTheDocument()
    })

    it('should not render icon when not provided', () => {
      render(<EmptyState title="No items" />)

      expect(screen.queryByTestId('custom-icon')).not.toBeInTheDocument()
    })
  })

  describe('action', () => {
    it('should render action when provided', () => {
      render(
        <EmptyState
          title="No items"
          action={<button>Add Item</button>}
        />
      )

      expect(screen.getByRole('button', { name: 'Add Item' })).toBeInTheDocument()
    })

    it('should not render action when not provided', () => {
      render(<EmptyState title="No items" />)

      expect(screen.queryByRole('button')).not.toBeInTheDocument()
    })

    it('should render complex action elements', () => {
      render(
        <EmptyState
          title="No items"
          action={
            <div>
              <button>Button 1</button>
              <button>Button 2</button>
            </div>
          }
        />
      )

      expect(screen.getByRole('button', { name: 'Button 1' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Button 2' })).toBeInTheDocument()
    })
  })

  describe('complete state', () => {
    it('should render all elements together', () => {
      render(
        <EmptyState
          title="Your cart is empty"
          description="Add some products to get started"
          icon={<span>Empty Cart</span>}
          action={<button>Start Shopping</button>}
        />
      )

      expect(screen.getByText('Your cart is empty')).toBeInTheDocument()
      expect(screen.getByText('Add some products to get started')).toBeInTheDocument()
      expect(screen.getByText('Empty Cart')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Start Shopping' })).toBeInTheDocument()
    })
  })
})

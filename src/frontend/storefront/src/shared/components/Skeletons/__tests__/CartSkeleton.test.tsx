import { describe, it, expect } from 'vitest'
import { render } from '@testing-library/react'
import CartSkeleton from '../CartSkeleton'

describe('CartSkeleton', () => {
  it('renders cart container', () => {
    const { container } = render(<CartSkeleton />)
    const cartContainer = container.querySelector('[class*="cartContainer"]')
    expect(cartContainer).toBeInTheDocument()
  })

  it('renders cart items section', () => {
    const { container } = render(<CartSkeleton />)
    const cartItems = container.querySelector('[class*="cartItems"]')
    expect(cartItems).toBeInTheDocument()
  })

  it('renders three cart item skeletons by default', () => {
    const { container } = render(<CartSkeleton />)
    const cartItemsContainer = container.querySelector('[class*="cartItems"]')
    const cartItems = cartItemsContainer?.querySelectorAll(':scope > [class*="cartItem"]')
    expect(cartItems).toHaveLength(3)
  })

  it('renders cart summary section', () => {
    const { container } = render(<CartSkeleton />)
    const cartSummary = container.querySelector('[class*="cartSummary"]')
    expect(cartSummary).toBeInTheDocument()
  })

  it('renders cart item info in each item', () => {
    const { container } = render(<CartSkeleton />)
    const cartItemInfos = container.querySelectorAll('[class*="cartItemInfo"]')
    expect(cartItemInfos.length).toBe(3)
  })

  it('renders cart item actions in each item', () => {
    const { container } = render(<CartSkeleton />)
    const cartItemActions = container.querySelectorAll('[class*="cartItemActions"]')
    expect(cartItemActions.length).toBe(3)
  })

  it('renders summary rows', () => {
    const { container } = render(<CartSkeleton />)
    const summaryRows = container.querySelectorAll('[class*="summaryRow"]')
    expect(summaryRows.length).toBeGreaterThanOrEqual(2)
  })

  it('renders multiple skeleton elements', () => {
    const { container } = render(<CartSkeleton />)
    const skeletons = container.querySelectorAll('span[class*="skeleton"]')
    // Should have multiple skeletons for items + summary
    expect(skeletons.length).toBeGreaterThan(10)
  })

  it('all skeletons have proper aria attributes', () => {
    const { container } = render(<CartSkeleton />)
    const skeletons = container.querySelectorAll('span[class*="skeleton"]')
    skeletons.forEach((skeleton) => {
      expect(skeleton).toHaveAttribute('aria-busy', 'true')
      expect(skeleton).toHaveAttribute('aria-label', 'Loading')
    })
  })
})

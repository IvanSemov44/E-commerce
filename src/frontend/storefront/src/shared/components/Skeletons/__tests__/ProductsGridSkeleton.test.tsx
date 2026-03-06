import { describe, it, expect } from 'vitest'
import { render } from '@testing-library/react'
import ProductsGridSkeleton from '../ProductsGridSkeleton'

describe('ProductsGridSkeleton', () => {
  it('renders products grid container', () => {
    const { container } = render(<ProductsGridSkeleton />)
    const productsGrid = container.querySelector('[class*="productsGrid"]')
    expect(productsGrid).toBeInTheDocument()
  })

  it('renders 12 product skeletons by default', () => {
    const { container } = render(<ProductsGridSkeleton />)
    const productCards = container.querySelectorAll('[class*="productCard"]')
    expect(productCards).toHaveLength(12)
  })

  it('renders custom count of product skeletons', () => {
    const { container } = render(<ProductsGridSkeleton count={6} />)
    const productCards = container.querySelectorAll('[class*="productCard"]')
    expect(productCards).toHaveLength(6)
  })

  it('renders single product skeleton when count is 1', () => {
    const { container } = render(<ProductsGridSkeleton count={1} />)
    const productCards = container.querySelectorAll('[class*="productCard"]')
    expect(productCards).toHaveLength(1)
  })

  it('renders large grid when count is high', () => {
    const { container } = render(<ProductsGridSkeleton count={24} />)
    const productCards = container.querySelectorAll('[class*="productCard"]')
    expect(productCards).toHaveLength(24)
  })

  it('each product skeleton has proper structure', () => {
    const { container } = render(<ProductsGridSkeleton count={3} />)
    const productInfos = container.querySelectorAll('[class*="productInfo"]')
    expect(productInfos.length).toBe(3)
  })

  it('renders multiple skeleton elements per product', () => {
    const { container } = render(<ProductsGridSkeleton count={1} />)
    const skeletons = container.querySelectorAll('span[class*="skeleton"]')
    // Each product has: image, name, price, rating, button = 5 skeletons
    expect(skeletons.length).toBeGreaterThanOrEqual(5)
  })

  it('all skeletons have proper aria attributes', () => {
    const { container } = render(<ProductsGridSkeleton count={2} />)
    const skeletons = container.querySelectorAll('span[class*="skeleton"]')
    skeletons.forEach((skeleton) => {
      expect(skeleton).toHaveAttribute('aria-busy', 'true')
      expect(skeleton).toHaveAttribute('aria-label', 'Loading')
    })
  })
})

import { describe, it, expect } from 'vitest'
import { render } from '@testing-library/react'
import LoadingSkeleton from '../LoadingSkeleton'

describe('LoadingSkeleton', () => {
  describe('card type', () => {
    it('should render card skeleton by default', () => {
      render(<LoadingSkeleton />)

      const skeletons = document.querySelectorAll('.animate-pulse')
      expect(skeletons.length).toBeGreaterThan(0)
    })

    it('should render correct number of card skeletons', () => {
      render(<LoadingSkeleton count={3} type="card" />)

      const skeletons = document.querySelectorAll('.animate-pulse')
      expect(skeletons.length).toBe(3)
    })

    it('should render single card skeleton when count is 1', () => {
      render(<LoadingSkeleton count={1} type="card" />)

      const skeletons = document.querySelectorAll('.animate-pulse')
      expect(skeletons.length).toBe(1)
    })
  })

  describe('text type', () => {
    it('should render text skeleton', () => {
      render(<LoadingSkeleton type="text" />)

      const skeletons = document.querySelectorAll('.animate-pulse')
      expect(skeletons.length).toBeGreaterThan(0)
    })

    it('should render correct number of text skeletons', () => {
      render(<LoadingSkeleton count={5} type="text" />)

      const skeletons = document.querySelectorAll('.animate-pulse')
      expect(skeletons.length).toBe(5)
    })
  })

  describe('image type', () => {
    it('should render image skeleton', () => {
      const { container } = render(<LoadingSkeleton type="image" />)

      expect(container.querySelector('.animate-pulse')).toBeInTheDocument()
    })

    it('should render single image skeleton regardless of count', () => {
      const { container } = render(<LoadingSkeleton count={5} type="image" />)

      const skeletons = container.querySelectorAll('.animate-pulse')
      expect(skeletons.length).toBe(1)
    })
  })

  describe('default values', () => {
    it('should default count to 1', () => {
      render(<LoadingSkeleton />)

      const skeletons = document.querySelectorAll('.animate-pulse')
      expect(skeletons.length).toBe(1)
    })

    it('should default type to card', () => {
      render(<LoadingSkeleton />)

      // Card type has nested structure
      const skeletons = document.querySelectorAll('.animate-pulse')
      expect(skeletons.length).toBeGreaterThan(0)
    })
  })
})
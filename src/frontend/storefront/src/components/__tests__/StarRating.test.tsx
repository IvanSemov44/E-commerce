import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import StarRating from '../StarRating'

describe('StarRating', () => {
  describe('rendering', () => {
    it('should render the correct number of stars', () => {
      render(<StarRating rating={3} />)

      const buttons = screen.getAllByRole('button')
      expect(buttons).toHaveLength(5)
    })

    it('should render custom maxStars count', () => {
      render(<StarRating rating={3} maxStars={10} />)

      const buttons = screen.getAllByRole('button')
      expect(buttons).toHaveLength(10)
    })

    it('should render filled stars up to rating', () => {
      render(<StarRating rating={3} />)

      const buttons = screen.getAllByRole('button')
      // First 3 stars should be filled (CSS modules generate hashed class names)
      expect(buttons[0].className).toMatch(/filled/)
      expect(buttons[1].className).toMatch(/filled/)
      expect(buttons[2].className).toMatch(/filled/)
      expect(buttons[3].className).toMatch(/empty/)
      expect(buttons[4].className).toMatch(/empty/)
    })

    it('should render all empty stars when rating is 0', () => {
      render(<StarRating rating={0} />)

      const buttons = screen.getAllByRole('button')
      buttons.forEach((button) => {
        expect(button.className).toMatch(/empty/)
      })
    })

    it('should render all filled stars when rating equals maxStars', () => {
      render(<StarRating rating={5} />)

      const buttons = screen.getAllByRole('button')
      buttons.forEach((button) => {
        expect(button.className).toMatch(/filled/)
      })
    })
  })

  describe('sizes', () => {
    it('should apply sm size class', () => {
      const { container } = render(<StarRating rating={3} size="sm" />)

      expect((container.firstChild as HTMLElement)?.className).toMatch(/sm/)
    })

    it('should apply md size class by default', () => {
      const { container } = render(<StarRating rating={3} />)

      expect((container.firstChild as HTMLElement)?.className).toMatch(/md/)
    })

    it('should apply lg size class', () => {
      const { container } = render(<StarRating rating={3} size="lg" />)

      expect((container.firstChild as HTMLElement)?.className).toMatch(/lg/)
    })
  })

  describe('interactivity', () => {
    it('should call onRatingChange when star is clicked', async () => {
      const user = userEvent.setup()
      const onRatingChange = vi.fn()

      render(<StarRating rating={3} onRatingChange={onRatingChange} />)

      const buttons = screen.getAllByRole('button')
      await user.click(buttons[4])

      expect(onRatingChange).toHaveBeenCalledWith(5)
    })

    it('should not call onRatingChange when readonly', async () => {
      const user = userEvent.setup()
      const onRatingChange = vi.fn()

      render(<StarRating rating={3} onRatingChange={onRatingChange} readonly />)

      const buttons = screen.getAllByRole('button')
      await user.click(buttons[4])

      expect(onRatingChange).not.toHaveBeenCalled()
    })

    it('should have disabled buttons when readonly', () => {
      render(<StarRating rating={3} readonly />)

      const buttons = screen.getAllByRole('button')
      buttons.forEach((button) => {
        expect(button).toBeDisabled()
      })
    })

    it('should have enabled buttons when not readonly', () => {
      render(<StarRating rating={3} />)

      const buttons = screen.getAllByRole('button')
      buttons.forEach((button) => {
        expect(button).not.toBeDisabled()
      })
    })
  })

  describe('accessibility', () => {
    it('should have title attribute on each star', () => {
      render(<StarRating rating={3} />)

      const buttons = screen.getAllByRole('button')
      expect(buttons[0]).toHaveAttribute('title', '1 stars')
      expect(buttons[1]).toHaveAttribute('title', '2 stars')
      expect(buttons[2]).toHaveAttribute('title', '3 stars')
      expect(buttons[3]).toHaveAttribute('title', '4 stars')
      expect(buttons[4]).toHaveAttribute('title', '5 stars')
    })

    it('should render svg icons inside buttons', () => {
      render(<StarRating rating={3} />)

      const buttons = screen.getAllByRole('button')
      buttons.forEach((button) => {
        const svg = button.querySelector('svg')
        expect(svg).toBeInTheDocument()
      })
    })
  })
})
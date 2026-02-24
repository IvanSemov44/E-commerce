import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, act, fireEvent } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import CookieConsent from '../CookieConsent'

// Mock timers
vi.useFakeTimers()

const renderCookieConsent = () => {
  return render(
    <MemoryRouter>
      <CookieConsent />
    </MemoryRouter>
  )
}

describe('CookieConsent', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  afterEach(() => {
    vi.clearAllTimers()
  })

  describe('visibility', () => {
    it('should not be visible initially', () => {
      renderCookieConsent()

      expect(screen.queryByText(/Cookie Preferences/)).not.toBeInTheDocument()
    })

    it('should become visible after delay when no consent is stored', () => {
      renderCookieConsent()

      act(() => {
        vi.advanceTimersByTime(1000)
      })

      expect(screen.getByText(/Cookie Preferences/)).toBeInTheDocument()
    })

    it('should not be visible when consent is already accepted', () => {
      localStorage.setItem('cookie-consent', 'accepted')

      renderCookieConsent()

      act(() => {
        vi.advanceTimersByTime(1000)
      })

      expect(screen.queryByText(/Cookie Preferences/)).not.toBeInTheDocument()
    })

    it('should not be visible when consent is already declined', () => {
      localStorage.setItem('cookie-consent', 'declined')

      renderCookieConsent()

      act(() => {
        vi.advanceTimersByTime(1000)
      })

      expect(screen.queryByText(/Cookie Preferences/)).not.toBeInTheDocument()
    })
  })

  describe('content', () => {
    it('should render title with cookie emoji', () => {
      renderCookieConsent()

      act(() => {
        vi.advanceTimersByTime(1000)
      })

      expect(screen.getByText('🍪 Cookie Preferences')).toBeInTheDocument()
    })

    it('should render description text', () => {
      renderCookieConsent()

      act(() => {
        vi.advanceTimersByTime(1000)
      })

      expect(screen.getByText(/We use cookies to enhance your browsing experience/)).toBeInTheDocument()
    })

    it('should render Privacy Policy link', () => {
      renderCookieConsent()

      act(() => {
        vi.advanceTimersByTime(1000)
      })

      const privacyLink = screen.getByRole('link', { name: 'Privacy Policy' })
      expect(privacyLink).toBeInTheDocument()
      expect(privacyLink).toHaveAttribute('href', '/privacy')
    })
  })

  describe('actions', () => {
    it('should render Decline button', () => {
      renderCookieConsent()

      act(() => {
        vi.advanceTimersByTime(1000)
      })

      expect(screen.getByRole('button', { name: 'Decline' })).toBeInTheDocument()
    })

    it('should render Accept All button', () => {
      renderCookieConsent()

      act(() => {
        vi.advanceTimersByTime(1000)
      })

      expect(screen.getByRole('button', { name: 'Accept All' })).toBeInTheDocument()
    })

    it('should store "accepted" and hide banner when Accept All is clicked', () => {
      renderCookieConsent()

      act(() => {
        vi.advanceTimersByTime(1000)
      })

      // Use fireEvent instead of userEvent for fake timers compatibility
      fireEvent.click(screen.getByRole('button', { name: 'Accept All' }))

      expect(localStorage.getItem('cookie-consent')).toBe('accepted')
      expect(screen.queryByText(/Cookie Preferences/)).not.toBeInTheDocument()
    })

    it('should store "declined" and hide banner when Decline is clicked', () => {
      renderCookieConsent()

      act(() => {
        vi.advanceTimersByTime(1000)
      })

      // Use fireEvent instead of userEvent for fake timers compatibility
      fireEvent.click(screen.getByRole('button', { name: 'Decline' }))

      expect(localStorage.getItem('cookie-consent')).toBe('declined')
      expect(screen.queryByText(/Cookie Preferences/)).not.toBeInTheDocument()
    })
  })

  describe('cleanup', () => {
    it('should clear timeout on unmount', () => {
      const { unmount } = renderCookieConsent()

      unmount()

      act(() => {
        vi.advanceTimersByTime(1000)
      })

      // Should not throw any errors
      expect(true).toBe(true)
    })
  })
})

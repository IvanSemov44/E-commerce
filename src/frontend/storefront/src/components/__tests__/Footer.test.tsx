import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { configureStore } from '@reduxjs/toolkit'
import { Provider } from 'react-redux'
import toastReducer from '../../store/slices/toastSlice'
import Footer from '../Footer'

const createMockStore = () => {
  return configureStore({
    reducer: {
      toast: toastReducer,
    },
  })
}

const renderFooter = () => {
  const store = createMockStore()
  return render(
    <Provider store={store}>
      <MemoryRouter>
        <Footer />
      </MemoryRouter>
    </Provider>
  )
}

describe('Footer', () => {
  describe('sections', () => {
    it('should render Company section', () => {
      renderFooter()

      expect(screen.getByRole('heading', { name: 'Company' })).toBeInTheDocument()
    })

    it('should render Support section', () => {
      renderFooter()

      expect(screen.getByRole('heading', { name: 'Support' })).toBeInTheDocument()
    })

    it('should render Legal section', () => {
      renderFooter()

      expect(screen.getByRole('heading', { name: 'Legal' })).toBeInTheDocument()
    })

    it('should render Newsletter section', () => {
      renderFooter()

      expect(screen.getByRole('heading', { name: 'Newsletter' })).toBeInTheDocument()
    })
  })

  describe('Company links', () => {
    it('should render About Us link', () => {
      renderFooter()

      expect(screen.getByRole('link', { name: 'About Us' })).toBeInTheDocument()
    })

    it('should render Careers link', () => {
      renderFooter()

      expect(screen.getByRole('link', { name: 'Careers' })).toBeInTheDocument()
    })

    it('should render Press link', () => {
      renderFooter()

      expect(screen.getByRole('link', { name: 'Press' })).toBeInTheDocument()
    })

    it('should render Blog link', () => {
      renderFooter()

      expect(screen.getByRole('link', { name: 'Blog' })).toBeInTheDocument()
    })
  })

  describe('Support links', () => {
    it('should render Help Center link', () => {
      renderFooter()

      expect(screen.getByRole('link', { name: 'Help Center' })).toBeInTheDocument()
    })

    it('should render Contact Us link', () => {
      renderFooter()

      expect(screen.getByRole('link', { name: 'Contact Us' })).toBeInTheDocument()
    })

    it('should render Track Order link', () => {
      renderFooter()

      expect(screen.getByRole('link', { name: 'Track Order' })).toBeInTheDocument()
    })

    it('should render Returns link', () => {
      renderFooter()

      const returnsLink = screen.getByRole('link', { name: 'Returns' })
      expect(returnsLink).toBeInTheDocument()
      expect(returnsLink).toHaveAttribute('href', '/returns')
    })
  })

  describe('Legal links', () => {
    it('should render Privacy Policy link', () => {
      renderFooter()

      const privacyLink = screen.getByRole('link', { name: 'Privacy Policy' })
      expect(privacyLink).toBeInTheDocument()
      expect(privacyLink).toHaveAttribute('href', '/privacy')
    })

    it('should render Terms of Service link', () => {
      renderFooter()

      const termsLink = screen.getByRole('link', { name: 'Terms of Service' })
      expect(termsLink).toBeInTheDocument()
      expect(termsLink).toHaveAttribute('href', '/terms')
    })

    it('should render Cookies link', () => {
      renderFooter()

      expect(screen.getByRole('link', { name: 'Cookies' })).toBeInTheDocument()
    })

    it('should render Security link', () => {
      renderFooter()

      expect(screen.getByRole('link', { name: 'Security' })).toBeInTheDocument()
    })
  })

  describe('Newsletter', () => {
    it('should render newsletter description', () => {
      renderFooter()

      expect(screen.getByText('Subscribe to get special offers and updates')).toBeInTheDocument()
    })

    it('should render email input', () => {
      renderFooter()

      const emailInput = screen.getByPlaceholderText('Your email')
      expect(emailInput).toBeInTheDocument()
      expect(emailInput).toHaveAttribute('type', 'email')
    })

    it('should render subscribe button', () => {
      renderFooter()

      expect(screen.getByRole('button', { name: 'Subscribe' })).toBeInTheDocument()
    })
  })

  describe('bottom section', () => {
    it('should render copyright text', () => {
      renderFooter()

      expect(screen.getByText(/2026 E-Commerce\. All rights reserved\./)).toBeInTheDocument()
    })

    it('should render social links', () => {
      renderFooter()

      expect(screen.getByRole('link', { name: 'Facebook' })).toBeInTheDocument()
      expect(screen.getByRole('link', { name: 'Twitter' })).toBeInTheDocument()
      expect(screen.getByRole('link', { name: 'Instagram' })).toBeInTheDocument()
    })
  })
})
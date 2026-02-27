import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { Provider } from 'react-redux'
import { configureStore } from '@reduxjs/toolkit'
import { I18nextProvider } from 'react-i18next'
import i18n from 'i18next'
import Checkout from '../Checkout'
import { cartReducer } from '../../store/slices/cartSlice'
import { authSlice } from '../../store/slices/authSlice'
import toastReducer from '../../store/slices/toastSlice'

// Mock useCheckout hook
const mockUseCheckout = vi.fn(() => ({
  formData: {
    email: '',
    firstName: '',
    lastName: '',
    address: '',
    city: '',
    postalCode: '',
    country: '',
    phone: '',
  },
  promoCode: '',
  promoCodeValidation: null,
  validatingPromoCode: false,
  handleApplyPromoCode: vi.fn(),

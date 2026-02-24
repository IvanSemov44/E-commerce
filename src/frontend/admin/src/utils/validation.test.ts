import { describe, it, expect } from 'vitest'
import {
  validators,
  validateRequired,
  validateEmail,
  validatePhone,
  validatePositiveNumber,
} from './validation'

describe('validation utilities', () => {
  describe('validators.required', () => {
    it('returns empty string for non-empty values', () => {
      expect(validators.required()('test')).toBe('')
      expect(validators.required()('  test  ')).toBe('')
    })

    it('returns error for empty values', () => {
      expect(validators.required()('')).toBe('This field is required')
      expect(validators.required()('   ')).toBe('This field is required')
    })

    it('uses custom field name in error message', () => {
      expect(validators.required('Email')('')).toBe('Email is required')
      expect(validators.required('Username')('')).toBe('Username is required')
    })
  })

  describe('validators.email', () => {
    it('returns empty string for valid emails', () => {
      expect(validators.email('test@example.com')).toBe('')
      expect(validators.email('user.name@domain.co.uk')).toBe('')
      expect(validators.email('user+tag@example.org')).toBe('')
    })

    it('returns error for invalid emails', () => {
      expect(validators.email('invalid')).toBe('Invalid email address')
      expect(validators.email('invalid@')).toBe('Invalid email address')
      expect(validators.email('@example.com')).toBe('Invalid email address')
      expect(validators.email('test@.com')).toBe('Invalid email address')
    })
  })

  describe('validators.minLength', () => {
    it('returns empty string when length meets minimum', () => {
      expect(validators.minLength(3)('test')).toBe('')
      expect(validators.minLength(5)('12345')).toBe('')
      expect(validators.minLength(0)('')).toBe('')
    })

    it('returns error when length is below minimum', () => {
      expect(validators.minLength(3)('ab')).toBe('Must be at least 3 characters')
      expect(validators.minLength(5)('1234')).toBe('Must be at least 5 characters')
    })
  })

  describe('validators.maxLength', () => {
    it('returns empty string when length is within maximum', () => {
      expect(validators.maxLength(5)('test')).toBe('')
      expect(validators.maxLength(3)('abc')).toBe('')
      expect(validators.maxLength(10)('')).toBe('')
    })

    it('returns error when length exceeds maximum', () => {
      expect(validators.maxLength(3)('abcd')).toBe('Must be no more than 3 characters')
      expect(validators.maxLength(5)('123456')).toBe('Must be no more than 5 characters')
    })
  })

  describe('validators.phone', () => {
    it('returns empty string for valid phone numbers', () => {
      expect(validators.phone('+1234567890')).toBe('')
      expect(validators.phone('1234567890')).toBe('')
      expect(validators.phone('+1 (234) 567-8901')).toBe('')
    })

    it('returns error for invalid phone numbers', () => {
      expect(validators.phone('123')).toBe('Invalid phone number')
      expect(validators.phone('abc')).toBe('Invalid phone number')
    })
  })

  describe('validators.numeric', () => {
    it('returns empty string for numeric values', () => {
      expect(validators.numeric('123')).toBe('')
      expect(validators.numeric('0')).toBe('')
      expect(validators.numeric('999999')).toBe('')
    })

    it('returns error for non-numeric values', () => {
      expect(validators.numeric('abc')).toBe('Must be a number')
      expect(validators.numeric('12.34')).toBe('Must be a number')
      expect(validators.numeric('')).toBe('Must be a number')
    })
  })

  describe('validators.positiveNumber', () => {
    it('returns empty string for positive numbers', () => {
      expect(validators.positiveNumber('1')).toBe('')
      expect(validators.positiveNumber('100')).toBe('')
      expect(validators.positiveNumber('0.01')).toBe('')
      expect(validators.positiveNumber('99.99')).toBe('')
    })

    it('returns error for non-positive numbers', () => {
      expect(validators.positiveNumber('0')).toBe('Must be a positive number')
      expect(validators.positiveNumber('-1')).toBe('Must be a positive number')
      expect(validators.positiveNumber('abc')).toBe('Must be a positive number')
    })
  })

  describe('validators.url', () => {
    it('returns empty string for valid URLs', () => {
      expect(validators.url('https://example.com')).toBe('')
      expect(validators.url('http://example.com')).toBe('')
      expect(validators.url('https://www.example.com/path')).toBe('')
    })

    it('returns error for invalid URLs', () => {
      expect(validators.url('example.com')).toBe('Must be a valid URL')
      expect(validators.url('ftp://example.com')).toBe('Must be a valid URL')
    })
  })

  describe('validators.match', () => {
    it('returns empty string when values match', () => {
      expect(validators.match('password', 'test123')('test123')).toBe('')
    })

    it('returns error when values do not match', () => {
      expect(validators.match('password', 'test123')('test456')).toBe('Must match password')
    })
  })

  describe('validators.compose', () => {
    it('returns empty string when all validators pass', () => {
      const validator = validators.compose(
        validators.required(),
        validators.minLength(3)
      )
      expect(validator('test')).toBe('')
    })

    it('returns first error when validation fails', () => {
      const validator = validators.compose(
        validators.required(),
        validators.minLength(5)
      )
      expect(validator('')).toBe('This field is required')
      expect(validator('abc')).toBe('Must be at least 5 characters')
    })

    it('stops at first failing validator', () => {
      const validator = validators.compose(
        validators.required(),
        validators.email
      )
      expect(validator('')).toBe('This field is required')
    })
  })

  describe('helper functions', () => {
    it('validateRequired works correctly', () => {
      expect(validateRequired('test')).toBe('')
      expect(validateRequired('')).toBe('This field is required')
      expect(validateRequired('', 'Name')).toBe('Name is required')
    })

    it('validateEmail works correctly', () => {
      expect(validateEmail('test@example.com')).toBe('')
      expect(validateEmail('invalid')).toBe('Invalid email address')
    })

    it('validatePhone works correctly', () => {
      expect(validatePhone('+1234567890')).toBe('')
      expect(validatePhone('123')).toBe('Invalid phone number')
    })

    it('validatePositiveNumber works correctly', () => {
      expect(validatePositiveNumber('100')).toBe('')
      expect(validatePositiveNumber('0')).toBe('Must be a positive number')
    })
  })
})
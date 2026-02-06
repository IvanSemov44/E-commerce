/**
 * Shared validation utilities for form inputs
 * Returns empty string if valid, error message if invalid
 */

export type Validator = (value: string) => string;

export const validators = {
  required: (fieldName: string = 'This field'): Validator => (value: string) =>
    value?.trim() ? '' : `${fieldName} is required`,

  email: (value: string): string =>
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim()) ? '' : 'Invalid email address',

  minLength: (min: number): Validator => (value: string) =>
    value.length >= min ? '' : `Must be at least ${min} characters`,

  maxLength: (max: number): Validator => (value: string) =>
    value.length <= max ? '' : `Must be at most ${max} characters`,

  phone: (value: string): string =>
    /^\+?[\d\s\-()]{10,}$/.test(value.trim()) ? '' : 'Invalid phone number',

  numeric: (value: string): string =>
    /^\d+$/.test(value.trim()) ? '' : 'Must be a number',

  positiveNumber: (value: string): string => {
    const num = parseFloat(value);
    return !isNaN(num) && num > 0 ? '' : 'Must be a positive number';
  },

  url: (value: string): string =>
    /^https?:\/\/.+\..+/.test(value.trim()) ? '' : 'Invalid URL',

  match: (otherValue: string, fieldName: string = 'Field'): Validator => (value: string) =>
    value === otherValue ? '' : `${fieldName} must match`,

  // Compose multiple validators
  compose: (...validators: Validator[]): Validator => (value: string) => {
    for (const validator of validators) {
      const error = validator(value);
      if (error) return error;
    }
    return '';
  },
};

/**
 * Common validation patterns for reuse across forms
 */
export const validateRequired = (value: string, fieldName: string = 'This field'): string =>
  validators.required(fieldName)(value);

export const validateEmail = (value: string): string =>
  validators.email(value);

export const validatePhone = (value: string): string =>
  validators.phone(value);

export const validatePositiveNumber = (value: string): string =>
  validators.positiveNumber(value);

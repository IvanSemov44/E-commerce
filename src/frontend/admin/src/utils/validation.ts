/**
 * Form Validation Utilities
 * Reusable validators for consistent form validation across the application
 */

export type Validator = (value: string) => string;

export const validators = {
  /**
   * Validates that a field is not empty
   * @param fieldName - Custom field name for error message
   * @returns Validator function
   */
  required: (fieldName: string = 'This field'): Validator => (value: string) =>
    value?.trim() ? '' : `${fieldName} is required`,

  /**
   * Validates email format
   */
  email: (value: string): string =>
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim()) ? '' : 'Invalid email address',

  /**
   * Validates minimum length
   */
  minLength: (min: number): Validator => (value: string) =>
    value.length >= min ? '' : `Must be at least ${min} characters`,

  /**
   * Validates maximum length
   */
  maxLength: (max: number): Validator => (value: string) =>
    value.length <= max ? '' : `Must be no more than ${max} characters`,

  /**
   * Validates phone number format
   */
  phone: (value: string): string =>
    /^\+?[\d\s\-()]{10,}$/.test(value.trim()) ? '' : 'Invalid phone number',

  /**
   * Validates numeric input
   */
  numeric: (value: string): string =>
    /^\d+$/.test(value.trim()) ? '' : 'Must be a number',

  /**
   * Validates positive number (including decimals)
   */
  positiveNumber: (value: string): string => {
    const num = parseFloat(value);
    return !isNaN(num) && num > 0 ? '' : 'Must be a positive number';
  },

  /**
   * Validates URL format
   */
  url: (value: string): string =>
    /^https?:\/\/.+/.test(value.trim()) ? '' : 'Must be a valid URL',

  /**
   * Validates that two fields match
   */
  match: (otherFieldName: string, otherValue: string): Validator => (value: string) =>
    value === otherValue ? '' : `Must match ${otherFieldName}`,

  /**
   * Compose multiple validators
   */
  compose: (...validators: Validator[]): Validator => (value: string) => {
    for (const validator of validators) {
      const error = validator(value);
      if (error) return error;
    }
    return '';
  },
};

// Helper functions for common validations
export const validateRequired = (value: string, fieldName?: string): string =>
  validators.required(fieldName)(value);

export const validateEmail = (value: string): string =>
  validators.email(value);

export const validatePhone = (value: string): string =>
  validators.phone(value);

export const validatePositiveNumber = (value: string): string =>
  validators.positiveNumber(value);

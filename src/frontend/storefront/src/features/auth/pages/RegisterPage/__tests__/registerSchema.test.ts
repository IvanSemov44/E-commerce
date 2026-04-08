import { describe, it, expect } from 'vitest';
import i18n from 'i18next';
import { createRegisterSchema } from '../registerSchema';

const t = i18n.t.bind(i18n);

const valid = {
  firstName: 'John',
  lastName: 'Doe',
  email: 'john@example.com',
  password: 'Password1',
  confirmPassword: 'Password1',
  termsAccepted: true,
};

function firstErrors(data: object): Record<string, string> {
  const result = createRegisterSchema(t).safeParse(data);
  if (result.success) return {};
  const map: Record<string, string> = {};
  for (const issue of result.error.issues) {
    const key = String(issue.path[0] ?? '');
    if (!map[key]) map[key] = issue.message;
  }
  return map;
}

describe('createRegisterSchema', () => {
  it('accepts fully valid data', () => {
    expect(createRegisterSchema(t).safeParse(valid).success).toBe(true);
  });

  describe('firstName', () => {
    it('requires a value', () => {
      expect(firstErrors({ ...valid, firstName: '' }).firstName).toBe('First Name is required');
    });

    it('enforces max 50 chars', () => {
      expect(firstErrors({ ...valid, firstName: 'A'.repeat(51) }).firstName).toBe(
        'Maximum 50 characters'
      );
    });

    it('accepts exactly 50 chars', () => {
      expect(firstErrors({ ...valid, firstName: 'A'.repeat(50) }).firstName).toBeUndefined();
    });
  });

  describe('lastName', () => {
    it('requires a value', () => {
      expect(firstErrors({ ...valid, lastName: '' }).lastName).toBe('Last Name is required');
    });

    it('enforces max 50 chars', () => {
      expect(firstErrors({ ...valid, lastName: 'A'.repeat(51) }).lastName).toBe(
        'Maximum 50 characters'
      );
    });
  });

  describe('email', () => {
    it('requires a value', () => {
      expect(firstErrors({ ...valid, email: '' }).email).toBe('Email is required');
    });

    it('rejects an invalid format', () => {
      expect(firstErrors({ ...valid, email: 'not-an-email' }).email).toBe('Invalid email format');
    });

    it('rejects missing domain', () => {
      expect(firstErrors({ ...valid, email: 'user@' }).email).toBe('Invalid email format');
    });

    it('accepts a valid email', () => {
      expect(firstErrors(valid).email).toBeUndefined();
    });
  });

  describe('password', () => {
    it('requires a value — abort stops further checks', () => {
      const errors = firstErrors({ ...valid, password: '', confirmPassword: '' });
      expect(errors.password).toBe('Password is required');
    });

    it('enforces min 8 chars', () => {
      const errors = firstErrors({ ...valid, password: 'Pass1', confirmPassword: 'Pass1' });
      expect(errors.password).toBe('Password must be at least 8 characters');
    });

    it('requires an uppercase letter', () => {
      const errors = firstErrors({ ...valid, password: 'password1', confirmPassword: 'password1' });
      expect(errors.password).toBe('Password must contain an uppercase letter');
    });

    it('requires a lowercase letter', () => {
      const errors = firstErrors({ ...valid, password: 'PASSWORD1', confirmPassword: 'PASSWORD1' });
      expect(errors.password).toBe('Password must contain a lowercase letter');
    });

    it('requires a digit', () => {
      const errors = firstErrors({ ...valid, password: 'PasswordA', confirmPassword: 'PasswordA' });
      expect(errors.password).toBe('Password must contain a digit');
    });
  });

  describe('confirmPassword', () => {
    it('requires a value', () => {
      expect(firstErrors({ ...valid, confirmPassword: '' }).confirmPassword).toBe(
        'Confirm Password is required'
      );
    });

    it('rejects a mismatch', () => {
      expect(firstErrors({ ...valid, confirmPassword: 'Different1' }).confirmPassword).toBe(
        'Passwords do not match'
      );
    });
  });

  describe('termsAccepted', () => {
    it('requires acceptance', () => {
      expect(firstErrors({ ...valid, termsAccepted: false }).termsAccepted).toBe(
        'You must accept the terms to continue'
      );
    });
  });
});

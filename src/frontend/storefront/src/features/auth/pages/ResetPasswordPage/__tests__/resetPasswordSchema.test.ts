import { describe, it, expect } from 'vitest';
import i18n from 'i18next';
import { createResetPasswordSchema } from '../resetPasswordSchema';

const t = i18n.t.bind(i18n);

function firstErrors(data: object): Record<string, string> {
  const result = createResetPasswordSchema(t).safeParse(data);
  if (result.success) return {};
  const map: Record<string, string> = {};
  for (const issue of result.error.issues) {
    const key = String(issue.path[0] ?? '');
    if (!map[key]) map[key] = issue.message;
  }
  return map;
}

const valid = {
  password: 'Password1',
  confirmPassword: 'Password1',
};

describe('createResetPasswordSchema', () => {
  describe('password field', () => {
    it('requires a value', () => {
      const errors = firstErrors({ password: '', confirmPassword: 'Password1' });
      expect(errors.password).toBeDefined();
    });

    it('rejects less than 8 characters', () => {
      const errors = firstErrors({ password: 'Pass1', confirmPassword: 'Pass1' });
      expect(errors.password).toBeDefined();
    });

    it('rejects without uppercase', () => {
      const errors = firstErrors({ password: 'password1', confirmPassword: 'password1' });
      expect(errors.password).toBeDefined();
    });

    it('rejects without lowercase', () => {
      const errors = firstErrors({ password: 'PASSWORD1', confirmPassword: 'PASSWORD1' });
      expect(errors.password).toBeDefined();
    });

    it('rejects without digit', () => {
      const errors = firstErrors({ password: 'Password', confirmPassword: 'Password' });
      expect(errors.password).toBeDefined();
    });
  });

  describe('confirmPassword field', () => {
    it('requires a value', () => {
      const errors = firstErrors({ password: 'Password1', confirmPassword: '' });
      expect(errors.confirmPassword).toBeDefined();
    });

    it('rejects mismatch', () => {
      const errors = firstErrors({ password: 'Password1', confirmPassword: 'Password2' });
      expect(errors.confirmPassword).toBe('Passwords do not match');
    });
  });

  describe('valid data', () => {
    it('accepts valid password and matching confirmPassword', () => {
      const result = createResetPasswordSchema(t).safeParse(valid);
      expect(result.success).toBe(true);
    });
  });
});

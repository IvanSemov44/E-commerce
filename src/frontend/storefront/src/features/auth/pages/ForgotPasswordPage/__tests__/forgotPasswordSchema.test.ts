import { describe, it, expect } from 'vitest';
import i18n from 'i18next';
import { createForgotPasswordSchema } from '../forgotPasswordSchema';

const t = i18n.t.bind(i18n);

const valid = {
  email: 'john@example.com',
};

function firstErrors(data: object): Record<string, string> {
  const result = createForgotPasswordSchema(t).safeParse(data);
  if (result.success) return {};
  const map: Record<string, string> = {};
  for (const issue of result.error.issues) {
    const key = String(issue.path[0] ?? '');
    if (!map[key]) map[key] = issue.message;
  }
  return map;
}

describe('createForgotPasswordSchema', () => {
  it('accepts fully valid data', () => {
    expect(createForgotPasswordSchema(t).safeParse(valid).success).toBe(true);
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
});

import { describe, it, expect } from 'vitest';
import i18n from 'i18next';
import { createLoginSchema } from '../loginSchema';

const t = i18n.t.bind(i18n);

const valid = {
  email: 'john@example.com',
  password: 'password123',
};

function firstErrors(data: object): Record<string, string> {
  const result = createLoginSchema(t).safeParse(data);
  if (result.success) return {};
  const map: Record<string, string> = {};
  for (const issue of result.error.issues) {
    const key = String(issue.path[0] ?? '');
    if (!map[key]) map[key] = issue.message;
  }
  return map;
}

describe('createLoginSchema', () => {
  it('accepts fully valid data', () => {
    expect(createLoginSchema(t).safeParse(valid).success).toBe(true);
  });

  describe('email', () => {
    it('requires a value — abort stops further checks', () => {
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
      expect(firstErrors({ ...valid, password: '' }).password).toBe('Password is required');
    });

    it('accepts any password (no strength rules on login)', () => {
      expect(firstErrors({ ...valid, password: 'a' }).password).toBeUndefined();
    });

    it('accepts an empty-like password for login (unlike registration)', () => {
      expect(firstErrors({ ...valid, password: ' ' }).password).toBeUndefined();
    });
  });
});

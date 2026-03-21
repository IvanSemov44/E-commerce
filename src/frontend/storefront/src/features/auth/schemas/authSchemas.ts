import { z } from 'zod';
import type { TFunction } from 'i18next';

/** Reusable email field: required → valid format. abort stops on first failure. */
export const emailField = (t: TFunction) =>
  z
    .string()
    .min(1, { error: t('auth.emailRequired'), abort: true })
    .check(z.email({ error: t('auth.emailInvalid') }));

/** Reusable strong-password field: required → min 8 → uppercase → lowercase → digit. */
export const passwordField = (t: TFunction) =>
  z
    .string()
    .min(1, { error: t('auth.passwordRequired'), abort: true })
    .min(8, t('auth.passwordMinLength'))
    .regex(/[A-Z]/, t('auth.passwordUppercase'))
    .regex(/[a-z]/, t('auth.passwordLowercase'))
    .regex(/[0-9]/, t('auth.passwordDigit'));

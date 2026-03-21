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

/**
 * Login form schema factory.
 */
export const createLoginSchema = (t: TFunction) =>
  z.object({
    email: emailField(t),
    password: z.string().min(1, t('auth.passwordRequired')),
  });

export type LoginFormValues = z.infer<ReturnType<typeof createLoginSchema>>;

/**
 * Forgot password form schema factory.
 */
export const createForgotPasswordSchema = (t: TFunction) =>
  z.object({
    email: emailField(t),
  });

export type ForgotPasswordFormValues = z.infer<ReturnType<typeof createForgotPasswordSchema>>;

/**
 * Reset password form schema factory.
 */
export const createResetPasswordSchema = (t: TFunction) =>
  z
    .object({
      password: passwordField(t),
      confirmPassword: z.string().min(1, t('auth.confirmPasswordRequired')),
    })
    .refine((data) => data.password === data.confirmPassword, {
      message: t('auth.passwordsDoNotMatch'),
      path: ['confirmPassword'],
    });

export type ResetPasswordFormValues = z.infer<ReturnType<typeof createResetPasswordSchema>>;

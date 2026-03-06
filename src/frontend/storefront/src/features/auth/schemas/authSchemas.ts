import { z } from 'zod';
import type { TFunction } from 'i18next';

/**
 * Login form schema factory.
 * Accepts the i18n `t` function so error messages are translated.
 */
export const createLoginSchema = (t: TFunction) =>
  z.object({
    email: z.string().min(1, t('auth.emailRequired')),
    password: z.string().min(1, t('auth.passwordRequired')),
  });

export type LoginFormValues = z.infer<ReturnType<typeof createLoginSchema>>;

/**
 * Register form schema factory.
 * Accepts the i18n `t` function so error messages are translated.
 */
export const createRegisterSchema = (t: TFunction) =>
  z
    .object({
      firstName: z
        .string()
        .min(1, `${t('profile.firstName')} ${t('common.required').toLowerCase()}`),
      lastName: z
        .string()
        .min(1, `${t('profile.lastName')} ${t('common.required').toLowerCase()}`),
      email: z.string().min(1, t('auth.emailRequired')),
      password: z.string().min(1, t('auth.passwordRequired')),
      confirmPassword: z
        .string()
        .min(1, `${t('auth.confirmPassword')} ${t('common.required').toLowerCase()}`),
    })
    .refine((data) => data.password === data.confirmPassword, {
      message: t('auth.passwordsDoNotMatch'),
      path: ['confirmPassword'],
    });

export type RegisterFormValues = z.infer<ReturnType<typeof createRegisterSchema>>;

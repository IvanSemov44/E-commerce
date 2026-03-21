import { z } from 'zod';
import type { TFunction } from 'i18next';
import { emailField, passwordField } from '@/features/auth/schemas/authSchemas';

export const createRegisterSchema = (t: TFunction) =>
  z
    .object({
      firstName: z
        .string()
        .min(1, {
          error: `${t('profile.firstName')} ${t('common.required').toLowerCase()}`,
          abort: true,
        })
        .max(50, t('auth.nameMaxLength', { max: 50 })),
      lastName: z
        .string()
        .min(1, {
          error: `${t('profile.lastName')} ${t('common.required').toLowerCase()}`,
          abort: true,
        })
        .max(50, t('auth.nameMaxLength', { max: 50 })),
      email: emailField(t),
      password: passwordField(t),
      confirmPassword: z
        .string()
        .min(1, `${t('auth.confirmPassword')} ${t('common.required').toLowerCase()}`),
      termsAccepted: z.boolean(),
    })
    .refine((data) => data.password === data.confirmPassword, {
      message: t('auth.passwordsDoNotMatch'),
      path: ['confirmPassword'],
    })
    .refine((data) => data.termsAccepted === true, {
      message: t('auth.termsRequired'),
      path: ['termsAccepted'],
    });

export type RegisterFormValues = z.infer<ReturnType<typeof createRegisterSchema>>;

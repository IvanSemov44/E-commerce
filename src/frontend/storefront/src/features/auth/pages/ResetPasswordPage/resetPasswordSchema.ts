import { z } from 'zod';
import type { TFunction } from 'i18next';
import { passwordField } from '@/features/auth/schemas/authSchemas';

export const createResetPasswordSchema = (t: TFunction) =>
  z
    .object({
      password: passwordField(t),
      confirmPassword: z.string().min(1, {
        error: `${t('auth.confirmPassword')} ${t('common.required').toLowerCase()}`,
        abort: true,
      }),
    })
    .refine((data) => data.password === data.confirmPassword, {
      message: t('auth.passwordsDoNotMatch'),
      path: ['confirmPassword'],
    });

export type ResetPasswordFormValues = z.infer<ReturnType<typeof createResetPasswordSchema>>;

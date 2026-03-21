import { z } from 'zod';
import type { TFunction } from 'i18next';
import { emailField } from '@/features/auth/schemas/authSchemas';

export const createLoginSchema = (t: TFunction) =>
  z.object({
    email: emailField(t),
    password: z.string().min(1, { error: t('auth.passwordRequired'), abort: true }),
  });

export type LoginFormValues = z.infer<ReturnType<typeof createLoginSchema>>;

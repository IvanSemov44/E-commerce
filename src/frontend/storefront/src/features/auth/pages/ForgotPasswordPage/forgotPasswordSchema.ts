import { z } from 'zod';
import type { TFunction } from 'i18next';
import { emailField } from '@/features/auth/schemas/authSchemas';

export const createForgotPasswordSchema = (t: TFunction) => z.object({ email: emailField(t) });

export type ForgotPasswordFormValues = z.infer<ReturnType<typeof createForgotPasswordSchema>>;

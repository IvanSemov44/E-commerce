import { z } from 'zod';
import type { TFunction } from 'i18next';

export const createProfileSchema = (t: TFunction) =>
  z.object({
    firstName: z
      .string()
      .min(1, { error: t('validation.isRequired', { field: t('profile.firstName') }), abort: true })
      .max(50, t('auth.nameMaxLength', { max: 50 })),
    lastName: z
      .string()
      .min(1, { error: t('validation.isRequired', { field: t('profile.lastName') }), abort: true })
      .max(50, t('auth.nameMaxLength', { max: 50 })),
    email: z.string(),
    phone: z
      .string()
      .refine((val) => !val || !val.trim() || /^\+?[\d\s\-()]{10,}$/.test(val.trim()), {
        message: t('validation.phoneInvalid'),
      })
      .optional(),
    avatarUrl: z
      .string()
      .refine(
        (val) => {
          if (!val || !val.trim()) return true;
          try {
            new URL(val);
            return true;
          } catch {
            return false;
          }
        },
        { message: t('profile.invalidImageUrl') }
      )
      .optional(),
  });

export type ProfileFormValues = z.infer<ReturnType<typeof createProfileSchema>>;

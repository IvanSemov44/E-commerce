import { z } from 'zod';

/**
 * Profile form schema.
 * - firstName and lastName are required.
 * - phone is optional but must match E.164-ish format when provided.
 * - email is read-only in the UI so it carries no validation constraint.
 * - avatarUrl is freeform, no validation.
 */
export const profileSchema = z.object({
  firstName: z.string().min(1, 'First name is required'),
  lastName: z.string().min(1, 'Last name is required'),
  email: z.string(),
  phone: z
    .string()
    .refine(
      (val) => !val || !val.trim() || /^\+?[\d\s\-()]{10,}$/.test(val.trim()),
      { message: 'Invalid phone number' }
    )
    .optional(),
  avatarUrl: z.string().optional(),
});

export type ProfileFormValues = z.infer<typeof profileSchema>;

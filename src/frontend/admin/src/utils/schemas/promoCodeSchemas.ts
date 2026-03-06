import { z } from 'zod';

const POSITIVE_NUMBER_MSG = 'Must be a positive number';

/**
 * Admin promo code form schema.
 * Numeric values are stored as strings in the form and coerced on submit.
 */
export const promoCodeSchema = z.object({
  code: z.string().min(1, 'Promo code is required'),
  discountType: z.string().min(1, 'Discount type is required'),
  discountValue: z
    .string()
    .min(1, 'Discount value is required')
    .refine(
      (val) => { const n = parseFloat(val); return !isNaN(n) && n > 0; },
      { message: POSITIVE_NUMBER_MSG }
    ),
  minOrderAmount: z
    .string()
    .refine(
      (val) => {
        if (!val || !val.trim()) return true;
        const n = parseFloat(val);
        return !isNaN(n) && n > 0;
      },
      { message: POSITIVE_NUMBER_MSG }
    )
    .optional(),
  maxDiscountAmount: z
    .string()
    .refine(
      (val) => {
        if (!val || !val.trim()) return true;
        const n = parseFloat(val);
        return !isNaN(n) && n > 0;
      },
      { message: POSITIVE_NUMBER_MSG }
    )
    .optional(),
  maxUses: z
    .string()
    .refine(
      (val) => !val || !val.trim() || /^\d+$/.test(val.trim()),
      { message: 'Max uses must be a whole number' }
    )
    .optional(),
  startDate: z.string().optional(),
  endDate: z.string().optional(),
  isActive: z.boolean(),
}).superRefine((data, ctx) => {
  // Percentage discount cannot exceed 100
  if (data.discountType === 'percentage' && data.discountValue) {
    const val = parseFloat(data.discountValue);
    if (!isNaN(val) && val > 100) {
      ctx.addIssue({
        code: 'custom',
        path: ['discountValue'],
        message: 'Percentage cannot exceed 100%',
      });
    }
  }
});

export type PromoCodeFormValues = z.infer<typeof promoCodeSchema>;

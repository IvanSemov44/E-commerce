import { z } from 'zod';

/**
 * Admin product form schema.
 * Form stores price/stock as strings (controlled inputs); they are coerced to
 * numbers on submit, so validation operates on the raw string values.
 */
export const productSchema = z.object({
  name: z.string().min(1, 'Product name is required'),
  slug: z.string().min(1, 'Slug is required'),
  description: z.string(),
  shortDescription: z.string(),
  price: z
    .string()
    .min(1, 'Price is required')
    .refine(
      (val) => { const n = parseFloat(val); return !isNaN(n) && n > 0; },
      { message: 'Must be a positive number' }
    ),
  compareAtPrice: z
    .string()
    .refine(
      (val) => {
        if (!val || !val.trim()) return true;
        const n = parseFloat(val);
        return !isNaN(n) && n > 0;
      },
      { message: 'Must be a positive number' }
    )
    .optional(),
  stockQuantity: z
    .string()
    .min(1, 'Stock quantity is required')
    .refine(
      (val) => /^\d+$/.test(val.trim()),
      { message: 'Stock quantity must be a whole number' }
    ),
  categoryId: z.string().min(1, 'Category is required'),
  isFeatured: z.boolean(),
});

export type ProductFormValues = z.infer<typeof productSchema>;

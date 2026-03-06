import { z } from 'zod';

/**
 * Checkout / shipping address form schema.
 * All fields are required; email and phone must also match format rules.
 */
export const checkoutSchema = z.object({
  firstName: z.string().min(1, 'First name is required'),
  lastName: z.string().min(1, 'Last name is required'),
  email: z
    .string()
    .min(1, 'Email is required')
    .email('Invalid email address'),
  phone: z
    .string()
    .min(1, 'Phone is required')
    .regex(/^\+?[\d\s\-()]{10,}$/, 'Invalid phone number'),
  streetLine1: z.string().min(1, 'Street address is required'),
  city: z.string().min(1, 'City is required'),
  state: z.string().min(1, 'State is required'),
  postalCode: z.string().min(1, 'Postal code is required'),
  country: z.string().min(1, 'Country is required'),
});

export type CheckoutFormValues = z.infer<typeof checkoutSchema>;

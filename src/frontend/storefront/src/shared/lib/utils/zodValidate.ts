import type { ZodSchema } from 'zod';

/**
 * Bridges a Zod schema to the validate function signature expected by useForm.
 * Converts Zod validation errors into a flat Record<keyof T, string> object.
 *
 * @example
 * const form = useForm({
 *   validate: zodValidate(loginSchema),
 *   ...
 * });
 */
export function zodValidate<T extends Record<string, unknown>>(
  schema: ZodSchema<T>
): (values: T) => Partial<Record<keyof T, string>> {
  return (values: T) => {
    const result = schema.safeParse(values);
    if (result.success) return {};

    const errors: Partial<Record<keyof T, string>> = {};
    for (const issue of result.error.issues) {
      const field = issue.path[0] as keyof T;
      // Only record the first error per field
      if (field !== undefined && !errors[field]) {
        errors[field] = issue.message;
      }
    }
    return errors;
  };
}

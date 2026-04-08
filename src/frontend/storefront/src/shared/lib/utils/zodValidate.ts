import { ZodType } from 'zod';

/**
 * Adapts a Zod schema into the validate function expected by useForm.
 * Returns field-level errors keyed by field name.
 */
export function zodValidate<T extends object>(
  schema: ZodType<T>
): (values: T) => Partial<Record<keyof T, string>> {
  return (values) => {
    const result = schema.safeParse(values);
    if (result.success) return {};

    const errors: Partial<Record<keyof T, string>> = {};
    for (const issue of result.error.issues) {
      const key = issue.path[0] as keyof T;
      if (key && !errors[key]) {
        errors[key] = issue.message;
      }
    }
    return errors;
  };
}

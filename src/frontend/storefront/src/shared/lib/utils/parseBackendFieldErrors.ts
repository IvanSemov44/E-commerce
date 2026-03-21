/**
 * Parses RTK Query error responses into a flat field-error map.
 *
 * Handles two ASP.NET backend error shapes:
 *   - Validation errors (400):  { errors: { Password: ["msg"] } }  → PascalCase→camelCase field map
 *   - Business errors (409/4xx): { errorDetails: { code, message } } → mapped via codeToField
 *
 * Returns null when the error doesn't match either shape so the caller
 * can fall back to a generic toast.
 */
export function parseBackendFieldErrors<TField extends string>(
  err: unknown,
  codeToField: Partial<Record<string, TField>>
): Partial<Record<TField, string>> | null {
  if (typeof err !== 'object' || err === null || !('data' in err)) return null;

  const data = (err as { data: Record<string, unknown> }).data;

  // ASP.NET validation errors: { errors: { FieldName: ["msg1", "msg2"] } }
  if (data.errors && typeof data.errors === 'object') {
    const result: Partial<Record<TField, string>> = {};
    for (const [key, msgs] of Object.entries(data.errors as Record<string, string[]>)) {
      const field = (key.charAt(0).toLowerCase() + key.slice(1)) as TField;
      if (Array.isArray(msgs) && msgs.length > 0) result[field] = msgs.join('. ');
    }
    return Object.keys(result).length > 0 ? result : null;
  }

  // Business errors: { errorDetails: { code: "DUPLICATE_EMAIL", message: "..." } }
  const details = data.errorDetails as { code?: string; message?: string } | undefined;
  if (details?.code && details.message) {
    const field = codeToField[details.code];
    if (field) return { [field]: details.message } as Partial<Record<TField, string>>;
  }

  return null;
}

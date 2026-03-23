export function isApiError(value: unknown): value is { errorDetails: { message: string } } {
  return (
    typeof value === 'object' &&
    value !== null &&
    'errorDetails' in value &&
    typeof (value as Record<string, unknown>).errorDetails === 'object' &&
    (value as Record<string, unknown>).errorDetails !== null &&
    typeof ((value as Record<string, unknown>).errorDetails as Record<string, unknown>).message ===
      'string'
  );
}

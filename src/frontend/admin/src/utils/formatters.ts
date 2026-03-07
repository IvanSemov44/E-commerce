export function formatCurrency(amount: number): string {
  return `$${amount.toFixed(2)}`;
}

export function formatDate(dateString: string | undefined, fallback = 'N/A'): string {
  if (!dateString) return fallback;
  return new Date(dateString).toLocaleDateString();
}

export function formatDateTime(dateString: string): string {
  return new Date(dateString).toLocaleString();
}

export function getErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof Object && 'data' in err) {
    const data = (err as Record<string, unknown>).data;
    if (data instanceof Object && 'message' in data) {
      return (data as Record<string, unknown>).message as string;
    }
  }
  if (err instanceof Error) return err.message;
  return fallback;
}

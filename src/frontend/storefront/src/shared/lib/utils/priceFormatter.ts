export function formatPrice(amount: number): string {
  return `$${amount.toFixed(2)}`;
}

export function formatPriceLocale(amount: number, locale = 'en-US'): string {
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: 'USD',
  }).format(amount);
}

import type { Address } from '@/shared/types';

/**
 * Format address into a single line string
 * @param address - Address object
 * @returns Formatted address string
 */
export function formatAddressLine(address: Address): string {
  const parts: string[] = [];

  if (address.streetLine1) parts.push(address.streetLine1);
  if (address.streetLine2) parts.push(address.streetLine2);
  if (address.city) parts.push(address.city);
  if (address.state) parts.push(address.state);
  if (address.postalCode) parts.push(address.postalCode);
  if (address.country) parts.push(address.country);

  return parts.join(', ');
}

/**
 * Get full name from address
 * @param address - Address object
 * @returns Full name string
 */
export function getFullName(address: Address): string {
  return `${address.firstName} ${address.lastName}`.trim();
}

/**
 * Get city, state, postal code line
 * @param address - Address object
 * @returns Formatted city/state/zip string
 */
export function getCityStateLine(address: Address): string {
  const parts: string[] = [];

  if (address.city) parts.push(address.city);
  if (address.state) parts.push(address.state);
  if (address.postalCode) parts.push(address.postalCode);

  return parts.join(', ');
}

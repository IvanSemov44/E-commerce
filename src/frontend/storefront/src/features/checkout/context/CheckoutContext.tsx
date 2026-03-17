import { createContext, useContext } from 'react';
import { useCheckout } from '../hooks/useCheckout';
import type { UseCheckoutReturn } from '../checkout.types';

export const CheckoutContext = createContext<UseCheckoutReturn | null>(null);

export function CheckoutProvider({ children }: { children: React.ReactNode }) {
  const checkout = useCheckout();
  return <CheckoutContext.Provider value={checkout}>{children}</CheckoutContext.Provider>;
}

export function useCheckoutContext(): UseCheckoutReturn {
  const ctx = useContext(CheckoutContext);
  if (ctx === null) {
    throw new Error('useCheckoutContext must be used within a CheckoutProvider');
  }
  return ctx;
}

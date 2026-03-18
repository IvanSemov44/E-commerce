/**
 * CheckoutForm Hooks
 * Component-specific hook for shipping form state, validation,
 * localStorage draft persistence, and authenticated user pre-fill
 */

import { useEffect, useMemo } from 'react';
import { useAppSelector } from '@/shared/lib/store';
import { selectIsAuthenticated, selectCurrentUser } from '@/features/auth/slices/authSlice';
import { useLocalStorage } from '@/shared/hooks/useLocalStorage';
import { useForm } from '@/shared/hooks/useForm';
import { zodValidate } from '@/shared/lib/utils/zodValidate';
import { checkoutSchema } from '@/features/checkout/checkoutSchemas';
import type { ShippingFormData } from '@/features/checkout/types';
import { CHECKOUT_DRAFT_KEY } from '@/features/checkout/constants';

interface UseCheckoutFormOptions {
  onSubmit: (values: ShippingFormData) => void | Promise<void>;
}

// All fields empty — used as the base for initial value composition below
const EMPTY_FORM: ShippingFormData = {
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  streetLine1: '',
  city: '',
  state: '',
  postalCode: '',
  country: '',
};

export function useCheckoutForm(options: UseCheckoutFormOptions) {
  const { onSubmit } = options;
  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  const user = useAppSelector(selectCurrentUser);

  // Reads the saved draft from localStorage (partial — user may have left mid-fill)
  const [shippingDraft, setShippingDraft] = useLocalStorage<Partial<ShippingFormData>>(
    CHECKOUT_DRAFT_KEY,
    {} as Partial<ShippingFormData>
  );

  // Resolve initial values once on mount using this priority order:
  //   1. Saved draft  — restores whatever the user typed last time
  //   2. User profile — pre-fills contact fields for authenticated users with no draft
  //   3. Empty form   — fresh guest checkout
  // Empty deps are intentional: mirrors useState(initialValues) which also only reads
  // initialValues on the first render. Adding deps would recompute but never apply the result.
  const initialValues = useMemo((): ShippingFormData => {
    let values = EMPTY_FORM;

    if (isAuthenticated && user) {
      values = {
        ...EMPTY_FORM,
        firstName: user.firstName || '',
        lastName: user.lastName || '',
        email: user.email || '',
        phone: user.phone || '',
      };
    }

    if (Object.values(shippingDraft).some(Boolean)) {
      values = { ...EMPTY_FORM, ...shippingDraft };
    }

    return values;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const form = useForm<ShippingFormData>({
    initialValues,
    validate: zodValidate(checkoutSchema),
    onSubmit,
  });

  // Save form values to localStorage after 500ms of inactivity — avoids a write on every keystroke
  useEffect(() => {
    const timer = setTimeout(() => setShippingDraft(form.values), 500);
    return () => clearTimeout(timer); // cancel pending write if values change before timer fires
  }, [form.values, setShippingDraft]);

  return { form };
}

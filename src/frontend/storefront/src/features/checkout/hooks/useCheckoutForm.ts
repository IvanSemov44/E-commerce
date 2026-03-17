/**
 * useCheckoutForm Hook
 * Manages shipping form state, validation, localStorage draft persistence,
 * and authenticated user pre-fill
 */

import { useEffect, useRef } from 'react';
import { useAppSelector } from '@/shared/lib/store';
import type { RootState } from '@/shared/lib/store';
import { useLocalStorage } from '@/shared/hooks/useLocalStorage';
import useForm from '@/shared/hooks/useForm';
import { zodValidate } from '@/shared/lib/utils/zodValidate';
import { checkoutSchema } from '../schemas/checkoutSchemas';
import type { ShippingFormData } from '../checkout.types';
import { CHECKOUT_DRAFT_KEY } from '../checkout.types';

const selectIsAuthenticated = (state: RootState) => state.auth.isAuthenticated;
const selectUser = (state: RootState) => state.auth.user;

interface UseCheckoutFormOptions {
  onSubmit?: (values: ShippingFormData) => void | Promise<void>;
}

interface UseCheckoutFormReturn {
  form: ReturnType<typeof useForm<ShippingFormData>>;
  shippingDraft: Partial<ShippingFormData>;
}

export function useCheckoutForm(options?: UseCheckoutFormOptions): UseCheckoutFormReturn {
  const { onSubmit } = options ?? {};
  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  const user = useAppSelector(selectUser);

  // Shipping form draft persisted in localStorage — auto-restored on mount
  const [shippingDraft, setShippingDraft] = useLocalStorage<Partial<ShippingFormData>>(
    CHECKOUT_DRAFT_KEY,
    {}
  );

  // Use ref to track if we've already pre-filled to avoid overwriting user edits
  const hasPrefilledRef = useRef(false);

  // Initialize useForm hook — restore any previously saved draft
  const form = useForm<ShippingFormData>({
    initialValues: {
      firstName: shippingDraft.firstName ?? '',
      lastName: shippingDraft.lastName ?? '',
      email: shippingDraft.email ?? '',
      phone: shippingDraft.phone ?? '',
      streetLine1: shippingDraft.streetLine1 ?? '',
      city: shippingDraft.city ?? '',
      state: shippingDraft.state ?? '',
      postalCode: shippingDraft.postalCode ?? '',
      country: shippingDraft.country ?? '',
    },
    validate: zodValidate(checkoutSchema),
    onSubmit: onSubmit ?? (() => {}),
  });

  // Persist form values to localStorage as a draft on every change
  useEffect(() => {
    setShippingDraft(form.values);
  }, [form.values, setShippingDraft]);

  // Pre-fill form with user data when authenticated
  useEffect(() => {
    if (isAuthenticated && user && !hasPrefilledRef.current) {
      // Only pre-fill if the form is empty
      const isFormEmpty = !form.values.firstName && !form.values.lastName && !form.values.email;

      if (isFormEmpty) {
        form.setValues({
          ...form.values,
          firstName: user.firstName || '',
          lastName: user.lastName || '',
          email: user.email || '',
          phone: user.phone || '',
        });
        hasPrefilledRef.current = true;
      }
    }
  }, [isAuthenticated, user, form]);

  return {
    form,
    shippingDraft,
  };
}

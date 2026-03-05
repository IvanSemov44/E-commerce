import type { ShippingFormData } from './CheckoutForm.types';

/**
 * Hook for handling checkout form field updates
 * @param formData - Current form data
 * @param onFormDataChange - Callback when form data changes
 * @returns Object with handleChange function
 */
export function useCheckoutFormHandling(
  formData: ShippingFormData,
  onFormDataChange: (data: Partial<ShippingFormData>) => void
) {
  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    onFormDataChange({ ...formData, [name]: value });
  };

  return { handleChange };
}

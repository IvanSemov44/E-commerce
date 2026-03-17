import type { ShippingFormData } from '../../checkout.types';

export type { ShippingFormData };

export interface CheckoutFormProps {
  formData: ShippingFormData;
  errors: Record<string, string>;
  onFormDataChange: (data: Partial<ShippingFormData>) => void;
  onSubmit: (e: React.FormEvent) => void;
  selectedPaymentMethod: string;
  onPaymentMethodChange: (method: string) => void;
}

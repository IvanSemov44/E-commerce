export interface ShippingFormData {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  streetLine1: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface CheckoutFormProps {
  formData: ShippingFormData;
  errors: Record<string, string>;
  onFormDataChange: (data: Partial<ShippingFormData>) => void;
  onSubmit: (e: React.FormEvent) => void;
  isAuthenticated: boolean;
  selectedPaymentMethod: string;
  onPaymentMethodChange: (method: string) => void;
}

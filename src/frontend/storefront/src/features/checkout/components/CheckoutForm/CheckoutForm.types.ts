import type { ShippingFormData } from '../../checkout.types';

export interface CheckoutFormProps {
  onSubmit: (values: ShippingFormData) => void | Promise<void>;
  payment: {
    method: string;
    onChange: (method: string) => void;
  };
}

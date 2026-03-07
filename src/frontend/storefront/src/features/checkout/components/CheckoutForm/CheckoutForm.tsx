import { useTranslation } from 'react-i18next';
import Button from '@/shared/components/ui/Button';
import Input from '@/shared/components/ui/Input';
import PaymentMethodSelector from '../PaymentMethodSelector/PaymentMethodSelector';
import type { CheckoutFormProps } from './CheckoutForm.types';

export default function CheckoutForm({
  formData,
  errors,
  onFormDataChange,
  onSubmit,
  selectedPaymentMethod,
  onPaymentMethodChange,
}: CheckoutFormProps) {
  const { t } = useTranslation();

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    onFormDataChange({ ...formData, [name]: value });
  };

  return (
    <form
      onSubmit={onSubmit}
      className="space-y-6"
      aria-label={t('checkout.shippingInfo')}
      noValidate
    >
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Input
          label={t('checkout.firstName')}
          type="text"
          id="firstName"
          name="firstName"
          value={formData.firstName}
          onChange={handleChange}
          error={errors.firstName}
          required
        />
        <Input
          label={t('checkout.lastName')}
          type="text"
          id="lastName"
          name="lastName"
          value={formData.lastName}
          onChange={handleChange}
          error={errors.lastName}
          required
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Input
          label={t('checkout.email')}
          type="email"
          id="email"
          name="email"
          value={formData.email}
          onChange={handleChange}
          error={errors.email}
          required
        />
        <Input
          label={t('checkout.phone')}
          type="tel"
          id="phone"
          name="phone"
          value={formData.phone}
          onChange={handleChange}
          error={errors.phone}
        />
      </div>

      <Input
        label={t('checkout.address')}
        type="text"
        id="streetLine1"
        name="streetLine1"
        value={formData.streetLine1}
        onChange={handleChange}
        error={errors.streetLine1}
        required
      />

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="col-span-2">
          <Input
            label={t('checkout.city')}
            type="text"
            id="city"
            name="city"
            value={formData.city}
            onChange={handleChange}
            error={errors.city}
            required
          />
        </div>
        <Input
          label={t('checkout.state') || 'State'}
          type="text"
          id="state"
          name="state"
          value={formData.state}
          onChange={handleChange}
          error={errors.state}
          required
        />
        <Input
          label={t('checkout.postalCode')}
          type="text"
          id="postalCode"
          name="postalCode"
          value={formData.postalCode}
          onChange={handleChange}
          error={errors.postalCode}
          required
        />
      </div>

      <div>
        <label htmlFor="country" className="block text-sm font-medium mb-1">
          {t('checkout.country')}
        </label>
        <select
          id="country"
          name="country"
          value={formData.country}
          onChange={handleChange}
          className="w-full px-3 py-2 border rounded-lg"
          required
          aria-required="true"
          aria-describedby={errors.country ? 'country-error' : undefined}
        >
          <option value="">{t('checkout.selectCountry')}</option>
          <option value="US">United States</option>
          <option value="CA">Canada</option>
          <option value="UK">United Kingdom</option>
          <option value="DE">Germany</option>
          <option value="FR">France</option>
        </select>
        {errors.country && <p id="country-error" role="alert" className="text-red-500 text-sm mt-1">{errors.country}</p>}
      </div>

      <PaymentMethodSelector
        selectedMethod={selectedPaymentMethod}
        onMethodChange={onPaymentMethodChange}
      />

      <Button type="submit" className="w-full">
        {t('checkout.placeOrder')}
      </Button>
    </form>
  );
}

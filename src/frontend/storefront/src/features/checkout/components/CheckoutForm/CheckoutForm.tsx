import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/components/ui/Button';
import { Input } from '@/shared/components/ui/Input';
import PaymentMethodSelector from '../PaymentMethodSelector/PaymentMethodSelector';
import { useCheckoutForm } from './CheckoutForm.hooks';
import { COUNTRIES } from '../../constants/countries';
import type { CheckoutFormProps } from './CheckoutForm.types';

export default function CheckoutForm({ onSubmit, payment }: CheckoutFormProps) {
  const { t } = useTranslation();
  const { form } = useCheckoutForm({ onSubmit });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    form.setValues({ ...form.values, [name]: value });
  };

  return (
    <form
      onSubmit={form.handleSubmit}
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
          value={form.values.firstName}
          onChange={handleChange}
          error={form.errors.firstName}
          required
        />
        <Input
          label={t('checkout.lastName')}
          type="text"
          id="lastName"
          name="lastName"
          value={form.values.lastName}
          onChange={handleChange}
          error={form.errors.lastName}
          required
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Input
          label={t('checkout.email')}
          type="email"
          id="email"
          name="email"
          value={form.values.email}
          onChange={handleChange}
          error={form.errors.email}
          required
        />
        <Input
          label={t('checkout.phone')}
          type="tel"
          id="phone"
          name="phone"
          value={form.values.phone}
          onChange={handleChange}
          error={form.errors.phone}
        />
      </div>

      <Input
        label={t('checkout.address')}
        type="text"
        id="streetLine1"
        name="streetLine1"
        value={form.values.streetLine1}
        onChange={handleChange}
        error={form.errors.streetLine1}
        required
      />

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="col-span-2">
          <Input
            label={t('checkout.city')}
            type="text"
            id="city"
            name="city"
            value={form.values.city}
            onChange={handleChange}
            error={form.errors.city}
            required
          />
        </div>
        <Input
          label={t('checkout.state')}
          type="text"
          id="state"
          name="state"
          value={form.values.state}
          onChange={handleChange}
          error={form.errors.state}
          required
        />
        <Input
          label={t('checkout.postalCode')}
          type="text"
          id="postalCode"
          name="postalCode"
          value={form.values.postalCode}
          onChange={handleChange}
          error={form.errors.postalCode}
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
          value={form.values.country}
          onChange={handleChange}
          className="w-full px-3 py-2 border rounded-lg"
          required
          aria-required="true"
          aria-describedby={form.errors.country ? 'country-error' : undefined}
        >
          <option value="">{t('checkout.selectCountry')}</option>
          {COUNTRIES.map((country) => (
            <option key={country.code} value={country.code}>
              {country.name}
            </option>
          ))}
        </select>
        {form.errors.country && (
          <p id="country-error" role="alert" className="text-red-500 text-sm mt-1">
            {form.errors.country}
          </p>
        )}
      </div>

      <PaymentMethodSelector selectedMethod={payment.method} onMethodChange={payment.onChange} />

      <Button type="submit" className="w-full">
        {t('checkout.placeOrder')}
      </Button>
    </form>
  );
}

import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/components/ui/Button';
import { Input } from '@/shared/components/ui/Input';
import { PaymentMethodSelector } from '@/features/checkout/components/PaymentMethodSelector';
import { useCheckoutForm } from './CheckoutForm.hooks';
import { COUNTRIES } from '@/features/checkout/constants';
import type { ShippingFormData } from '@/features/checkout/types';
import styles from './CheckoutForm.module.css';

interface CheckoutFormProps {
  onSubmit: (values: ShippingFormData) => void | Promise<void>;
  payment: {
    method: string;
    onChange: (method: string) => void;
  };
}

export function CheckoutForm({ onSubmit, payment }: CheckoutFormProps) {
  const { t } = useTranslation();
  const { form } = useCheckoutForm({ onSubmit });

  return (
    <form
      onSubmit={form.handleSubmit}
      className={styles.form}
      aria-label={t('checkout.shippingInfo')}
      noValidate
    >
      <div className={styles.row}>
        <Input
          label={t('checkout.firstName')}
          type="text"
          id="firstName"
          name="firstName"
          value={form.values.firstName}
          onChange={form.handleChange}
          error={form.errors.firstName}
          required
        />
        <Input
          label={t('checkout.lastName')}
          type="text"
          id="lastName"
          name="lastName"
          value={form.values.lastName}
          onChange={form.handleChange}
          error={form.errors.lastName}
          required
        />
      </div>

      <div className={styles.row}>
        <Input
          label={t('checkout.email')}
          type="email"
          id="email"
          name="email"
          value={form.values.email}
          onChange={form.handleChange}
          error={form.errors.email}
          required
        />
        <Input
          label={t('checkout.phone')}
          type="tel"
          id="phone"
          name="phone"
          value={form.values.phone}
          onChange={form.handleChange}
          error={form.errors.phone}
          required
        />
      </div>

      <Input
        label={t('checkout.address')}
        type="text"
        id="streetLine1"
        name="streetLine1"
        value={form.values.streetLine1}
        onChange={form.handleChange}
        error={form.errors.streetLine1}
        required
      />

      <div className={styles.addressRow}>
        <Input
          label={t('checkout.city')}
          type="text"
          id="city"
          name="city"
          value={form.values.city}
          onChange={form.handleChange}
          error={form.errors.city}
          required
        />
        <Input
          label={t('checkout.state')}
          type="text"
          id="state"
          name="state"
          value={form.values.state}
          onChange={form.handleChange}
          error={form.errors.state}
          required
        />
        <Input
          label={t('checkout.postalCode')}
          type="text"
          id="postalCode"
          name="postalCode"
          value={form.values.postalCode}
          onChange={form.handleChange}
          error={form.errors.postalCode}
          required
        />
      </div>

      <div className={styles.fieldGroup}>
        <label htmlFor="country" className={styles.label}>
          {t('checkout.country')}
        </label>
        <select
          id="country"
          name="country"
          value={form.values.country}
          onChange={form.handleChange}
          className={styles.select}
          required
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
          <p id="country-error" role="alert" className={styles.fieldError}>
            {form.errors.country}
          </p>
        )}
      </div>

      <PaymentMethodSelector selectedMethod={payment.method} onMethodChange={payment.onChange} />

      <Button type="submit" className={styles.submitButton} disabled={form.isSubmitting}>
        {form.isSubmitting ? t('common.loading') : t('checkout.placeOrder')}
      </Button>
    </form>
  );
}

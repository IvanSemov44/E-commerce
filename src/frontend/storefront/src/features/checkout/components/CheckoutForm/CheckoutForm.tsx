import { useTranslation } from 'react-i18next';
import Button from '@/shared/components/ui/Button';
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
        <div>
          <label htmlFor="firstName" className="block text-sm font-medium mb-1">
            {t('checkout.firstName')}
          </label>
          <input
            type="text"
            id="firstName"
            name="firstName"
            value={formData.firstName}
            onChange={handleChange}
            className="w-full px-3 py-2 border rounded-lg"
            required
            aria-required="true"
            aria-describedby={errors.firstName ? 'firstName-error' : undefined}
          />
          {errors.firstName && <p id="firstName-error" role="alert" className="text-red-500 text-sm mt-1">{errors.firstName}</p>}
        </div>
        <div>
          <label htmlFor="lastName" className="block text-sm font-medium mb-1">
            {t('checkout.lastName')}
          </label>
          <input
            type="text"
            id="lastName"
            name="lastName"
            value={formData.lastName}
            onChange={handleChange}
            className="w-full px-3 py-2 border rounded-lg"
            required
            aria-required="true"
            aria-describedby={errors.lastName ? 'lastName-error' : undefined}
          />
          {errors.lastName && <p id="lastName-error" role="alert" className="text-red-500 text-sm mt-1">{errors.lastName}</p>}
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="email" className="block text-sm font-medium mb-1">
            {t('checkout.email')}
          </label>
          <input
            type="email"
            id="email"
            name="email"
            value={formData.email}
            onChange={handleChange}
            className="w-full px-3 py-2 border rounded-lg"
            required
            aria-required="true"
            aria-describedby={errors.email ? 'email-error' : undefined}
          />
          {errors.email && <p id="email-error" role="alert" className="text-red-500 text-sm mt-1">{errors.email}</p>}
        </div>
        <div>
          <label htmlFor="phone" className="block text-sm font-medium mb-1">
            {t('checkout.phone')}
          </label>
          <input
            type="tel"
            id="phone"
            name="phone"
            value={formData.phone}
            onChange={handleChange}
            className="w-full px-3 py-2 border rounded-lg"
            aria-describedby={errors.phone ? 'phone-error' : undefined}
          />
          {errors.phone && <p id="phone-error" role="alert" className="text-red-500 text-sm mt-1">{errors.phone}</p>}
        </div>
      </div>

      <div>
        <label htmlFor="streetLine1" className="block text-sm font-medium mb-1">
          {t('checkout.address')}
        </label>
        <input
          type="text"
          id="streetLine1"
          name="streetLine1"
          value={formData.streetLine1}
          onChange={handleChange}
          className="w-full px-3 py-2 border rounded-lg"
          required
          aria-required="true"
          aria-describedby={errors.streetLine1 ? 'streetLine1-error' : undefined}
        />
        {errors.streetLine1 && <p id="streetLine1-error" role="alert" className="text-red-500 text-sm mt-1">{errors.streetLine1}</p>}
      </div>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="col-span-2">
          <label htmlFor="city" className="block text-sm font-medium mb-1">
            {t('checkout.city')}
          </label>
          <input
            type="text"
            id="city"
            name="city"
            value={formData.city}
            onChange={handleChange}
            className="w-full px-3 py-2 border rounded-lg"
            required
            aria-required="true"
            aria-describedby={errors.city ? 'city-error' : undefined}
          />
          {errors.city && <p id="city-error" role="alert" className="text-red-500 text-sm mt-1">{errors.city}</p>}
        </div>
        <div>
          <label htmlFor="state" className="block text-sm font-medium mb-1">
            {t('checkout.state') || 'State'}
          </label>
          <input
            type="text"
            id="state"
            name="state"
            value={formData.state}
            onChange={handleChange}
            className="w-full px-3 py-2 border rounded-lg"
            required
            aria-required="true"
            aria-describedby={errors.state ? 'state-error' : undefined}
          />
          {errors.state && <p id="state-error" role="alert" className="text-red-500 text-sm mt-1">{errors.state}</p>}
        </div>
        <div>
          <label htmlFor="postalCode" className="block text-sm font-medium mb-1">
            {t('checkout.postalCode')}
          </label>
          <input
            type="text"
            id="postalCode"
            name="postalCode"
            value={formData.postalCode}
            onChange={handleChange}
            className="w-full px-3 py-2 border rounded-lg"
            required
            aria-required="true"
            aria-describedby={errors.postalCode ? 'postalCode-error' : undefined}
          />
          {errors.postalCode && <p id="postalCode-error" role="alert" className="text-red-500 text-sm mt-1">{errors.postalCode}</p>}
        </div>
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

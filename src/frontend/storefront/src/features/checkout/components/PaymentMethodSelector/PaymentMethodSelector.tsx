import { useTranslation } from 'react-i18next';
import {
  CreditCardIcon,
  DebitCardIcon,
  PayPalIcon,
  ApplePayIcon,
  GooglePayIcon,
  InfoCircleIcon,
} from '@/shared/components/icons';
import { useGetPaymentMethodsQuery } from '@/features/checkout/api/paymentsApi';
import styles from './PaymentMethodSelector.module.css';

interface PaymentMethodSelectorProps {
  selectedMethod: string;
  onMethodChange: (method: string) => void;
}

// Map of method key → icon component from the centralized icon library
const METHOD_ICONS: Record<string, React.ReactNode> = {
  stripe: <CreditCardIcon aria-hidden="true" className={styles.icon} fill="currentColor" />,
  credit_card: (
    <CreditCardIcon
      aria-hidden="true"
      className={styles.icon}
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
    />
  ),
  debit_card: (
    <DebitCardIcon
      aria-hidden="true"
      className={styles.icon}
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
    />
  ),
  paypal: <PayPalIcon aria-hidden="true" className={styles.icon} fill="currentColor" />,
  apple_pay: <ApplePayIcon aria-hidden="true" className={styles.icon} fill="currentColor" />,
  google_pay: <GooglePayIcon aria-hidden="true" className={styles.icon} fill="currentColor" />,
};

const FALLBACK_ICON = (
  <InfoCircleIcon
    aria-hidden="true"
    className={styles.icon}
    fill="none"
    stroke="currentColor"
    strokeWidth={2}
  />
);

export default function PaymentMethodSelector({
  selectedMethod,
  onMethodChange,
}: PaymentMethodSelectorProps) {
  const { t } = useTranslation();
  const { data, isLoading } = useGetPaymentMethodsQuery();

  if (isLoading) {
    return (
      <div
        className={styles.loading}
        aria-busy="true"
        aria-label={t('checkout.selectPaymentMethod')}
      >
        <div className={styles.skeleton} />
        <div className={styles.skeleton} />
        <div className={styles.skeleton} />
      </div>
    );
  }

  const methods = data?.methods ?? [];

  if (methods.length === 0) return null;

  return (
    <fieldset className={styles.fieldset}>
      <legend className={styles.legend}>{t('checkout.paymentMethodLabel')}</legend>
      <div role="radiogroup" aria-label={t('checkout.selectPaymentMethod')} className={styles.grid}>
        {methods.map((method) => {
          const labelKey = `checkout.paymentOptions.${method}` as const;
          const label = t(labelKey, { defaultValue: method.replace(/_/g, ' ') });
          const isSelected = selectedMethod === method;

          return (
            <label
              key={method}
              className={`${styles.option} ${isSelected ? styles.selected : ''}`}
              htmlFor={`payment-${method}`}
            >
              <input
                type="radio"
                id={`payment-${method}`}
                name="paymentMethod"
                value={method}
                checked={isSelected}
                onChange={() => onMethodChange(method)}
                className={styles.radio}
                aria-label={label}
              />
              <span className={styles.optionContent}>
                <span className={styles.iconWrapper}>{METHOD_ICONS[method] ?? FALLBACK_ICON}</span>
                <span className={styles.optionLabel}>{label}</span>
              </span>
            </label>
          );
        })}
      </div>
    </fieldset>
  );
}

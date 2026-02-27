import { useTranslation } from 'react-i18next';
import Input from '../../../components/ui/Input';
import Button from '../../../components/ui/Button';
import styles from './CheckoutForm.module.css';

interface FormData {
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

interface CheckoutFormProps {
  formData: FormData;
  errors?: Partial<Record<keyof FormData, string>>;
  onFormDataChange: (data: FormData) => void;
  onSubmit: (e: React.FormEvent) => void;
  isAuthenticated?: boolean;
}

export default function CheckoutForm({ 
  formData, 
  errors = {}, 
  onFormDataChange, 
  onSubmit,
  isAuthenticated = false 
}: CheckoutFormProps) {
  const { t } = useTranslation();
  
  const handleFieldChange = (field: keyof FormData, value: string) => {
    onFormDataChange({ ...formData, [field]: value });
  };

  return (
    <form onSubmit={onSubmit} className={styles.form}>
      {/* Guest Email Section - Prominent for guests */}
      {!isAuthenticated && (
        <div className={styles.guestEmailSection}>
          <h3 className={styles.sectionTitle}>{t('checkout.contactInfo')}</h3>
          <p className={styles.sectionDescription}>
            {t('checkout.emailConfirmation')}
          </p>
          <Input
            label={t('checkout.email')}
            type="email"
            value={formData.email}
            onChange={(e) => handleFieldChange('email', e.target.value)}
            error={errors.email}
            placeholder="your@email.com"
            required
          />
        </div>
      )}

      {/* Authenticated user email display */}
      {isAuthenticated && formData.email && (
        <div className={styles.authenticatedEmail}>
          <span className={styles.emailLabel}>{t('checkout.emailSentTo')}:</span>
          <span className={styles.emailValue}>{formData.email}</span>
        </div>
      )}

      <h3 className={styles.sectionTitle}>{t('checkout.shippingAddress')}</h3>
      
      <div className={styles.formGroup}>
        <Input
          label={t('checkout.firstName')}
          type="text"
          value={formData.firstName}
          onChange={(e) => handleFieldChange('firstName', e.target.value)}
          error={errors.firstName}
          placeholder="John"
          required
        />
        <Input
          label={t('checkout.lastName')}
          type="text"
          value={formData.lastName}
          onChange={(e) => handleFieldChange('lastName', e.target.value)}
          error={errors.lastName}
          placeholder="Doe"
          required
        />
      </div>

      {/* Email field for authenticated users (hidden but editable) */}
      {isAuthenticated && (
        <Input
          label={t('checkout.email')}
          type="email"
          value={formData.email}
          onChange={(e) => handleFieldChange('email', e.target.value)}
          error={errors.email}
          placeholder="your@email.com"
          required
        />
      )}

      <Input
        label={t('checkout.phone')}
        type="tel"
        value={formData.phone}
        onChange={(e) => handleFieldChange('phone', e.target.value)}
        error={errors.phone}
        placeholder="+1 (555) 123-4567"
        required
      />

      <Input
        label={t('checkout.address')}
        type="text"
        value={formData.streetLine1}
        onChange={(e) => handleFieldChange('streetLine1', e.target.value)}
        error={errors.streetLine1}
        placeholder="123 Main St"
        required
      />

      <div className={styles.formGroup}>
        <Input
          label={t('checkout.city')}
          type="text"
          value={formData.city}
          onChange={(e) => handleFieldChange('city', e.target.value)}
          error={errors.city}
          placeholder="New York"
          required
        />
        <Input
          label={t('checkout.state')}
          type="text"
          value={formData.state}
          onChange={(e) => handleFieldChange('state', e.target.value)}
          error={errors.state}
          placeholder="NY"
          required
        />
      </div>

      <div className={styles.formGroup}>
        <Input
          label={t('checkout.postalCode')}
          type="text"
          value={formData.postalCode}
          onChange={(e) => handleFieldChange('postalCode', e.target.value)}
          error={errors.postalCode}
          placeholder="10001"
          required
        />
        <Input
          label={t('checkout.country')}
          type="text"
          value={formData.country}
          onChange={(e) => handleFieldChange('country', e.target.value)}
          error={errors.country}
          placeholder="United States"
          required
        />
      </div>

      <Button type="submit" size="lg" className={styles.actionButton}>
        {t('checkout.placeOrder')}
      </Button>
    </form>
  );
}

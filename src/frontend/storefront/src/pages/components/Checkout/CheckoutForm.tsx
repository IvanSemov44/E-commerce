import Input from '../../../components/ui/Input';
import Button from '../../../components/ui/Button';
import styles from './CheckoutForm.module.css';

interface CheckoutFormProps {
  formData: {
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    streetLine1: string;
    city: string;
    state: string;
    postalCode: string;
    country: string;
  };
  onFormDataChange: (data: CheckoutFormProps['formData']) => void;
  onSubmit: (e: React.FormEvent) => void;
}

export default function CheckoutForm({ formData, onFormDataChange, onSubmit }: CheckoutFormProps) {
  const handleFieldChange = (field: keyof CheckoutFormProps['formData'], value: string) => {
    onFormDataChange({ ...formData, [field]: value });
  };

  return (
    <form onSubmit={onSubmit} className={styles.form}>
      <div className={styles.formGroup}>
        <Input
          label="First Name"
          type="text"
          value={formData.firstName}
          onChange={(e) => handleFieldChange('firstName', e.target.value)}
          placeholder="John"
          required
        />
        <Input
          label="Last Name"
          type="text"
          value={formData.lastName}
          onChange={(e) => handleFieldChange('lastName', e.target.value)}
          placeholder="Doe"
          required
        />
      </div>

      <Input
        label="Email Address"
        type="email"
        value={formData.email}
        onChange={(e) => handleFieldChange('email', e.target.value)}
        placeholder="your@email.com"
        required
      />

      <Input
        label="Phone"
        type="tel"
        value={formData.phone}
        onChange={(e) => handleFieldChange('phone', e.target.value)}
        placeholder="+1 (555) 123-4567"
        required
      />

      <Input
        label="Street Address"
        type="text"
        value={formData.streetLine1}
        onChange={(e) => handleFieldChange('streetLine1', e.target.value)}
        placeholder="123 Main St"
        required
      />

      <div className={styles.formGroup}>
        <Input
          label="City"
          type="text"
          value={formData.city}
          onChange={(e) => handleFieldChange('city', e.target.value)}
          placeholder="New York"
          required
        />
        <Input
          label="State"
          type="text"
          value={formData.state}
          onChange={(e) => handleFieldChange('state', e.target.value)}
          placeholder="NY"
          required
        />
      </div>

      <div className={styles.formGroup}>
        <Input
          label="Zip Code"
          type="text"
          value={formData.postalCode}
          onChange={(e) => handleFieldChange('postalCode', e.target.value)}
          placeholder="10001"
          required
        />
        <Input
          label="Country"
          type="text"
          value={formData.country}
          onChange={(e) => handleFieldChange('country', e.target.value)}
          placeholder="United States"
          required
        />
      </div>

      <Button type="submit" size="lg" className={styles.actionButton}>
        Place Order
      </Button>
    </form>
  );
}

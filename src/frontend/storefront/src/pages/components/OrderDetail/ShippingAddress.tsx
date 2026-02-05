import Card from '../../../components/ui/Card';
import styles from './ShippingAddress.module.css';

interface Address {
  firstName?: string;
  lastName?: string;
  streetLine1?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  country?: string;
  phone?: string;
}

interface ShippingAddressProps {
  address: Address | undefined;
}

export default function ShippingAddress({ address }: ShippingAddressProps) {
  if (!address) return null;

  return (
    <Card variant="elevated" padding="lg">
      <h2 className={styles.title}>Shipping Address</h2>

      <div className={styles.addressBlock}>
        <p className={styles.line}>
          {address.firstName} {address.lastName}
        </p>
        <p className={styles.line}>{address.streetLine1}</p>
        <p className={styles.line}>
          {address.city}, {address.state} {address.postalCode}
        </p>
        <p className={styles.line}>{address.country}</p>
        <p className={styles.phone}>Phone: {address.phone}</p>
      </div>
    </Card>
  );
}

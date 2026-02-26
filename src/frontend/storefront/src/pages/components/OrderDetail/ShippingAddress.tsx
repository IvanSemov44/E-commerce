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

// Icons
const MapPinIcon = () => (
  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" className={styles.titleIcon}>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
  </svg>
);

const PhoneIcon = () => (
  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" className={styles.phoneIcon}>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
  </svg>
);

export default function ShippingAddress({ address }: ShippingAddressProps) {
  if (!address) return null;

  return (
    <Card variant="elevated" padding="lg">
      <h2 className={styles.title}>
        <MapPinIcon />
        Shipping Address
      </h2>

      <div className={styles.addressBlock}>
        <p className={styles.name}>
          {address.firstName} {address.lastName}
        </p>
        <p className={styles.line}>{address.streetLine1}</p>
        <p className={styles.line}>
          {address.city}, {address.state} {address.postalCode}
        </p>
        <p className={styles.line}>{address.country}</p>
        {address.phone && (
          <p className={styles.phone}>
            <PhoneIcon />
            {address.phone}
          </p>
        )}
      </div>
    </Card>
  );
}

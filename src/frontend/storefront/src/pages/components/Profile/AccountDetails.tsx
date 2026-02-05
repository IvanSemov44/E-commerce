import Card from '../../../components/ui/Card';
import styles from './AccountDetails.module.css';

interface AccountDetailsProps {
  memberSince: string;
}

export default function AccountDetails({ memberSince }: AccountDetailsProps) {
  return (
    <Card variant="elevated" padding="lg">
      <h3 className={styles.title}>Account Details</h3>
      <div className={styles.detailsGrid}>
        <p className={styles.detailItem}>
          <strong>Member Since:</strong>{' '}
          {new Date(memberSince).toLocaleDateString()}
        </p>
        <p className={styles.detailItem}>
          <strong>Account Status:</strong> <span className={styles.activeStatus}>Active</span>
        </p>
      </div>
    </Card>
  );
}

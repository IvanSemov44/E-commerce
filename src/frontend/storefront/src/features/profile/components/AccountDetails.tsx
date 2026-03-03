import styles from './AccountDetails.module.css';

interface AccountDetailsProps {
  memberSince: string;
}

export default function AccountDetails({ memberSince }: AccountDetailsProps) {
  const parsed = new Date(memberSince);
  const dateText = Number.isNaN(parsed.getTime()) ? memberSince : parsed.toLocaleDateString();

  return (
    <section className={styles.card}>
      <h3 className={styles.title}>Account Details</h3>
      <p className={styles.row}><strong>Member Since:</strong> {dateText}</p>
      <p className={styles.row}><strong>Account Status:</strong> <span className={styles.active}>Active</span></p>
    </section>
  );
}

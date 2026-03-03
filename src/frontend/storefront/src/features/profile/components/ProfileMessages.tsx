import styles from './ProfileMessages.module.css';

interface ProfileMessagesProps {
  successMessage?: string;
  errorMessage?: string;
}

export default function ProfileMessages({ successMessage, errorMessage }: ProfileMessagesProps) {
  if (!successMessage && !errorMessage) {
    return null;
  }

  return (
    <div className={styles.container}>
      {successMessage ? <p className={styles.success}>{successMessage}</p> : null}
      {errorMessage ? <p className={styles.error}>{errorMessage}</p> : null}
    </div>
  );
}

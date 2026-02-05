import ErrorAlert from '../../../components/ErrorAlert';
import styles from './ProfileMessages.module.css';

interface ProfileMessagesProps {
  successMessage: string;
  errorMessage: string;
}

export default function ProfileMessages({ successMessage, errorMessage }: ProfileMessagesProps) {
  if (!successMessage && !errorMessage) return null;

  return (
    <>
      {successMessage && (
        <div className={styles.successMessage}>{successMessage}</div>
      )}
      {errorMessage && <ErrorAlert message={errorMessage} />}
    </>
  );
}

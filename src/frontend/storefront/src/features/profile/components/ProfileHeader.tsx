import Button from '@/shared/components/ui/Button';
import styles from './ProfileHeader.module.css';

interface ProfileHeaderProps {
  isEditMode: boolean;
  onEditClick: () => void;
}

export default function ProfileHeader({ isEditMode, onEditClick }: ProfileHeaderProps) {
  return (
    <header className={styles.header}>
      <div>
        <h2 className={styles.title}>Profile</h2>
        <p className={styles.subtitle}>Manage your account information</p>
      </div>
      {!isEditMode ? (
        <Button type="button" variant="outline" size="sm" onClick={onEditClick}>Edit</Button>
      ) : null}
    </header>
  );
}

import Button from '../../../components/ui/Button';
import styles from './ProfileHeader.module.css';

interface ProfileHeaderProps {
  isEditMode: boolean;
  onEditClick: () => void;
}

export default function ProfileHeader({ isEditMode, onEditClick }: ProfileHeaderProps) {
  return (
    <div className={styles.header}>
      <h2 className={styles.title}>Profile Information</h2>
      {!isEditMode && (
        <Button variant="secondary" onClick={onEditClick} size="sm">
          Edit Profile
        </Button>
      )}
    </div>
  );
}

import Button from '@/shared/components/ui/Button';
import type { ProfileFormData, ProfileFormProps } from './ProfileForm.types';
import styles from './ProfileForm.module.css';

export default function ProfileForm({
  formData,
  isEditMode,
  isUpdating,
  onFormDataChange,
  onSubmit,
  onCancel,
  onAvatarError,
}: ProfileFormProps) {
  const updateField = (field: keyof ProfileFormData, value: string) => {
    onFormDataChange({ ...formData, [field]: value });
  };

  const handleAvatarBlur = () => {
    if (!formData.avatarUrl) return;
    try {
      new URL(formData.avatarUrl);
    } catch {
      onAvatarError?.();
    }
  };

  return (
    <form className={styles.form} onSubmit={onSubmit}>
      <div className={styles.grid}>
        <label className={styles.field}>
          <span>First Name</span>
          <input
            value={formData.firstName}
            onChange={(e) => updateField('firstName', e.target.value)}
            disabled={!isEditMode || isUpdating}
          />
        </label>
        <label className={styles.field}>
          <span>Last Name</span>
          <input
            value={formData.lastName}
            onChange={(e) => updateField('lastName', e.target.value)}
            disabled={!isEditMode || isUpdating}
          />
        </label>
        <label className={styles.field}>
          <span>Email</span>
          <input value={formData.email} disabled />
        </label>
        <label className={styles.field}>
          <span>Phone</span>
          <input
            value={formData.phone}
            onChange={(e) => updateField('phone', e.target.value)}
            disabled={!isEditMode || isUpdating}
          />
        </label>
        <label className={styles.field}>
          <span>Avatar URL</span>
          <input
            value={formData.avatarUrl}
            onChange={(e) => updateField('avatarUrl', e.target.value)}
            onBlur={handleAvatarBlur}
            disabled={!isEditMode || isUpdating}
          />
        </label>
      </div>

      {isEditMode ? (
        <div className={styles.actions}>
          <Button type="button" variant="ghost" onClick={onCancel} disabled={isUpdating}>
            Cancel
          </Button>
          <Button type="submit" disabled={isUpdating}>
            {isUpdating ? 'Saving...' : 'Save Changes'}
          </Button>
        </div>
      ) : null}
    </form>
  );
}

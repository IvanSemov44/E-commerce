import { useProfileForm } from './useProfileForm';
import { Card } from '@/shared/components/ui/Card';
import { ProfileHeader } from '../ProfileHeader/ProfileHeader';
import { ProfileForm } from '../ProfileForm/ProfileForm';

export function ProfileFormCard() {
  const {
    values,
    fieldErrors,
    isEditMode,
    isPending,
    action,
    handleChange,
    handleBlur,
    setIsEditMode,
    handleCancel,
  } = useProfileForm();

  return (
    <Card variant="elevated" padding="lg">
      <ProfileHeader isEditMode={isEditMode} onEditClick={() => setIsEditMode(true)} />
      <ProfileForm
        values={values}
        fieldErrors={fieldErrors}
        isEditMode={isEditMode}
        isPending={isPending}
        action={action}
        onCancel={handleCancel}
        onChange={handleChange}
        onBlur={handleBlur}
      />
    </Card>
  );
}

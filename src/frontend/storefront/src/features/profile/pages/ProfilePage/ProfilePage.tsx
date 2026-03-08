import { useTranslation } from 'react-i18next';
import { useProfileForm } from '@/features/profile/hooks/useProfileForm';
import Card from '@/shared/components/ui/Card';
import PageHeader from '@/shared/components/PageHeader';
import ErrorAlert from '@/shared/components/ErrorAlert';
import { ProfileSkeleton } from '@/shared/components/Skeletons';
import ProfileHeader from '@/features/profile/components/ProfileHeader';
import ProfileForm from '@/features/profile/components/ProfileForm';
import ProfileMessages from '@/features/profile/components/ProfileMessages';
import AccountDetails from '@/features/profile/components/AccountDetails';
import styles from './ProfilePage.module.css';

export default function ProfilePage() {
  const { t } = useTranslation();
  const {
    profile,
    formData,
    isEditMode,
    successMessage,
    errorMessage,
    isLoading,
    isUpdating,
    error,
    setFormData,
    setIsEditMode,
    setErrorMessage,
    handleSubmit,
    handleCancel,
  } = useProfileForm();

  return (
    <div className={styles.container}>
      <PageHeader title={t('profile.title')} />

      {error ? (
        <ErrorAlert message={t('profile.failedToLoad')} />
      ) : isLoading ? (
        <Card variant="elevated" padding="lg">
          <ProfileSkeleton />
        </Card>
      ) : (
        <div className={styles.content}>
          <Card variant="elevated" padding="lg">
            <ProfileHeader isEditMode={isEditMode} onEditClick={() => setIsEditMode(true)} />

            <ProfileMessages successMessage={successMessage} errorMessage={errorMessage} />

            <ProfileForm
              formData={formData}
              isEditMode={isEditMode}
              isUpdating={isUpdating}
              onFormDataChange={setFormData}
              onSubmit={handleSubmit}
              onCancel={handleCancel}
              onAvatarError={() => setErrorMessage(t('profile.invalidImageUrl'))}
            />
          </Card>

          {profile && <AccountDetails memberSince={profile.email} />}
        </div>
      )}
    </div>
  );
}

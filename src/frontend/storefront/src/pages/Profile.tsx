import { useTranslation } from 'react-i18next';
import { useProfileForm } from '../hooks';
import Card from '../components/ui/Card';
import PageHeader from '../components/PageHeader';
import ErrorAlert from '../components/ErrorAlert';
import { ProfileSkeleton } from '../components/Skeletons';
import { ProfileHeader, ProfileForm, ProfileMessages, AccountDetails } from './components/Profile';
import styles from './Profile.module.css';

export default function Profile() {
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

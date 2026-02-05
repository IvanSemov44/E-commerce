import { useState, useEffect } from 'react';
import { useGetProfileQuery, useUpdateProfileMutation } from '../store/api/profileApi';
import { useAppDispatch } from '../store/hooks';
import { updateUser } from '../store/slices/authSlice';
import Card from '../components/ui/Card';
import PageHeader from '../components/PageHeader';
import ErrorAlert from '../components/ErrorAlert';
import LoadingSkeleton from '../components/LoadingSkeleton';
import { ProfileHeader, ProfileForm, ProfileMessages, AccountDetails } from './components/Profile';
import styles from './Profile.module.css';

export default function Profile() {
  const dispatch = useAppDispatch();
  const { data: profile, isLoading: isLoadingProfile, error: profileError } = useGetProfileQuery();
  const [updateProfile, { isLoading: isUpdating }] = useUpdateProfileMutation();

  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    avatarUrl: '',
  });

  const [isEditMode, setIsEditMode] = useState(false);
  const [successMessage, setSuccessMessage] = useState('');
  const [errorMessage, setErrorMessage] = useState('');

  useEffect(() => {
    if (profile) {
      setFormData({
        firstName: profile.firstName || '',
        lastName: profile.lastName || '',
        email: profile.email || '',
        phone: profile.phone || '',
        avatarUrl: profile.avatarUrl || '',
      });
    }
  }, [profile]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage('');
    setSuccessMessage('');

    if (!formData.firstName || !formData.lastName) {
      setErrorMessage('First name and last name are required');
      return;
    }

    try {
      const result = await updateProfile({
        firstName: formData.firstName,
        lastName: formData.lastName,
        phone: formData.phone || undefined,
        avatarUrl: formData.avatarUrl || undefined,
      }).unwrap();

      // Update auth state with new user data
      dispatch(
        updateUser({
          ...result,
          phone: result.phone,
          avatarUrl: result.avatarUrl,
        })
      );

      setSuccessMessage('Profile updated successfully');
      setIsEditMode(false);

      setTimeout(() => {
        setSuccessMessage('');
      }, 3000);
    } catch (err: any) {
      setErrorMessage(err.data?.message || 'Failed to update profile');
    }
  };

  const handleCancel = () => {
    setIsEditMode(false);
    setErrorMessage('');
    if (profile) {
      setFormData({
        firstName: profile.firstName || '',
        lastName: profile.lastName || '',
        email: profile.email || '',
        phone: profile.phone || '',
        avatarUrl: profile.avatarUrl || '',
      });
    }
  };

  return (
    <div className={styles.container}>
      <PageHeader title="My Profile" />

      {profileError ? (
        <ErrorAlert message="Failed to load profile. Please try again later." />
      ) : isLoadingProfile ? (
        <LoadingSkeleton count={1} type="card" />
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
              onAvatarError={() => setErrorMessage('Invalid image URL')}
            />
          </Card>

          {profile && <AccountDetails memberSince={profile.email} />}
        </div>
      )}
    </div>
  );
}

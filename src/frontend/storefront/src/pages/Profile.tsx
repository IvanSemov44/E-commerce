import { useState, useEffect } from 'react';
import { useGetProfileQuery, useUpdateProfileMutation } from '../store/api/profileApi';
import { useAppDispatch } from '../store/hooks';
import { updateUser } from '../store/slices/authSlice';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Card from '../components/ui/Card';
import PageHeader from '../components/PageHeader';
import ErrorAlert from '../components/ErrorAlert';
import LoadingSkeleton from '../components/LoadingSkeleton';

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

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

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

  return (
    <div style={{ maxWidth: '800px', margin: '0 auto', padding: '0 1rem' }}>
      <PageHeader title="My Profile" />

      {profileError ? (
        <ErrorAlert message="Failed to load profile. Please try again later." />
      ) : isLoadingProfile ? (
        <LoadingSkeleton count={1} type="card" />
      ) : (
        <div style={{ display: 'grid', gap: '2rem' }}>
          {/* Profile Card */}
          <Card variant="elevated" padding="lg">
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                marginBottom: '2rem',
              }}
            >
              <h2 style={{ margin: 0 }}>Profile Information</h2>
              {!isEditMode && (
                <Button
                  variant="secondary"
                  onClick={() => setIsEditMode(true)}
                  size="sm"
                >
                  Edit Profile
                </Button>
              )}
            </div>

            {/* Messages */}
            {successMessage && (
              <div
                style={{
                  padding: '1rem',
                  marginBottom: '1rem',
                  backgroundColor: '#e8f5e9',
                  border: '1px solid #4caf50',
                  borderRadius: '0.5rem',
                  color: '#2e7d32',
                }}
              >
                {successMessage}
              </div>
            )}

            {errorMessage && <ErrorAlert message={errorMessage} />}

            <form onSubmit={handleSubmit}>
              <div style={{ display: 'grid', gap: '1.5rem' }}>
                {/* Name Fields */}
                <div
                  style={{
                    display: 'grid',
                    gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
                    gap: '1rem',
                  }}
                >
                  <Input
                    label="First Name"
                    type="text"
                    name="firstName"
                    value={formData.firstName}
                    onChange={handleChange}
                    disabled={!isEditMode}
                    required
                  />
                  <Input
                    label="Last Name"
                    type="text"
                    name="lastName"
                    value={formData.lastName}
                    onChange={handleChange}
                    disabled={!isEditMode}
                    required
                  />
                </div>

                {/* Email (Read-only) */}
                <Input
                  label="Email Address"
                  type="email"
                  name="email"
                  value={formData.email}
                  disabled={true}
                  title="Email cannot be changed"
                />

                {/* Phone */}
                <Input
                  label="Phone Number"
                  type="tel"
                  name="phone"
                  value={formData.phone}
                  onChange={handleChange}
                  disabled={!isEditMode}
                  placeholder="+1 (555) 123-4567"
                />

                {/* Avatar URL */}
                <Input
                  label="Avatar URL"
                  type="url"
                  name="avatarUrl"
                  value={formData.avatarUrl}
                  onChange={handleChange}
                  disabled={!isEditMode}
                  placeholder="https://example.com/avatar.jpg"
                />

                {/* Preview Avatar */}
                {formData.avatarUrl && (
                  <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#666' }}>
                      Avatar Preview:
                    </p>
                    <img
                      src={formData.avatarUrl}
                      alt="Avatar preview"
                      style={{
                        width: '60px',
                        height: '60px',
                        borderRadius: '50%',
                        objectFit: 'cover',
                        border: '2px solid #1976d2',
                      }}
                      onError={() => {
                        setErrorMessage('Invalid image URL');
                      }}
                    />
                  </div>
                )}

                {/* Action Buttons */}
                {isEditMode && (
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                    <Button
                      type="submit"
                      disabled={isUpdating}
                      isLoading={isUpdating}
                    >
                      {isUpdating ? 'Saving...' : 'Save Changes'}
                    </Button>
                    <Button
                      variant="secondary"
                      onClick={() => {
                        setIsEditMode(false);
                        setErrorMessage('');
                        // Reset form to original values
                        if (profile) {
                          setFormData({
                            firstName: profile.firstName || '',
                            lastName: profile.lastName || '',
                            email: profile.email || '',
                            phone: profile.phone || '',
                            avatarUrl: profile.avatarUrl || '',
                          });
                        }
                      }}
                    >
                      Cancel
                    </Button>
                  </div>
                )}
              </div>
            </form>
          </Card>

          {/* Additional Info */}
          <Card variant="elevated" padding="lg">
            <h3 style={{ marginTop: 0 }}>Account Details</h3>
            <div style={{ display: 'grid', gap: '1rem', color: '#666', fontSize: '0.95rem' }}>
              <p style={{ margin: 0 }}>
                <strong>Member Since:</strong>{' '}
                {profile ? new Date(profile.email).toLocaleDateString() : 'N/A'}
              </p>
              <p style={{ margin: 0 }}>
                <strong>Account Status:</strong> <span style={{ color: '#4caf50' }}>Active</span>
              </p>
            </div>
          </Card>
        </div>
      )}
    </div>
  );
}

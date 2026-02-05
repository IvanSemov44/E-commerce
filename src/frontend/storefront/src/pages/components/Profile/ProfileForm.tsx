import Input from '../../../components/ui/Input';
import Button from '../../../components/ui/Button';
import styles from './ProfileForm.module.css';

interface ProfileFormData {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  avatarUrl: string;
}

interface ProfileFormProps {
  formData: ProfileFormData;
  isEditMode: boolean;
  isUpdating: boolean;
  onFormDataChange: (data: ProfileFormData) => void;
  onSubmit: (e: React.FormEvent) => void;
  onCancel: () => void;
  onAvatarError: () => void;
}

export default function ProfileForm({
  formData,
  isEditMode,
  isUpdating,
  onFormDataChange,
  onSubmit,
  onCancel,
  onAvatarError,
}: ProfileFormProps) {
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    onFormDataChange({ ...formData, [name]: value });
  };

  return (
    <form onSubmit={onSubmit}>
      <div className={styles.formGrid}>
        {/* Name Fields */}
        <div className={styles.nameGrid}>
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
          <div className={styles.avatarPreview}>
            <p className={styles.avatarLabel}>Avatar Preview:</p>
            <img
              src={formData.avatarUrl}
              alt="Avatar preview"
              className={styles.avatarImage}
              onError={onAvatarError}
            />
          </div>
        )}

        {/* Action Buttons */}
        {isEditMode && (
          <div className={styles.buttonGrid}>
            <Button type="submit" disabled={isUpdating} isLoading={isUpdating}>
              {isUpdating ? 'Saving...' : 'Save Changes'}
            </Button>
            <Button variant="secondary" onClick={onCancel}>
              Cancel
            </Button>
          </div>
        )}
      </div>
    </form>
  );
}

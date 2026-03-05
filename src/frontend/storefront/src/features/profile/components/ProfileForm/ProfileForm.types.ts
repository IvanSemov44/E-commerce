export interface ProfileFormData {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  avatarUrl?: string;
}

export interface ProfileFormProps {
  formData: ProfileFormData;
  isEditMode: boolean;
  isUpdating: boolean;
  onFormDataChange: (data: ProfileFormData) => void;
  onSubmit: (e: React.FormEvent) => Promise<void> | void;
  onCancel: () => void;
  onAvatarError?: () => void;
}

import type { ProfileFormData } from './ProfileForm.types';

/**
 * Hook for handling profile form field updates
 * @param formData - Current form data
 * @param onFormDataChange - Callback when form data changes
 * @returns Object with updateField function
 */
export function useProfileFormHandling(
  formData: ProfileFormData,
  onFormDataChange: (data: ProfileFormData) => void
) {
  const updateField = (field: keyof ProfileFormData, value: string) => {
    onFormDataChange({ ...formData, [field]: value });
  };

  return { updateField };
}

/**
 * Hook for validating avatar URL
 * @param avatarUrl - Avatar URL to validate
 * @param onAvatarError - Callback when avatar URL is invalid
 * @returns Object with handleAvatarBlur function
 */
export function useAvatarValidation(
  avatarUrl: string,
  onAvatarError?: () => void
) {
  const handleAvatarBlur = () => {
    if (!avatarUrl) return;
    try {
      new URL(avatarUrl);
    } catch {
      onAvatarError?.();
    }
  };

  return { handleAvatarBlur };
}

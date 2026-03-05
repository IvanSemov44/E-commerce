/**
 * useProfileForm Hook
 * Manages profile form state and submission logic
 */

import { useState, useEffect } from 'react';
import { useGetProfileQuery, useUpdateProfileMutation } from '../api/profileApi';
import { useAppDispatch } from '@/shared/lib/store';
import { updateUser } from '@/features/auth/slices/authSlice';
import useForm from '@/shared/hooks/useForm';
import { validators } from '@/shared/lib/utils/validation';
import type { UserProfile } from '@/shared/types';

export interface ProfileFormData {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  avatarUrl?: string;
}

export interface UseProfileFormReturn {
  profile: UserProfile | undefined;
  formData: ProfileFormData;
  isEditMode: boolean;
  successMessage: string;
  errorMessage: string;
  isLoading: boolean;
  isUpdating: boolean;
  error: unknown;
  errors: Partial<Record<keyof ProfileFormData, string>>;
  handleChange: (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => void;
  setFormData: (data: ProfileFormData) => void;
  setIsEditMode: (mode: boolean) => void;
  setErrorMessage: (msg: string) => void;
  handleSubmit: (e: React.FormEvent) => Promise<void>;
  handleCancel: () => void;
}

// Validation function for profile form
const validateProfileForm = (values: ProfileFormData): Partial<Record<keyof ProfileFormData, string>> => {
  const errors: Partial<Record<keyof ProfileFormData, string>> = {};

  const firstNameError = validators.required('First name')(values.firstName);
  if (firstNameError) errors.firstName = firstNameError;

  const lastNameError = validators.required('Last name')(values.lastName);
  if (lastNameError) errors.lastName = lastNameError;

  if (values.phone && values.phone.trim()) {
    const phoneError = validators.phone(values.phone);
    if (phoneError) errors.phone = phoneError;
  }

  return errors;
};

/**
 * Custom hook for managing profile form state and submission
 * Handles:
 * - Profile data fetching
 * - Form data sync with profile
 * - Edit mode toggle
 * - Form validation
 * - Profile submission
 * - Error/success messaging
 * - Auth state update
 */
export const useProfileForm = (): UseProfileFormReturn => {
  const dispatch = useAppDispatch();
  const { data: profile, isLoading, error } = useGetProfileQuery();
  const [updateProfile, { isLoading: isUpdating }] = useUpdateProfileMutation();

  const [isEditMode, setIsEditMode] = useState(false);
  const [successMessage, setSuccessMessage] = useState('');
  const [errorMessage, setErrorMessage] = useState('');

  // Handle form submission
  const handleFormSubmit = async (values: ProfileFormData) => {
    setErrorMessage('');
    setSuccessMessage('');

    try {
      const result = await updateProfile({
        firstName: values.firstName,
        lastName: values.lastName,
        phone: values.phone || undefined,
        avatarUrl: values.avatarUrl || undefined,
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

      // Auto-clear success message after 3 seconds
      setTimeout(() => {
        setSuccessMessage('');
      }, 3000);
    } catch (err: unknown) {
      const error = err as { data?: { message?: string } };
      setErrorMessage(error.data?.message || 'Failed to update profile');
    }
  };

  // Initialize useForm hook
  const form = useForm<ProfileFormData>({
    initialValues: {
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      avatarUrl: '',
    },
    validate: validateProfileForm,
    onSubmit: handleFormSubmit,
  });

  // Sync profile data to form when profile loads
  useEffect(() => {
    if (profile) {
      form.setValues({
        firstName: profile.firstName || '',
        lastName: profile.lastName || '',
        email: profile.email || '',
        phone: profile.phone || '',
        avatarUrl: profile.avatarUrl || '',
      });
    }
  }, [profile, form]);

  // Handle form cancel
  const handleCancel = () => {
    setIsEditMode(false);
    setErrorMessage('');

    // Reset form to original profile data
    if (profile) {
      form.setValues({
        firstName: profile.firstName || '',
        lastName: profile.lastName || '',
        email: profile.email || '',
        phone: profile.phone || '',
        avatarUrl: profile.avatarUrl || '',
      });
    }
    form.reset();
  };

  // Adapter for backward compatibility with ProfileForm
  const setFormData = (data: ProfileFormData) => {
    form.setValues(data);
  };

  return {
    profile,
    formData: form.values,
    isEditMode,
    successMessage,
    errorMessage,
    isLoading,
    isUpdating,
    error,
    errors: form.errors,
    handleChange: form.handleChange,
    setFormData,
    setIsEditMode,
    setErrorMessage,
    handleSubmit: form.handleSubmit,
    handleCancel,
  };
};

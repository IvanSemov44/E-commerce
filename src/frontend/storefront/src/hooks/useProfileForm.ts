/**
 * useProfileForm Hook
 * Manages profile form state and submission logic
 */

import { useState, useEffect } from 'react';
import { useGetProfileQuery, useUpdateProfileMutation } from '../store/api/profileApi';
import { useAppDispatch } from '../store/hooks';
import { updateUser } from '../store/slices/authSlice';

export interface ProfileFormData {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  avatarUrl: string;
}

export interface UseProfileFormReturn {
  profile: any; // typing from API
  formData: ProfileFormData;
  isEditMode: boolean;
  successMessage: string;
  errorMessage: string;
  isLoading: boolean;
  isUpdating: boolean;
  error: any;
  setFormData: (data: ProfileFormData) => void;
  setIsEditMode: (mode: boolean) => void;
  setErrorMessage: (msg: string) => void;
  handleSubmit: (e: React.FormEvent) => Promise<void>;
  handleCancel: () => void;
}

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

  const [formData, setFormData] = useState<ProfileFormData>({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    avatarUrl: '',
  });

  const [isEditMode, setIsEditMode] = useState(false);
  const [successMessage, setSuccessMessage] = useState('');
  const [errorMessage, setErrorMessage] = useState('');

  // Sync profile data to form when profile loads
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

  // Handle form submission
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage('');
    setSuccessMessage('');

    // Validation
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

      // Auto-clear success message after 3 seconds
      setTimeout(() => {
        setSuccessMessage('');
      }, 3000);
    } catch (err: any) {
      setErrorMessage(err.data?.message || 'Failed to update profile');
    }
  };

  // Handle form cancel
  const handleCancel = () => {
    setIsEditMode(false);
    setErrorMessage('');

    // Reset form to original profile data
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

  return {
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
  };
};

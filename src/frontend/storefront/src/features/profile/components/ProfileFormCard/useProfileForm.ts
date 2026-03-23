import { useState, useMemo, useActionState, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useGetProfileQuery, useUpdateProfileMutation } from '@/features/profile/api/profileApi';
import { useAppDispatch } from '@/shared/lib/store';
import { updateUser } from '@/features/auth/slices/authSlice';
import { useToast, useApiErrorHandler } from '@/shared/hooks';
import { parseBackendFieldErrors } from '@/shared/lib/utils/parseBackendFieldErrors';
import { createProfileSchema } from './profileSchemas';
import type { ProfileFormValues } from './profileSchemas';

type FieldErrors = Partial<Record<keyof ProfileFormValues, string>>;

const CODE_TO_FIELD: Partial<Record<string, keyof ProfileFormValues>> = {
  DUPLICATE_EMAIL: 'email',
  DUPLICATE_PHONE: 'phone',
};

const INITIAL_VALUES: ProfileFormValues = {
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  avatarUrl: '',
};

export function useProfileForm() {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const { data: profile } = useGetProfileQuery();
  const [updateProfile] = useUpdateProfileMutation();
  const { toast } = useToast();
  const { handleError } = useApiErrorHandler();

  const schema = useMemo(() => createProfileSchema(t), [t]);
  const [values, setValues] = useState<ProfileFormValues>(INITIAL_VALUES);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [isEditMode, setIsEditMode] = useState(false);
  const syncedRef = useRef<string | undefined>(undefined);

  useEffect(() => {
    if (profile && profile.id !== syncedRef.current) {
      syncedRef.current = profile.id;
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setValues({
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
    setValues((prev) => ({ ...prev, [name]: value }));
    setFieldErrors((prev) => ({ ...prev, [name as keyof ProfileFormValues]: undefined }));
  };

  const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
    const name = e.target.name as keyof ProfileFormValues;
    const result = schema.safeParse(values);
    const fieldIssue = result.success
      ? undefined
      : result.error.issues.find((issue) => issue.path[0] === name);
    setFieldErrors((prev) => ({ ...prev, [name]: fieldIssue?.message }));
  };

  const [, action, isPending] = useActionState(async () => {
    const result = schema.safeParse(values);

    if (!result.success) {
      const errors: FieldErrors = {};
      for (const issue of result.error.issues) {
        const field = issue.path[0] as keyof ProfileFormValues;
        if (field !== undefined && !errors[field]) errors[field] = issue.message;
      }
      setFieldErrors(errors);
      return null;
    }

    try {
      const updated = await updateProfile({
        firstName: result.data.firstName,
        lastName: result.data.lastName,
        phone: result.data.phone || undefined,
        avatarUrl: result.data.avatarUrl || undefined,
      }).unwrap();

      dispatch(updateUser({ ...updated, phone: updated.phone, avatarUrl: updated.avatarUrl }));
      toast.success(t('profile.savedSuccess'));
      setIsEditMode(false);
    } catch (err) {
      const parsedErrors = parseBackendFieldErrors(err, CODE_TO_FIELD);
      if (parsedErrors) {
        setFieldErrors(parsedErrors);
      } else {
        handleError(err, t('profile.savedFailed'));
      }
    }

    return null;
  }, null);

  const handleCancel = () => {
    setIsEditMode(false);
    setFieldErrors({});
    if (profile) {
      setValues({
        firstName: profile.firstName || '',
        lastName: profile.lastName || '',
        email: profile.email || '',
        phone: profile.phone || '',
        avatarUrl: profile.avatarUrl || '',
      });
    }
  };

  return {
    values,
    fieldErrors,
    isEditMode,
    isPending,
    handleChange,
    handleBlur,
    action,
    setIsEditMode,
    handleCancel,
  };
}

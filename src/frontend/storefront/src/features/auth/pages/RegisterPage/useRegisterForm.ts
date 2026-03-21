import { useState, useMemo, useActionState } from 'react';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import type { ChangeEvent, FocusEvent } from 'react';
import { useRegisterMutation } from '@/features/auth/api/authApi';
import { useAppDispatch } from '@/shared/lib/store';
import { loginSuccess } from '@/features/auth/slices/authSlice';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { useToast, useApiErrorHandler } from '@/shared/hooks';
import { parseBackendFieldErrors } from '@/shared/lib/utils';
import { createRegisterSchema } from './registerSchema';
import type { RegisterFormValues } from './registerSchema';
import { usePasswordVisibility } from '@/features/auth/hooks/usePasswordVisibility';

type FieldErrors = Partial<Record<keyof RegisterFormValues, string>>;

const INITIAL_VALUES: RegisterFormValues = {
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  confirmPassword: '',
  termsAccepted: false,
};

const CODE_TO_FIELD: Partial<Record<string, keyof RegisterFormValues>> = {
  DUPLICATE_EMAIL: 'email',
};

export function useRegisterForm() {
  const { t } = useTranslation();
  const [register] = useRegisterMutation();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { toast } = useToast();
  const { handleError } = useApiErrorHandler();

  const schema = useMemo(() => createRegisterSchema(t), [t]);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [values, setValues] = useState<RegisterFormValues>(INITIAL_VALUES);
  const password = usePasswordVisibility();
  const confirmPassword = usePasswordVisibility();

  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    const { name, type, value, checked } = e.target;
    const fieldValue = type === 'checkbox' ? checked : value;
    const newValues = { ...values, [name]: fieldValue } as RegisterFormValues;
    setValues(newValues);

    const updates: FieldErrors = { [name as keyof RegisterFormValues]: undefined };

    if (name === 'password' && (newValues.confirmPassword || fieldErrors.confirmPassword)) {
      updates.confirmPassword =
        newValues.confirmPassword !== value ? t('auth.passwordsDoNotMatch') : undefined;
    }

    setFieldErrors((prev) => ({ ...prev, ...updates }));
  };

  const handleBlur = (e: FocusEvent<HTMLInputElement>) => {
    const name = e.target.name as keyof RegisterFormValues;
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
        const field = issue.path[0] as keyof RegisterFormValues;
        if (field !== undefined && !errors[field]) errors[field] = issue.message;
      }
      setFieldErrors(errors);
      return null;
    }

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const { confirmPassword, termsAccepted, ...registerData } = result.data;
    try {
      const response = await register(registerData).unwrap();
      if (response.success && response.user) {
        dispatch(loginSuccess(response.user));
        toast.success(t('auth.registrationSuccess'));
        navigate(ROUTE_PATHS.home);
      } else {
        toast.error(response.message || t('auth.registrationFailed'));
      }
    } catch (err) {
      const backendErrors = parseBackendFieldErrors(err, CODE_TO_FIELD);
      if (backendErrors) {
        setFieldErrors(backendErrors);
      } else {
        handleError(err, t('auth.registrationError'));
      }
    }

    return null;
  }, null);

  return {
    values,
    fieldErrors,
    password,
    confirmPassword,
    handleChange,
    handleBlur,
    action,
    isPending,
  };
}

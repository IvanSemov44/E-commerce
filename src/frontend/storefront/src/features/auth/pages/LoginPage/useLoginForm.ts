import { useState, useMemo, useActionState } from 'react';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useLoginMutation } from '@/features/auth/api/authApi';
import { useAppDispatch } from '@/shared/lib/store';
import { loginSuccess } from '@/features/auth/slices/authSlice';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { useToast, useApiErrorHandler } from '@/shared/hooks';
import { parseBackendFieldErrors, isApiError } from '@/shared/lib/utils';
import { usePasswordVisibility } from '@/features/auth/hooks/usePasswordVisibility';
import { createLoginSchema } from './loginSchema';
import type { LoginFormValues } from './loginSchema';

type FieldErrors = Partial<Record<keyof LoginFormValues, string>>;

const INITIAL_VALUES: LoginFormValues = { email: '', password: '' };

const CODE_TO_FIELD: Partial<Record<string, keyof LoginFormValues>> = {
  INVALID_CREDENTIALS: 'email',
};

export function useLoginForm() {
  const { t } = useTranslation();
  const [login] = useLoginMutation();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { toast } = useToast();
  const { handleError } = useApiErrorHandler();

  const schema = useMemo(() => createLoginSchema(t), [t]);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [values, setValues] = useState<LoginFormValues>(INITIAL_VALUES);
  const password = usePasswordVisibility();

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setValues((prev) => ({ ...prev, [name]: value }));
    setFieldErrors((prev) => ({ ...prev, [name as keyof LoginFormValues]: undefined }));
  };

  const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
    const name = e.target.name as keyof LoginFormValues;
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
        const field = issue.path[0] as keyof LoginFormValues;
        if (field !== undefined && !errors[field]) errors[field] = issue.message;
      }
      setFieldErrors(errors);
      return null;
    }

    try {
      const response = await login(result.data).unwrap();
      if (response.success && response.user) {
        dispatch(loginSuccess(response.user));
        toast.success(t('auth.loginSuccess'));
        navigate(ROUTE_PATHS.home);
      } else {
        const errorMessage = isApiError(response)
          ? response.errorDetails.message
          : response.message || t('auth.loginError');
        toast.error(errorMessage);
      }
    } catch (err) {
      const backendErrors = parseBackendFieldErrors(err, CODE_TO_FIELD);
      if (backendErrors) {
        setFieldErrors(backendErrors);
      } else {
        handleError(err, t('auth.loginError'));
      }
    }

    return null;
  }, null);

  return { values, fieldErrors, password, handleChange, handleBlur, action, isPending };
}

import { useState, useMemo, useActionState } from 'react';
import { useNavigate, useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import type { ChangeEvent } from 'react';
import { useResetPasswordMutation } from '@/features/auth/api/authApi';
import { useToast, useApiErrorHandler } from '@/shared/hooks';
import { parseBackendFieldErrors } from '@/shared/lib/utils';
import { usePasswordVisibility } from '@/features/auth/hooks/usePasswordVisibility';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { createResetPasswordSchema } from './resetPasswordSchema';
import type { ResetPasswordFormValues } from './resetPasswordSchema';

type FieldErrors = Partial<Record<keyof ResetPasswordFormValues, string>>;

const INITIAL_VALUES: ResetPasswordFormValues = { password: '', confirmPassword: '' };

// Map backend token error codes to fields if the API returns them
const CODE_TO_FIELD: Partial<Record<string, keyof ResetPasswordFormValues>> = {};

export function useResetPasswordForm() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [resetPassword] = useResetPasswordMutation();
  const { toast } = useToast();
  const { handleError } = useApiErrorHandler();

  const email = searchParams.get('email') ?? '';
  const token = searchParams.get('token') ?? '';

  const schema = useMemo(() => createResetPasswordSchema(t), [t]);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [values, setValues] = useState<ResetPasswordFormValues>(INITIAL_VALUES);
  const [submitted, setSubmitted] = useState(false);
  const password = usePasswordVisibility();
  const confirmPassword = usePasswordVisibility();

  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    const newValues = { ...values, [name]: value } as ResetPasswordFormValues;
    setValues(newValues);

    const updates: FieldErrors = { [name as keyof ResetPasswordFormValues]: undefined };
    if (name === 'password' && (newValues.confirmPassword || fieldErrors.confirmPassword)) {
      updates.confirmPassword =
        newValues.confirmPassword !== value ? t('auth.passwordsDoNotMatch') : undefined;
    }
    setFieldErrors((prev) => ({ ...prev, ...updates }));
  };

  const handleBlur = (e: ChangeEvent<HTMLInputElement>) => {
    const name = e.target.name as keyof ResetPasswordFormValues;
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
        const field = issue.path[0] as keyof ResetPasswordFormValues;
        if (field !== undefined && !errors[field]) errors[field] = issue.message;
      }
      setFieldErrors(errors);
      return null;
    }

    try {
      await resetPassword({ email, token, newPassword: result.data.password }).unwrap();
      setSubmitted(true);
      toast.success(t('resetPassword.passwordResetSuccess'));
      navigate(ROUTE_PATHS.login);
    } catch (err) {
      const backendErrors = parseBackendFieldErrors(err, CODE_TO_FIELD);
      if (backendErrors) {
        setFieldErrors(backendErrors);
      } else {
        handleError(err, t('resetPassword.failed'));
      }
    }

    return null;
  }, null);

  return {
    values,
    fieldErrors,
    password,
    confirmPassword,
    submitted,
    hasValidParams: Boolean(email && token),
    handleChange,
    handleBlur,
    action,
    isPending,
  };
}

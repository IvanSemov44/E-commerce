import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import type { ChangeEvent, FormEvent } from 'react';
import { useForgotPasswordMutation } from '@/features/auth/api/authApi';
import { useApiErrorHandler } from '@/shared/hooks';
import { createForgotPasswordSchema } from './forgotPasswordSchema';
import type { ForgotPasswordFormValues } from './forgotPasswordSchema';

type FieldErrors = Partial<Record<keyof ForgotPasswordFormValues, string>>;

const INITIAL_VALUES: ForgotPasswordFormValues = { email: '' };

export function useForgotPasswordForm() {
  const { t } = useTranslation();
  const [forgotPassword] = useForgotPasswordMutation();
  const { handleError } = useApiErrorHandler();

  const schema = useMemo(() => createForgotPasswordSchema(t), [t]);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [values, setValues] = useState<ForgotPasswordFormValues>(INITIAL_VALUES);
  const [submitted, setSubmitted] = useState(false);
  const [isPending, setIsPending] = useState(false);

  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    setValues((prev) => ({ ...prev, email: e.target.value }));
    setFieldErrors({});
  };

  const handleBlur = () => {
    const result = schema.safeParse(values);
    const fieldIssue = result.success
      ? undefined
      : result.error.issues.find((issue) => issue.path[0] === 'email');
    setFieldErrors({ email: fieldIssue?.message });
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setIsPending(true);

    const result = schema.safeParse(values);

    if (!result.success) {
      const message = result.error.issues[0]?.message;
      setFieldErrors({ email: message });
      setIsPending(false);
      return;
    }

    try {
      await forgotPassword({ email: result.data.email }).unwrap();
      setSubmitted(true);
    } catch (err) {
      handleError(err, t('common.error'));
    } finally {
      setIsPending(false);
    }
  };

  return { values, fieldErrors, submitted, handleChange, handleBlur, handleSubmit, isPending };
}

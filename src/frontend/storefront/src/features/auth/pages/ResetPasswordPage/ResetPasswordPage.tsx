import { useState } from 'react';
import { Link, useSearchParams, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useResetPasswordMutation } from '@/features/auth/api/authApi';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { useToast, useApiErrorHandler } from '@/shared/hooks';
import { useForm } from '@/shared/hooks/useForm';
import { zodValidate } from '@/shared/lib/utils/zodValidate';
import { createResetPasswordSchema } from '@/features/auth/schemas/authSchemas';
import { Button, Input, Card } from '@/shared/components/ui';
import styles from './ResetPasswordPage.module.css';

export function ResetPasswordPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [success, setSuccess] = useState(false);
  const [resetPassword, { isLoading }] = useResetPasswordMutation();
  const { toast } = useToast();
  const { handleError } = useApiErrorHandler();

  const email = searchParams.get('email') ?? '';
  const token = searchParams.get('token') ?? '';

  const form = useForm({
    initialValues: { password: '', confirmPassword: '' },
    validate: zodValidate(createResetPasswordSchema(t)),
    onSubmit: async (values) => {
      try {
        await resetPassword({ email, token, newPassword: values.password }).unwrap();
        setSuccess(true);
        toast.success(t('resetPassword.passwordResetSuccess'));
        navigate(ROUTE_PATHS.login);
      } catch (err) {
        handleError(err, t('resetPassword.failed'));
      }
    },
  });

  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>{t('resetPassword.title')}</h1>

        {success ? (
          <div className={styles.centered}>
            <div className={styles.successBox}>
              <p className={styles.successTitle}>{t('resetPassword.successTitle')}</p>
              <p>{t('resetPassword.successMessage')}</p>
            </div>
            <Link to={ROUTE_PATHS.login} className={styles.footerLink}>
              {t('resetPassword.goToLogin')}
            </Link>
          </div>
        ) : (
          <>
            {email && token ? (
              <>
                <p className={styles.description}>{t('resetPassword.subtitle')}</p>

                <form onSubmit={form.handleSubmit} className={styles.form}>
                  <Input
                    label={t('resetPassword.newPassword')}
                    type="password"
                    name="password"
                    value={form.values.password}
                    onChange={form.handleChange}
                    error={form.errors.password}
                    disabled={isLoading}
                    required
                    placeholder={t('resetPassword.newPasswordPlaceholder')}
                  />

                  <Input
                    label={t('resetPassword.confirmNewPassword')}
                    type="password"
                    name="confirmPassword"
                    value={form.values.confirmPassword}
                    onChange={form.handleChange}
                    error={form.errors.confirmPassword}
                    disabled={isLoading}
                    required
                    placeholder={t('resetPassword.confirmPasswordPlaceholder')}
                  />

                  <Button type="submit" disabled={isLoading || form.isSubmitting} size="lg">
                    {isLoading || form.isSubmitting
                      ? t('resetPassword.resetting')
                      : t('resetPassword.resetPasswordBtn')}
                  </Button>
                </form>
              </>
            ) : (
              <div className={styles.centered}>
                <p className={styles.description}>{t('resetPassword.invalidLink')}</p>
                <Link to={ROUTE_PATHS.forgotPassword} className={styles.footerLink}>
                  {t('resetPassword.requestNewLink')}
                </Link>
              </div>
            )}

            <div className={styles.footer}>
              <p className={styles.footerText}>
                {t('auth.rememberMe')}{' '}
                <Link to={ROUTE_PATHS.login} className={styles.footerLink}>
                  {t('resetPassword.backToLogin')}
                </Link>
              </p>
            </div>
          </>
        )}
      </Card>
    </div>
  );
}

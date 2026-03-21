import { useState } from 'react';
import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useForgotPasswordMutation } from '@/features/auth/api/authApi';
import { useToast, useApiErrorHandler } from '@/shared/hooks';
import { useForm } from '@/shared/hooks/useForm';
import { zodValidate } from '@/shared/lib/utils/zodValidate';
import { createForgotPasswordSchema } from '@/features/auth/schemas/authSchemas';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { Button, Input, Card } from '@/shared/components/ui';
import styles from './ForgotPasswordPage.module.css';

export function ForgotPasswordPage() {
  const { t } = useTranslation();
  const [success, setSuccess] = useState(false);
  const [forgotPassword, { isLoading }] = useForgotPasswordMutation();
  const { toast } = useToast();
  const { handleError } = useApiErrorHandler();

  const form = useForm({
    initialValues: { email: '' },
    validate: zodValidate(createForgotPasswordSchema(t)),
    onSubmit: async (values) => {
      try {
        await forgotPassword({ email: values.email }).unwrap();
        setSuccess(true);
        toast.success(t('forgotPassword.resetLinkSent'));
      } catch (err) {
        handleError(err, t('common.error'));
      }
    },
  });

  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>{t('forgotPassword.title')}</h1>

        {success ? (
          <div className={styles.centered}>
            <div className={styles.successBox}>
              <p className={styles.successTitle}>{t('forgotPassword.checkEmail')}</p>
              <p>{t('forgotPassword.resetLinkSent')}</p>
            </div>
            <Link to={ROUTE_PATHS.login} className={styles.footerLink}>
              {t('forgotPassword.backToLogin')}
            </Link>
          </div>
        ) : (
          <>
            <p className={styles.description}>{t('forgotPassword.subtitle')}</p>

            <form onSubmit={form.handleSubmit} className={styles.form}>
              <Input
                label={t('forgotPassword.emailLabel')}
                type="email"
                name="email"
                value={form.values.email}
                onChange={form.handleChange}
                error={form.errors.email}
                disabled={isLoading}
                required
                placeholder={t('forgotPassword.emailPlaceholder')}
              />

              <Button type="submit" disabled={isLoading || form.isSubmitting} size="lg">
                {isLoading || form.isSubmitting
                  ? t('forgotPassword.sending')
                  : t('forgotPassword.sendResetLink')}
              </Button>
            </form>

            <div className={styles.footer}>
              <p className={styles.footerText}>
                {t('auth.rememberMe')}{' '}
                <Link to={ROUTE_PATHS.login} className={styles.footerLink}>
                  {t('forgotPassword.backToLogin')}
                </Link>
              </p>
            </div>
          </>
        )}
      </Card>
    </div>
  );
}

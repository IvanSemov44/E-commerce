import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForgotPasswordMutation } from '../api/authApi';
import { useToast, useApiErrorHandler } from '@/shared/hooks';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { Button, Input, Card } from '@/shared/components/ui';
import styles from './ForgotPassword.module.css';

export default function ForgotPassword() {
  const { t } = useTranslation();
  const [email, setEmail] = useState('');
  const [success, setSuccess] = useState(false);
  const [forgotPassword, { isLoading }] = useForgotPasswordMutation();
  const { toast } = useToast();
  const { handleError } = useApiErrorHandler();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      await forgotPassword({ email }).unwrap();
      setSuccess(true);
      toast.success(t('forgotPassword.resetLinkSent'));
    } catch (err) {
      handleError(err, t('common.error'));
    }
  };

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

            <form onSubmit={handleSubmit} className={styles.form}>
              <Input
                label={t('forgotPassword.emailLabel')}
                type="email"
                value={email}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => setEmail(e.target.value)}
                disabled={!email || isLoading}
                required
                placeholder={t('forgotPassword.emailPlaceholder')}
              />

              <Button type="submit" disabled={isLoading} size="lg">
                {isLoading ? t('forgotPassword.sending') : t('forgotPassword.sendResetLink')}
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

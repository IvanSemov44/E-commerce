import { useState, useEffect } from 'react';
import { Link, useSearchParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useResetPasswordMutation } from '../api/authApi';
import { useToast } from '@/shared/hooks/useToast';
import { Button, Input, Card } from '../../../shared/components/ui';
import styles from './ResetPassword.module.css';

export default function ResetPassword() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [success, setSuccess] = useState(false);
  const [resetPassword, { isLoading }] = useResetPasswordMutation();
  const { toast } = useToast();

  const email = searchParams.get('email') || '';
  const token = searchParams.get('token') || '';

  useEffect(() => {
    if (!email || !token) {
      toast.error(t('resetPassword.invalidLink'));
    }
  }, [email, token, toast, t]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (password !== confirmPassword) {
      toast.error(t('auth.passwordsDoNotMatch'));
      return;
    }

    if (password.length < 6) {
      toast.error(t('auth.passwordMinLength'));
      return;
    }

    try {
      await resetPassword({ email, token, newPassword: password }).unwrap();
      setSuccess(true);
      toast.success(t('resetPassword.passwordResetSuccess'));
      // Redirect to login after 2 seconds
      setTimeout(() => {
        navigate('/login');
      }, 2000);
    } catch (err: any) {
      toast.error(err?.data?.message || t('resetPassword.failed'));
    }
  };

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
            <Link to="/login" className={styles.footerLink}>
              {t('resetPassword.goToLogin')}
            </Link>
          </div>
        ) : (
          <>
            {email && token ? (
              <>
                <p className={styles.description}>
                  {t('resetPassword.subtitle')}
                </p>

                <form onSubmit={handleSubmit} className={styles.form}>
                  <Input
                    label={t('resetPassword.newPassword')}
                    type="password"
                    value={password}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) => setPassword(e.target.value)}
                    required
                    placeholder={t('resetPassword.newPasswordPlaceholder')}
                  />

                  <Input
                    label={t('resetPassword.confirmNewPassword')}
                    type="password"
                    value={confirmPassword}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) => setConfirmPassword(e.target.value)}
                    required
                    placeholder={t('resetPassword.confirmPasswordPlaceholder')}
                  />

                  <Button
                    type="submit"
                    disabled={isLoading}
                    size="lg"
                  >
                    {isLoading ? t('resetPassword.resetting') : t('resetPassword.resetPasswordBtn')}
                  </Button>
                </form>
              </>
            ) : (
              <div className={styles.centered}>
                <p className={styles.description}>
                  {t('resetPassword.invalidLink')}
                </p>
                <Link to="/forgot-password" className={styles.footerLink}>
                  {t('resetPassword.requestNewLink')}
                </Link>
              </div>
            )}

            <div className={styles.footer}>
              <p className={styles.footerText}>
                {t('auth.rememberMe')}{' '}
                <Link to="/login" className={styles.footerLink}>
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

import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForgotPasswordMutation } from '../store/api/authApi';
import { useToast } from '../hooks';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Card from '../components/ui/Card';
import styles from './Login.module.css';

export default function ForgotPassword() {
  const { t } = useTranslation();
  const [email, setEmail] = useState('');
  const [success, setSuccess] = useState(false);
  const [forgotPassword, { isLoading }] = useForgotPasswordMutation();
  const { toast } = useToast();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      await forgotPassword({ email }).unwrap();
      setSuccess(true);
      toast.success(t('forgotPassword.resetLinkSent'));
    } catch (err: any) {
      toast.error(err?.data?.message || t('common.error'));
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
            <Link to="/login" className={styles.footerLink}>
              {t('forgotPassword.backToLogin')}
            </Link>
          </div>
        ) : (
          <>
            <p className={styles.description}>
              {t('forgotPassword.subtitle')}
            </p>

            <form onSubmit={handleSubmit} className={styles.form}>
              <Input
                label={t('forgotPassword.emailLabel')}
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                placeholder={t('forgotPassword.emailPlaceholder')}
              />

              <Button
                type="submit"
                disabled={isLoading}
                size="lg"
              >
                {isLoading ? t('forgotPassword.sending') : t('forgotPassword.sendResetLink')}
              </Button>
            </form>

            <div className={styles.footer}>
              <p className={styles.footerText}>
                {t('auth.rememberMe')}{' '}
                <Link to="/login" className={styles.footerLink}>
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

import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { Button, Input, Card } from '@/shared/components/ui';
import { useForgotPasswordForm } from './useForgotPasswordForm';
import styles from './ForgotPasswordPage.module.css';

export function ForgotPasswordPage() {
  const { t } = useTranslation();
  const { values, fieldErrors, submitted, handleChange, handleBlur, action, isPending } =
    useForgotPasswordForm();

  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 id="forgot-password-title" className={styles.title}>
          {t('forgotPassword.title')}
        </h1>

        {submitted ? (
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

            <form
              action={action}
              noValidate
              aria-labelledby="forgot-password-title"
              className={styles.form}
            >
              <Input
                label={t('auth.email')}
                type="email"
                name="email"
                value={values.email}
                autoComplete="email"
                autoCapitalize="none"
                autoCorrect="off"
                onChange={handleChange}
                onBlur={handleBlur}
                error={fieldErrors.email}
                disabled={isPending}
                required
              />

              <Button type="submit" disabled={isPending} size="lg">
                {isPending ? t('forgotPassword.sending') : t('forgotPassword.sendResetLink')}
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

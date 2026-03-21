import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { Button, Input, Card } from '@/shared/components/ui';
import { PasswordStrengthIndicator } from '@/features/auth/components/PasswordStrengthIndicator/PasswordStrengthIndicator';
import { PasswordToggleButton } from '@/features/auth/components/PasswordToggleButton/PasswordToggleButton';
import { useResetPasswordForm } from './useResetPasswordForm';
import styles from './ResetPasswordPage.module.css';

export function ResetPasswordPage() {
  const { t } = useTranslation();
  const {
    values,
    fieldErrors,
    password,
    confirmPassword,
    submitted,
    hasValidParams,
    handleChange,
    handleBlur,
    action,
    isPending,
  } = useResetPasswordForm();

  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 id="reset-password-title" className={styles.title}>
          {t('resetPassword.title')}
        </h1>

        {submitted ? (
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
            {hasValidParams ? (
              <>
                <p className={styles.description}>{t('resetPassword.subtitle')}</p>

                <form
                  action={action}
                  noValidate
                  aria-labelledby="reset-password-title"
                  className={styles.form}
                >
                  <Input
                    label={t('resetPassword.newPassword')}
                    type={password.inputType}
                    name="password"
                    value={values.password}
                    autoComplete="new-password"
                    autoCapitalize="none"
                    spellCheck={false}
                    onChange={handleChange}
                    onBlur={handleBlur}
                    error={fieldErrors.password}
                    disabled={isPending}
                    required
                    placeholder={t('resetPassword.newPasswordPlaceholder')}
                    trailingElement={
                      <PasswordToggleButton
                        show={password.show}
                        ariaLabel={password.ariaLabel}
                        onClick={password.toggle}
                      />
                    }
                  />

                  <PasswordStrengthIndicator password={values.password} />

                  <Input
                    label={t('resetPassword.confirmNewPassword')}
                    type={confirmPassword.inputType}
                    name="confirmPassword"
                    value={values.confirmPassword}
                    autoComplete="new-password"
                    autoCapitalize="none"
                    spellCheck={false}
                    onChange={handleChange}
                    onBlur={handleBlur}
                    error={fieldErrors.confirmPassword}
                    disabled={isPending}
                    required
                    placeholder={t('resetPassword.confirmPasswordPlaceholder')}
                    trailingElement={
                      <PasswordToggleButton
                        show={confirmPassword.show}
                        ariaLabel={confirmPassword.ariaLabel}
                        onClick={confirmPassword.toggle}
                      />
                    }
                  />

                  <Button type="submit" disabled={isPending} size="lg">
                    {isPending ? t('resetPassword.resetting') : t('resetPassword.resetPasswordBtn')}
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

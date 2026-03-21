import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { Button, Input, Card } from '@/shared/components/ui';
import { PasswordStrengthIndicator } from '@/features/auth/components/PasswordStrengthIndicator/PasswordStrengthIndicator';
import { PasswordToggleButton } from '@/features/auth/components/PasswordToggleButton/PasswordToggleButton';
import { useRegisterForm } from './useRegisterForm';
import styles from './RegisterPage.module.css';

export function RegisterPage() {
  const { t } = useTranslation();
  const {
    values,
    fieldErrors,
    password,
    confirmPassword,
    handleChange,
    handleBlur,
    action,
    isPending,
  } = useRegisterForm();

  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 id="register-title" className={styles.title}>
          {t('auth.register')}
        </h1>

        <form action={action} noValidate aria-labelledby="register-title" className={styles.form}>
          <div className={styles.nameFields}>
            <Input
              label={t('profile.firstName')}
              name="firstName"
              value={values.firstName}
              autoComplete="given-name"
              maxLength={50}
              onChange={handleChange}
              onBlur={handleBlur}
              error={fieldErrors.firstName}
              disabled={isPending}
              required
            />
            <Input
              label={t('profile.lastName')}
              name="lastName"
              value={values.lastName}
              autoComplete="family-name"
              maxLength={50}
              onChange={handleChange}
              onBlur={handleBlur}
              error={fieldErrors.lastName}
              disabled={isPending}
              required
            />
          </div>

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

          <Input
            label={t('auth.password')}
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
            label={t('auth.confirmPassword')}
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
            trailingElement={
              <PasswordToggleButton
                show={confirmPassword.show}
                ariaLabel={confirmPassword.ariaLabel}
                onClick={confirmPassword.toggle}
              />
            }
          />

          <div className={styles.termsField}>
            <label className={styles.termsLabel}>
              <input
                type="checkbox"
                name="termsAccepted"
                checked={values.termsAccepted}
                onChange={handleChange}
                className={styles.termsCheckbox}
                disabled={isPending}
                aria-required="true"
                aria-invalid={!!fieldErrors.termsAccepted || undefined}
                aria-describedby={fieldErrors.termsAccepted ? 'terms-error' : undefined}
              />
              <span>
                {t('auth.termsAgree')}{' '}
                <Link
                  to={ROUTE_PATHS.terms}
                  className={styles.footerLink}
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  {t('footer.termsOfService')}
                </Link>{' '}
                {t('common.and')}{' '}
                <Link
                  to={ROUTE_PATHS.privacy}
                  className={styles.footerLink}
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  {t('footer.privacyPolicy')}
                </Link>
              </span>
            </label>
            {fieldErrors.termsAccepted && (
              <p id="terms-error" role="alert" className={styles.termsError}>
                {fieldErrors.termsAccepted}
              </p>
            )}
          </div>

          <Button type="submit" disabled={isPending} size="lg">
            {isPending ? t('auth.registering') : t('auth.register')}
          </Button>
        </form>

        <div className={styles.footer}>
          <p className={styles.footerText}>
            {t('auth.alreadyHaveAccount')}{' '}
            <Link to={ROUTE_PATHS.login} className={styles.footerLink}>
              {t('auth.loginHere')}
            </Link>
          </p>
        </div>
      </Card>
    </div>
  );
}
